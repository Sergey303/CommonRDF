using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Web.UI.WebControls;

namespace CommonRDF
{


    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;
            // Проект Standard

            //GraphTripletsTree.rdfAbout = "rdf:about";
            //GraphTripletsTree.rdfResource = "rdf:resource";
            //   gr.Load(@"..\..\PA\0001.xml");

            // Проект twomillions
            //   GraphBase gr = new GraphTripletsTree(@"..\..\twomillions\");
            //Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            // gr.Load(@"..\..\twomillions\tm_0.xml");
            //return;

            // Проект Freebase3M
            // GraphBase gr = new GraphTripletsTree(@"..\..\DataFreebase\");
            //gr.Load(@"F:\freebase-rdf-2013-02-10-00-00.nt2");
            

            GraphBase gr=null;
            // Проект Берлинские тестовые данные
            //GraphBase gr = new GraphTripletsTree(@"..\..\???\");
            DirectoryInfo directoryInfo = new DirectoryInfo(@"D:\bsbm\indexes");
          // if (directoryInfo.Exists)
            //    directoryInfo.Delete(true);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            Perfomance.ComputeTime(() =>
            {
                gr = new PolarBasedRdfGraph(directoryInfo);
               // gr.CreateGraph();
                gr.Load(@"D:\deployed\dataset1M.ttl");
            }, "create load graph 1M", true);
   
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //Console.WriteLine("Test ok duration========================" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
//            MagProgram mprog = new MagProgram(gr);
          //  mprog.Run();
            //Console.WriteLine("Run ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //Expression<Func<string, string, bool>> p = (r, l) => (r = l) == l;
//gr.Test();
            LeshProgram l = new LeshProgram(gr);
            //  Perfomance.ComputeTime(
            l.Run();//, "");
        }
    }
}
