using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using PolarDB;
using sema2012m;

namespace CommonRDF
{
    class GraphTripletsTree : GraphBase
    {
         private string path;
        private PType tp_triplets;
        private PType tp_quads;
        private PType tp_n4;
        private PType tp_graph;

        private PaCell triplets;
        private PaEntry any_triplet;

        private PxCell graph_x;
        private PxCell n4_x;

        public GraphTripletsTree(string path)
        {
            if (path[path.Length - 1] != '\\' && path[path.Length - 1] == '/') path = path + "/";
            this.path = path;
            InitTypes();
            InitCells();
        }

        public override IEnumerable<string> GetEntities()
        {
            foreach (PxEntry pxe in graph_x.Root.Elements())
            {
                var first = pxe.Field(1).Elements().FirstOrDefault();
                if (first.Typ == null || !first.Field(1).Elements().Any())
                {
                     first = pxe.Field(2).Elements().FirstOrDefault();
                     if (first.Typ == null || !first.Field(1).Elements().Any())
                    {
                         first = pxe.Field(3).Elements().FirstOrDefault();
                        if (first.Typ == null || !first.Field(1).Elements().Any())
                            continue;
                        yield return ((OProp)Triplet.Create(GetTriplet((long)first.Field(1).Elements().First().Get().Value))).o;
                    }
                }
               yield return Triplet.Create(GetTriplet((long)first.Field(1).Elements().First().Get().Value)).s;
            }
        }

        public override IEnumerable<PredicateEntityPair> GetDirect(string id)
        {
            return GetProperty(id, 1, t => t.s == id)
                .Cast<OProp>()
                .Select(t => new PredicateEntityPair(t.p,t.o));
        }

        public override IEnumerable<PredicateEntityPair> GetInverse(string id)
        {
            return GetProperty(id, 1, t => t is OProp && ((OProp)t).o == id)
               .Select(t => new PredicateEntityPair(t.p, t.s));
        }

        public override IEnumerable<PredicateDataTriple> GetData(string id)
        {
            return GetProperty(id, 1, t => t.s == id)
                .Cast<DProp>()
                .Select(t => new PredicateDataTriple(t.p, t.d, t.d));
        }

        private IEnumerable<Triplet> GetProperty(string id, int direction,
            Predicate<Triplet> predicateValuesTest, int predicateSC = -1)
        {
            PxEntry found = GetEntryById(id);
            if (found.IsEmpty) return null;
            Triplet first4Test;
            IEnumerable<PxEntry> pxEntries = found.Field(direction).Elements();
            if (predicateSC != -1)
                pxEntries = pxEntries
                    .Where(pRec => (int) pRec.Field(0).Get().Value == predicateSC);
            return pxEntries
                .Select(pRec =>
                    pRec.Field(1)
                        .Elements()
                        .Select(offEn => offEn.Get().Value)
                        .Cast<long>()
                        .Select(GetTriplet)
                        .Select(Triplet.Create))
                .Where(predicateResults =>
                    (first4Test = predicateResults.FirstOrDefault()) != null
                    && predicateValuesTest(first4Test))
                .SelectMany(resultsGroup => resultsGroup);
            // Еще отбраковкаtri => tri is OProp && tri.s == id &&
        }

        private object GetTriplet(long offTtripl)
        {
            any_triplet.offset = offTtripl;
            return any_triplet.Get().Value;
        }

        public override IEnumerable<string> GetDirect(string id, string predicate)
        {
            return GetProperty(id, 1, t => t.s == id && t.p == predicate, predicate.GetHashCode())
                .Cast<OProp>()
                .Select(t => t.o);
        }

        public override IEnumerable<string> GetInverse(string id, string predicate)
        {
            return GetProperty(id, 2, 
                t => t.p == predicate && (t is OProp) && ((OProp) t).o == id,
                predicate.GetHashCode())
                    .Select(t => t.s);
        }

        public override IEnumerable<string> GetData(string id, string predicate)
        {
            return GetProperty(id, 3, t => t.s == id && t.p == predicate, predicate.GetHashCode())
                .Cast<DProp>()
                .Select(t => t.d);
        }

        public override IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate)
        {
            return GetData(id, predicate).Select(SplitLang);
        }

        public override void GetItembyId(string id)
        {
            throw new NotImplementedException();
        }

        public override void Test()
        {
            throw new NotImplementedException();
        }
        public override string[] SearchByN4(string ss)
        {
            throw new NotImplementedException();
        }

