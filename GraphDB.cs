using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PolarDB;
namespace CommonRDF
{
    class GraphDB : Graph
    {
        private PxCell pxGraph;
        public new void Load(string path)
        {
            if (pxGraph != null) pxGraph.Close();
            pxGraph = new PxCell(tp_graph, path + "\\data.pxc", false);
          //  if (pxGraph.IsEmpty) return;
            XElement db = XElement.Load(path+"\\0001.xml");

            List<Quad> quads = new List<Quad>();
            List<KeyValuePair<string, string>> id_names = new List<KeyValuePair<string, string>>();
            var query = db.Elements() //.Take(1000)
                .Where(el => el.Attribute(sema2012m.ONames.rdfabout) != null);
            foreach (XElement record in query)
            {
                string about = record.Attribute(sema2012m.ONames.rdfabout).Value;
                // Зафиксировать тип
                quads.Add(new Quad(
                    0,
                    about,
                    sema2012m.ONames.rdftypestring,
                    record.Name.NamespaceName + record.Name.LocalName));
                // Сканировать элементы
                foreach (var prop in record.Elements())
                {
                    // Есть разница между объектными свойствами и полями данных
                    string prop_name = prop.Name.NamespaceName + prop.Name.LocalName;
                    XAttribute rdfresource_att = prop.Attribute(sema2012m.ONames.rdfresource);
                    if (rdfresource_att != null)
                    {
                        quads.Add(new Quad(
                            0,
                            about,
                            prop_name,
                            rdfresource_att.Value));
                        quads.Add(new Quad(
                            1,
                            rdfresource_att.Value,
                            prop_name,
                            about));
                    }
                    else
                    {
                        string ex_data = prop.Value; // Надо продолжить!
                        XAttribute lang_att = prop.Attribute(sema2012m.ONames.xmllang);
                        if (lang_att != null) ex_data += "@" + lang_att.Value;
                        quads.Add(new Quad(
                            2,
                            about,
                            prop_name,
                            ex_data));
                        if (prop_name == "http://fogid.net/o/name")
                            id_names.Add(new KeyValuePair<string, string>(about, prop.Value));
                    }
                }
            }
            // Буду строить вот такую структуру:

          pxGraph.Fill2(quads.GroupBy(q => q.entity)
                .Select(q1 =>
                {
                    string type_id = null;
                    Axe[] direct = null;
                    Axe[] inverse = new Axe[0];
                    Axe[] data = null;
                    var rea = q1.GroupBy(q => q.vid)
                        .Select(q2 => new
                        {
                            vid = q2.Key,
                            predvalues = q2.GroupBy(q => q.predicate)
                                .Select(q3 => new { p = q3.Key, preds = q3.Select(q => q.rest).ToList() })
                                .ToArray()
                        }).ToArray();
                    foreach (var va in rea)
                    {
                        if (va.vid == 0)
                        {
                            // Поиск первого типа (может не надо уничтожать запись???)
                            var qw = va.predvalues.FirstOrDefault(p => p.p == sema2012m.ONames.rdftypestring);
                            if (qw != null)
                            {
                                type_id = qw.preds.First();
                                //qw.preds.RemoveAt(0); // Уничтожение ссылки на тип
                            }
                            direct = va.predvalues
                                .Select(pv => new Axe() { predicate = pv.p, variants = pv.preds.ToArray() })
                                .ToArray();
                        }
                        else if (va.vid == 1)
                        {
                            inverse = va.predvalues
                                .Select(pv => new Axe() { predicate = pv.p, variants = pv.preds.ToArray() })
                                .ToArray();
                        }
                        else if (va.vid == 2)
                        {
                            data = va.predvalues
                                .Select(pv => new Axe() { predicate = pv.p, variants = pv.preds.ToArray() })
                                .ToArray();
                        }
                        
                    }if (direct == null) direct = new Axe[0];
                        if (inverse == null) inverse = new Axe[0];
                        if (data == null) data = new Axe[0];
                    return
                        new[] { (object)q1.Key, (object)q1.Key, direct.Select(Axe2Objects).ToArray(), inverse.Select(Axe2Objects).ToArray(), data.Select(Axe2Objects).ToArray() };
                })
                .ToArray());
        }

        private static object[]  Axe2Objects(Axe axe)
        {
            return new[]{(object)axe.predicate, axe.variants.Cast<object>().ToArray()};
        }
        public static readonly PType tp_axe= new PTypeRecord(
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("variants", new PTypeSequence(new PType(PTypeEnumeration.sstring))));
        public static readonly PType tp_graph= new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("rtype", new PType(PTypeEnumeration.sstring)),
                new NamedType("direct", new PTypeSequence(tp_axe)),
                new NamedType("inverse", new PTypeSequence(tp_axe)),
                new NamedType("data", new PTypeSequence(tp_axe))));
    }
}