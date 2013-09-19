using System;

namespace CommonRDF
{


    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;
            GraphBase gr = new GraphTripletsTree(@"..\..\DataFreebase\");
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
           gr.Load(@"F:\freebase-rdf-2013-02-10-00-00.nt2");//"@"..\..\0001.xml"
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //Console.WriteLine("Test ok duration========================" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

              MagProgram mprog = new MagProgram(gr);
            mprog.Run();
            LeshProgram l = new LeshProgram(gr);
            l.Run(); 
         

            gr.Test();
        }
    }
}