        // =============== Методы доступа к данным ==================
        internal PxEntry GetEntryById(string id)
        {
            int e_hs = id.GetHashCode();
            PxEntry found = graph_x.Root.BinarySearchFirst(element =>
            {
                int v = (int)element.Field(0).Get().Value;
                return v < e_hs ? -1 : (v == e_hs ? 0 : 1);
            });
            return found;
        }
        public override void Load(string[] rdf_files)
        {
            DateTime tt0 = DateTime.Now;
            // Закроем использование
            if (triplets != null) { triplets.Close(); triplets = null; }
            if (graph_x != null) { graph_x.Close(); graph_x = null; }
            if (n4_x != null) { n4_x.Close(); graph_x = null; }
            // Создадим ячейки
            triplets = new PaCell(tp_triplets, path + "triplets.pac", false);
            triplets.Clear();
            var quads = new PaCell(tp_quads, path + "quads.pac", false);
            var graph_a = new PaCell(tp_graph, path + "graph_a.pac", false);
            graph_x = new PxCell(tp_graph, path + "graph_x.pxc", false); graph_x.Clear();
            n4_x = new PxCell(tp_n4, path + "n4_x.pxc", false); n4_x.Clear();
            var n4 = new PaCell(tp_n4, path + "n4.pac", false);
            n4.Clear();
            Console.WriteLine("cells initiated duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            TripletSerialInput(triplets, rdf_files);
            Console.WriteLine("After TripletSerialInput. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;


            LoadQuadsAndSort(n4, quads);
            Console.WriteLine("After LoadQuadsAndSort(). duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            FormingSerialGraph(new SerialBuffer(graph_a, 3), quads);
            Console.WriteLine("Forming serial graph ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // произвести объектное представление
            graph_x.Fill2(graph_a.Root.Get().Value);
            n4_x.Fill2(n4.Root.Get().Value);
            Console.WriteLine("Forming fixed graph ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // ========= Завершение загрузки =========
            // Закроем файлы и уничтожим ненужные
            triplets.Close();
            quads.Close(); File.Delete(path + "quads.pac");
            graph_a.Close(); File.Delete(path + "graph_a.pac");
            graph_x.Close();
            // Откроем для использования
            InitCells();
        }
     
        // ============ Технические методы ============
        private void FormingSerialGraph(ISerialFlow serial, PaCell quads)
        {
            serial.StartSerialFlow();
            serial.S();

            int hs_e = Int32.MinValue;
            int vid = Int32.MinValue;
            int vidstate = 0;
            int hs_p = Int32.MinValue;

            bool firsttime = true;
            bool firstprop = true;
            foreach (object[] el in quads.Root.Elements().Select(e => e.Value))
            {
                FourFields record = new FourFields((int)el[0], (int)el[1], (int)el[2], (long)el[3]);
                if (firsttime || record.e_hs != hs_e)
                { // Начало новой записи
                    firstprop = true;
                    if (!firsttime)
                    { // Закрыть предыдущую запись
                        serial.Se();
                        serial.Re();
                        serial.Se();
                        while (vid < 2 && vidstate <= 2)
                        {
                            serial.S();
                            serial.Se();
                            vidstate += 1;
                        }
                        serial.Re();
                    }
                    vidstate = 0;
                    hs_e = record.e_hs;
                    serial.R();
                    serial.V(record.e_hs);
                    vid = record.vid;
                    while (vidstate < vid)
                    {
                        serial.S();
                        serial.Se();
                        vidstate += 1;
                    }
                    vidstate += 1;
                    serial.S();
                }
                else if (record.vid != vid)
                {
                    serial.Se();
                    serial.Re();
                    firstprop = true;

                    serial.Se();
                    vid = record.vid;
                    while (vid != vidstate)
                    {
                        serial.S();
                        serial.Se();
                        vidstate += 1;
                    }
                    vidstate += 1;
                    serial.S();
                }

                if (firstprop || record.p_hs != hs_p)
                {
                    hs_p = record.p_hs;
                    if (!firstprop)
                    {
                        serial.Se();
                        serial.Re();
                    }
                    firstprop = false;
                    serial.R();
                    serial.V(record.p_hs);
                    serial.S();
                }
                serial.V(record.off);
                firsttime = false;
            }
            if (!firsttime)
            { // Закрыть последнюю запись
                serial.Se();
                serial.Re();
                serial.Se();
                while (vid < 2 && vidstate <= 2)
                {
                    serial.S();
                    serial.Se();
                    vidstate += 1;
                }
                serial.Re();
            }
            serial.Se();
            serial.EndSerialFlow();
        }
        private struct FourFields
        {
            public int e_hs, vid, p_hs;
            public long off;
            public FourFields(int a, int b, int c, long d)
            {
                this.e_hs = a; this.vid = b; this.p_hs = c; this.off = d;
            }
        }


        private void InitCells()
        {
            if (!File.Exists(path + "triplets.pac")
                || !File.Exists(path + "graph_x.pxc")) return;
            triplets = new PaCell(tp_triplets, path + "triplets.pac");
            any_triplet = triplets.Root.Element(0);
            graph_x = new PxCell(tp_graph, path + "graph_x.pxc");
        }
        private static void TripletSerialInput(ISerialFlow sflow, IEnumerable<string> rdf_filenames)
        {
            sflow.StartSerialFlow();
            sflow.S();
            foreach (string db_falename in rdf_filenames)
                ReadXML2Quad(db_falename, (id, property, value, isObj, lang) =>
                    sflow.V(isObj
                        ? new object[] { 1, new object[] { id, property, value } }
                        : new object[] { 2, new object[] { id, property, value, lang ?? "" } }));
            sflow.Se();
            sflow.EndSerialFlow();
        }
        private delegate void QuadAction(string id, string property,
             string value, bool isDirect = false, string lang = null);

        private static string langAttributeName = "xml:lang",
            rdfAbout = "rdf:about",
               rdfResource = "rdf:resource",
               NS = "http://fogid.net/o/";


        private static void ReadXML2Quad(string url, QuadAction quadAction)
        {
            string resource;
            bool isDirect;
            string id = string.Empty;
            using (var xml = new XmlTextReader(url))
                while (xml.Read())
                    if (xml.IsStartElement())
                        if (xml.Depth == 1 && (id = xml[rdfAbout]) != null)
                            quadAction(id, ONames.rdftypestring, NS + xml.Name);
                        else if (xml.Depth == 2 && id != null)
                            quadAction(id, NS + xml.Name,
                                isDirect: isDirect = (resource = xml[rdfResource]) != null,
                                lang: isDirect ? null : xml[langAttributeName],
                                value: isDirect ? resource : xml.ReadString());
        }
        private void LoadQuadsAndSort(PaCell n4, PaCell quads)
        {
            n4.StartSerialFlow();
            n4.S();
            quads.StartSerialFlow();
            quads.S();
            foreach (var tri in triplets.Root.Elements())
            {
                object[] tri_uni = (object[])tri.Value;
                int tag = (int)tri_uni[0];
                object[] rec = (object[])tri_uni[1];
                int hs_s = ((string)rec[0]).GetHashCode();
                int hs_p = ((string)rec[1]).GetHashCode();
                if (tag == 1) // объектое свойство
                {
                    int hs_o = ((string)rec[2]).GetHashCode();
                    quads.V(new object[] { hs_s, 0, hs_p, tri.Offset });
                    quads.V(new object[] { hs_o, 1, hs_p, tri.Offset });
                }
                else // поле данных
                {
                    quads.V(new object[] { hs_s, 2, hs_p, tri.Offset });
                    if ((string)rec[1] != sema2012m.ONames.p_name) continue;
                    // Поместим информацию в таблицу имен n4
                    string name = (string)rec[2];
                    string name4 = name.Length <= 4 ? name : name.Substring(0, 4);
                    n4.V(new object[] { hs_s, name4.ToLower() });
                }
            }
            quads.Se();
            quads.EndSerialFlow();
            n4.Se();
            n4.EndSerialFlow();

            // Сортировка квадриков
            quads.Root.Sort((o1, o2) =>
            {
                object[] v1 = (object[])o1;
                object[] v2 = (object[])o2;
                int e1 = (int)v1[0];
                int e2 = (int)v2[0];
                int q1 = (int)v1[1];
                int q2 = (int)v2[1];
                int p1 = (int)v1[2];
                int p2 = (int)v2[2];
                return e1 < e2 ? -3 : (e1 > e2 ? 3 :
                    (q1 < q2 ? -2 : (q1 > q2 ? 2 :
                    (p1 < p2 ? -1 : (p1 > p2 ? 1 : 0)))));
            });
            // Сортировка таблицы имен
            n4.Root.Sort((o1, o2) =>
            {
                object[] v1 = (object[])o1;
                object[] v2 = (object[])o2;
                string s1 = (string)v1[1];
                string s2 = (string)v2[1];
                return s1.CompareTo(s2);
            });
        }
        private void InitTypes()
        {
            tp_triplets =
                new PTypeSequence(
                    new PTypeUnion(
                        new NamedType("empty", new PType(PTypeEnumeration.none)), // не используется, нужен для выполнения правила атомарного варианта
                        new NamedType("op",
                            new PTypeRecord(
                                new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                                new NamedType("obj", new PType(PTypeEnumeration.sstring)))),
                        new NamedType("dp",
                            new PTypeRecord(
                                new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                                new NamedType("data", new PType(PTypeEnumeration.sstring)),
                                new NamedType("lang", new PType(PTypeEnumeration.sstring))))));
            tp_quads =
                new PTypeSequence(
                    new PTypeRecord(
                        new NamedType("hs_e", new PType(PTypeEnumeration.integer)),
                        new NamedType("vid", new PType(PTypeEnumeration.integer)),
                        new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                        new NamedType("off", new PType(PTypeEnumeration.longinteger))));
            tp_graph = new PTypeSequence(new PTypeRecord(
                new NamedType("hs_e", new PType(PTypeEnumeration.integer)),
                new NamedType("direct",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("off", new PTypeSequence(new PType(PTypeEnumeration.longinteger)))))),
                new NamedType("inverse",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("off", new PTypeSequence(new PType(PTypeEnumeration.longinteger)))))),
                new NamedType("data",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("off", new PTypeSequence(new PType(PTypeEnumeration.longinteger))))))));
            tp_n4 = new PTypeSequence(new PTypeRecord(
                new NamedType("hs_triplets", new PTypeSequence(new PType(PTypeEnumeration.longinteger))),
                new NamedType("s4", new PTypeFString(4))));
        }
    }
}