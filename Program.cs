using System;

namespace CommonRDF
{


    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;
            //Graph gr = new Graph();
            //gr.Load(@"..\..\0001.xml");
            //gr.Test();
            GraphDBmy grdb = new GraphDBmy(@"..\..\");
            //grdb.Load();
            grdb.Test();
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            return;
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            Console.WriteLine("Test ok duration========================" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            //MagProgram mprog = new MagProgram(gr);
            //mprog.Run();
            //LeshProgram l = new LeshProgram(gr);
            //l.Run();
        }
    }
}
