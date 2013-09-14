using System;

namespace CommonRDF
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime tt0 = DateTime.Now;
            Graph gr = new Graph();
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            gr.Load(@"..\..\0001.xml");
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            gr.Test();
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }

      
    }
}
