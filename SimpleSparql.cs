﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using sema2012m;

namespace CommonRDF
{
    public interface IReceiver
    {
        void Restart();
        void Receive(string[] row);
    }
    public class SimpleSparql
    {
        private Sample[] testquery = new Sample[0];
        private DescrVar[] testvars = new DescrVar[0];
        public SimpleSparql(string id)
        {
            testquery = new Sample[]
            {
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 0,
                    subject = new TVariable {isVariable = true, value = "?s", index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.p_participant},
                    obj = new TVariable {isVariable = false, value = id, index = 4}
                },
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 1,
                    subject = new TVariable {isVariable = true, value = "?s", index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.p_inorg},
                    obj = new TVariable {isVariable = true, value = "?inorg", index = 1}
                },
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 2,
                    subject = new TVariable {isVariable = true, value = id, index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.rdftypestring},
                    obj = new TVariable {isVariable = false, value = "http://fogid.net/o/participation", index = 5}
                },
                new Sample
                {
                    vid = TripletVid.dp,
                    firstunknown = 2,
                    subject = new TVariable {isVariable = true, value = "?inorg", index = 1},
                    predicate = new TVariable {isVariable = false, value = ONames.p_name},
                    obj = new TVariable {isVariable = true, value = "?orgname", index = 2}
                },
                new Sample
                {
                    vid = TripletVid.dp,
                    firstunknown = 3,
                    subject = new TVariable {isVariable = true, value = "?s", index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.p_fromdate},
                    obj = new TVariable {isVariable = true, value = "?fd", index = 3},
                    option = true
                },
            };
            testvars = new DescrVar[] 
            {
                new DescrVar { isEntity = true, varName="?s" },
                new DescrVar { isEntity = true, varName="?inorg" },
                new DescrVar { isEntity = false, varName="?orgname" },
                new DescrVar { isEntity = false, varName="?fd" },
                //consts objects
                new DescrVar { isEntity = true, varValue =id },
                new DescrVar { isEntity = true, varValue ="http://fogid.net/o/participation" },
            };
        }
        public bool Match(GraphBase gr, IReceiver receive) { return Match(gr, 0, receive); } 
        // Возвращает истину если сопоставление состоялось хотя бы один раз
        private bool Match(GraphBase gr, int nextsample, IReceiver receive)
        {
            // Вывести если дошли до конца
            if (nextsample >= testquery.Length)
            {
                string[] row = new string[testvars.Length];
                for ( int i = 0; i < testvars.Length; i++)
                {
                    row[i] = testvars[i].varValue;
                }
                receive.Receive(row);
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
                if (sam.firstunknown < testvars.Length) testvars[sam.firstunknown].varValue = null;
            }
            // Пока считаю предикаты известными. Вариантов 4: 0 - обе части неизвестны, 1 - субъект известен, 2 - объект известен, 3 - все известно
            int variant = (sam.subject.isVariable && sam.subject.index >= sam.firstunknown ? 0 : 1) +
                (sam.obj.isVariable && sam.obj.index >= sam.firstunknown ? 0 : 2);
            if (variant == 1)
            {
                string idd = sam.subject.isVariable ? testvars[sam.subject.index].varValue : sam.subject.value;
                bool atleastonce = false; 
                // В зависимости от вида, будут использоваться данные разных осей
                if (sam.vid == TripletVid.dp)
                { // Dataproperty
                    foreach (var data in gr.GetData(idd, sam.predicate.value))
                    {
                        testvars[sam.obj.index].varValue = data;
                        atleastonce=Match(gr, nextsample + 1, receive);

                    }
                }
                else
                { // Objectproperty
                    foreach (var directid in gr.GetDirect(idd, sam.predicate.value))
                    {
                        testvars[sam.obj.index].varValue = directid;
                        atleastonce=Match(gr, nextsample + 1, receive);
                    }
                }
                return atleastonce || sam.option && Match(gr, nextsample + 1, receive);
            }
            else if (variant == 2) // obj - known, subj - unknown
            {
                string ido = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                // Пока будем обрабатывать только объектные ссылки
                if (sam.vid == TripletVid.op)
                {
                    foreach (var inverseid in gr.GetInverse(ido, sam.predicate.value))
                    {
                        testvars[sam.subject.index].varValue = inverseid;
                        Match(gr, nextsample + 1, receive);
                    }
                    //TODO: Нужен ли вариант с опцией?
                }
                else
                { //Здесь вариант, когда данное известно
                    if (sam.predicate.value==ONames.p_name)
                        foreach (var id in gr.SearchByName(ido))
                        {
                            testvars[sam.subject.index].varValue = id;
                            Match(gr, nextsample + 1, receive);
                        }
                    foreach (var id in gr.GetEntities().Where(id => gr.GetData(id, sam.predicate.value).Contains(ido)))
                    {
                        testvars[sam.subject.index].varValue = id;
                        Match(gr, nextsample + 1, receive);
                    }
                }
            }
            else if (variant == 3)
            {
                string idd = sam.subject.isVariable ? testvars[sam.subject.index].varValue : sam.subject.value;
                //string obj = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                bool br = false;
                foreach (var directid in gr.GetDirect(idd, sam.predicate.value))
                {
                    string objvalue = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                    if (objvalue != directid) continue;
                    br = Match(gr, nextsample + 1, receive);
                }
                return br;
                //TODO: Нужен ли вариант, связанный с опциями?
            }
            else
            {
                throw new Exception("Unimplemented");
            }
            return true;
        }
    }
}
