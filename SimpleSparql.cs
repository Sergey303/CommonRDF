using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonRDF
{
    public class SimpleSparql
    {
        private Dictionary<string, RecordEx> dics;
        public SimpleSparql(Dictionary<string, RecordEx> dics, string id)
        {
            this.dics = dics;
            // Sparql section
            Sample[] testquery = new Sample[0];
            DescrVar[] testvars = new DescrVar[0];
            testquery = new Sample[] 
            {
                new Sample() { vid = TripletVid.op, firstunknown = 0, 
                    subject= new TVariable() { isVariable=true, value="?s", index=0 },
                    predicate = new TVariable() { isVariable=false, value=sema2012m.ONames.p_participant },
                    obj = new TVariable() { isVariable = false, value=id}},
                new Sample() { vid = TripletVid.op, firstunknown = 1, 
                    subject= new TVariable() { isVariable=true, value="?s", index=0 },
                    predicate = new TVariable() { isVariable=false, value=sema2012m.ONames.p_inorg },
                    obj = new TVariable() { isVariable = true, value="?inorg", index=1 }},
                new Sample() { vid = TripletVid.op, firstunknown = 2, 
                    subject= new TVariable() { isVariable=true, value=id, index=0 },
                    predicate = new TVariable() { isVariable=false, value=sema2012m.ONames.rdftypestring },
                    obj = new TVariable() { isVariable = false, value="http://fogid.net/o/participation"}},
                new Sample() { vid = TripletVid.dp, firstunknown = 2, 
                    subject= new TVariable() { isVariable=true, value="?inorg", index=1 },
                    predicate = new TVariable() { isVariable=false, value=sema2012m.ONames.p_name },
                    obj = new TVariable() { isVariable = true, value="?orgname", index=2 }},
                new Sample() { vid = TripletVid.dp, firstunknown = 3, 
                    subject= new TVariable() { isVariable=true, value="?s", index=0 },
                    predicate = new TVariable() { isVariable=false, value=sema2012m.ONames.p_fromdate },
                    obj = new TVariable() { isVariable = true, value="?fd", index=3 }, option = true },
            };
            testvars = new DescrVar[] 
            {
                new DescrVar() { isEntity = true, varName="?s" },
                new DescrVar() { isEntity = true, varName="?inorg" },
                new DescrVar() { isEntity = false, varName="?orgname" },
                new DescrVar() { isEntity = false, varName="?fd" },
            };
            // Попытка написать вычисление
            int nextsample = 0;
            bool bresult = Match(gr, testquery, testvars, nextsample);
            if (!bresult) Console.WriteLine("false");
        }
        // Возвращает истину если сопоставление состоялось
        private static bool Match(Graph gr, Sample[] testquery, DescrVar[] testvars, int nextsample)
        {
            // Вывести если дошли до конца
            if (nextsample >= testquery.Length)
            {
                Console.Write("R:"); // Здесь будет вывод значения переменных
                foreach (var va in testvars)
                {
                    Console.Write(va.varName + "=" + va.varValue + " ");
                }
                Console.WriteLine();
                return true;
            }
            // Match
            var sam = testquery[nextsample];
            // Разметить пустыми значениями, если option
            if (sam.option)
            {
                //for (int i = nextsample; i < // нужен цикл!
                if (sam.firstunknown < testvars.Length) testvars[sam.firstunknown].varValue = null;
            }
            // Пока считаю предикаты известными. Вариантов 4: 0 - обе части неизвестны, 1 - субъект известен, 2 - объект известен, 3 - все известно
            int variant = (sam.subject.isVariable && sam.subject.index >= sam.firstunknown ? 0 : 1) +
                (sam.obj.isVariable && sam.obj.index >= sam.firstunknown ? 0 : 2);
            if (variant == 1)
            {
                string idd = sam.subject.isVariable ? testvars[sam.subject.index].varValue : sam.subject.value;
                // В зависимости от вида, будут использоваться данные разных осей
                if (sam.vid == TripletVid.dp)
                { // Dataproperty
                    foreach (var data in gr.GetData(idd, sam.predicate.value))
                    {
                        testvars[sam.obj.index].varValue = data;
                        Match(gr, testquery, testvars, nextsample + 1);
                    }
                }
                else
                {
                    return sam.option ? Match(gr, testquery, testvars, nextsample + 1) : false;
                }
                //RecordEx erec;
                //if (dics.TryGetValue(idd, out erec))
                //{
                //    Axe[] predicate_group = sam.vid == TripletVid.dp ? erec.data : erec.direct;
                //    Axe found_predicate = predicate_group.FirstOrDefault(ax => ax.predicate == sam.predicate.value); // не проверяется, что предикат - константа
                //    if (found_predicate != null)
                //    {
                //        foreach (var v in found_predicate.variants)
                //        {
                //            testvars[sam.obj.index].varValue = v;
                //            bool br = Match(dics, testquery, testvars, nextsample + 1);
                //        }
                //        // Надо бы еще посмотреть случай пустого количества вариантов 
                //    }
                //    else return sam.option ? Match(dics, testquery, testvars, nextsample + 1) : false; // Здесь должны быть особенности в связи с опциями
                //}
                //else throw new Exception("can't find " + idd);
            }
            else if (variant == 2) // obj - known, subj - unknown
            {
                string ido = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                // Пока будем обрабатывать только объектные ссылки
                if (sam.vid == TripletVid.op)
                {
                    RecordEx erec;
                    if (dics.TryGetValue(ido, out erec))
                    {
                        Axe[] predicate_group = erec.inverse;
                        Axe found_predivate = predicate_group.FirstOrDefault(ax => ax.predicate == sam.predicate.value); // не проверяется, что предикат - константа
                        if (found_predivate != null)
                        {
                            foreach (var v in found_predivate.variants)
                            {
                                testvars[sam.subject.index].varValue = v;
                                bool br = Match(dics, testquery, testvars, nextsample + 1);
                            }
                        }
                        else return sam.option ? Match(dics, testquery, testvars, nextsample + 1) : false; // Здесь должны быть особенности в связи с опциями
                    }
                    else throw new Exception("can't find " + ido);
                }
                else throw new Exception("datatype properties are not implemented to inverse direction");
            }
            else if (variant == 3)
            {
                string idd = sam.subject.isVariable ? testvars[sam.subject.index].varValue : sam.subject.value;
                string obj = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                RecordEx erec;
                if (dics.TryGetValue(idd, out erec))
                {
                    Axe[] predicate_group = sam.vid == TripletVid.dp ? erec.data : erec.direct;
                    Axe found_predivate = predicate_group.FirstOrDefault(ax => ax.predicate == sam.predicate.value); // не проверяется, что предикат - константа
                    if (found_predivate != null)
                    {
                        foreach (var v in found_predivate.variants)
                        {
                            string objvalue = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                            if (objvalue != v) continue;
                            bool br = Match(dics, testquery, testvars, nextsample + 1);
                        }
                    }
                    else return sam.option ? Match(dics, testquery, testvars, nextsample + 1) : false; // Здесь должны быть особенности в связи с опциями
                }
                else throw new Exception("can't find " + idd);
            }
            else
            {
                throw new Exception("Unimplemented");
            }
            return true;
        }
    }
}
