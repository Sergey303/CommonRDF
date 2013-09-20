using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CommonRDF
{
    class MagProgram : IReceiver
    {
        private GraphBase gr;
        private List<string[]> receive_list;
        public MagProgram(GraphBase gr)
        {
            this.gr = gr;
            Restart();
        }
        public void Restart() { receive_list = new List<string[]>(); }
        public void Receive(string[] row) { receive_list.Add(row); }
        public void Run()
        {
            if (false) // прект freebase3m
            {
                int i = 0;
                foreach (var entity in gr.GetEntities())
                {
                    Console.WriteLine("{0}", entity);
                    i++;
                    if (i >= 10) break;
                }
                string idd = "ns:m.05ypwqv"; //"ns:m.0hz6pwx";
                //foreach (var qu in gr.GetData(idd)) Console.WriteLine("\t{0} {1}", qu.predicate, qu.data);
                XElement portrait = ((GraphTripletsTree)gr).GetPortraitSimple(idd, true);
                if (portrait != null) Console.WriteLine(portrait.ToString());

                return;
            }
            DateTime tt0 = DateTime.Now;
            //string id = "w20070417_5_8436"; // Марчук Александр Гурьевич
            string id = "piu_200809051791";  // Ершов Андрей Петрович
            SimpleSparql sims = new SimpleSparql(id);
            bool atleastonce = sims.Match(gr, this);
            Console.WriteLine("mag Sparql test ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            if (!atleastonce) Console.WriteLine("false");
            else
            {
                foreach (var row in receive_list)
                {
                    foreach (string v in row) Console.Write(v + " ");
                    Console.WriteLine();
                }
            }
            tt0 = DateTime.Now;

            Restart();
            atleastonce = sims.Match(gr, this);
            if (!atleastonce) Console.WriteLine("false");
            Console.WriteLine("mag Sparql test 2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
