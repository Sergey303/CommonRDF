﻿using System;
using System.Linq;
using System.Xml.Linq;

namespace CommonRDF
{


    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;
            // Проект Standard
            //GraphBase gr = new GraphTripletsTree(@"..\..\PA\");
            //GraphTripletsTree.rdfAbout = "rdf:about";
            //GraphTripletsTree.rdfResource = "rdf:resource";
            //gr.Load(@"..\..\PA\0001.xml");

            // Проект twomillions
            GraphBase gr = new GraphTripletsTree(@"..\..\twomillions\");
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks/10000L);
            tt0 = DateTime.Now;
            //gr.Load(@"..\..\twomillions\tm_0.xml");
            //return;

            // Проект Freebase3M
            //  GraphBase gr = new GraphTripletsTree(@"..\..\DataFreebase\");
            //  gr.Load(@"F:\freebase-rdf-2013-02-10-00-00.nt2");

            // Проект ???
            //GraphBase gr = new GraphTripletsTree(@"..\..\???\");
            //gr.Load(@"???");

            gr.CreateGraph();
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks/10000L);
            tt0 = DateTime.Now;
            //Console.WriteLine("Test ok duration========================" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            MagProgram mprog = new MagProgram(gr);
            LeshProgram l = new LeshProgram(gr);
            //  l.Run();
            mprog.Run();

            gr.Test();
        }
    }
}
