using System;

namespace CommonRDF
{


    internal class Program
    {
        private static void Main(string[] args)
        {
            DateTime tt0 = DateTime.Now;
            Graph gr = new Graph();
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            gr.Load(@"..\..\0001.xml");
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            gr.Test();
            Console.WriteLine("Test ok duration========================" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            MagProgram mprog = new MagProgram(gr);
            mprog.Run();
            //LeshProgram l = new LeshProgram(gr);
            //l.Run();
        }
    }
}
