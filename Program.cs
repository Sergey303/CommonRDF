<<<<<<< HEAD
﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
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
            GraphBase gr = new GraphTripletsTree(@"..\..\PA\");
            //GraphTripletsTree.rdfAbout = "rdf:about";
            //GraphTripletsTree.rdfResource = "rdf:resource";
            //gr.Load(@"..\..\PA\0001.xml");

        // Проект twomillions
        //   GraphBase gr = new GraphTripletsTree(@"..\..\twomillions\");
           //Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
          // gr.Load(@"..\..\twomillions\tm_0.xml");
           //return;
            
        // Проект Freebase3M
           // GraphBase gr = new GraphTripletsTree(@"..\..\DataFreebase\");
            //gr.Load(@"F:\freebase-rdf-2013-02-10-00-00.nt2");
        
        // Проект ???
            //GraphBase gr = new GraphTripletsTree(@"..\..\???\");
            //gr.Load(@"???");

           //gr.CreateGraph();
            Console.WriteLine("Graph ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //Console.WriteLine("Test ok duration========================" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
//            MagProgram mprog = new MagProgram(gr);
          //  mprog.Run();
            //Console.WriteLine("Run ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //Expression<Func<string, string, bool>> p = (r, l) => (r = l) == l;
            
            LeshProgram l = new LeshProgram(gr);
         //  Perfomance.ComputeTime(
            l.Run();//, "");
            l.Run();//, "");
            l.Run();//, "");
            l.Run();//, "");
          
            //gr.Test();
          //  MethodExpressionsExperiments();
            //RegexTestManymatches();
        }

        private static void MethodExpressionsExperiments()
        {
            var s1 = Expression.Parameter(typeof (object), "s1");
            LambdaExpression fExpres =
                Expression.Lambda(
                    Expression.Equal(s1, Expression.Constant(1.0, typeof(object))),
                    new[] {s1});

            Console.WriteLine(fExpres.ToString());
            string p = "1.0";
            Console.WriteLine(fExpres.Compile().DynamicInvoke(p));
            Console.WriteLine(p);
        }

        public static void RegexTest()
        {
            Regex r1 = new Regex("(?<s>(?>^[^<>]*?(((?'Open'<)[^<>]*?)+?((?'Close-Open'>)[^<>]*?)+?)*?(?(Open)(?!))$))");
            Regex r2 = new Regex("(?<s>^[^<>]*?(((?'Open'<)[^<>]*?)+?((?'Close-Open'>)[^<>]*?)+?)*?(?(Open)(?!))$)");
            Regex r3 = new Regex("(?<s>^[^<>]*(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*(?(Open)(?!))$)");
            Regex r4 = new Regex("(?<s>(?>^[^<>]*(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*(?(Open)(?!))$))");
            var s =
                @"optional { filter (?person=piu_20080<9051791 || 				?person=""svet_100616111408_10844"" || (?person=""pavl_100531115859_2020""||                ?person=""pavl_100531115859_6952"")|| ?person=""svet_100616111408_10864""|| ?person=""w20090506_svetlana_5727"" || ?person=""piu_200809051742"")  ?person http://fogid.net/o/name ?personName. ?s http://fogid.net/o/participant ?person. ?s http://fogid.net/o/in-org ?inorg. <?s <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> http://fogid.net/o/participation. ?inorg http://fogid.net/o/name ?orgname. optional {?s http://fogid.net/o/from-date ?fd}optional {?s http://fogid.net/o/to-date ?td} optional {?s http://fogid.net/o/role ?ro} filter (( bound(?fd) && bound(?td)) && - ?fd + ?td => -(- 1*10+50/10))filter ( lang(?personName)=?personNameLang)filter ( !langMatches(?personNameLang, ""*"") || langMatches(?personNameLang, ""ru"") || langMatches(lang(?personName), ""ru-ru"") >|| langMatches(lang(?personName), ""RU"")|| langMatches(lang(?personName), ""RU-ru""))}<<<<>>>>";
            for (int i = 0; i < 10; i++)
            {
                s += s;
            }
            var sl = s.Length;
            GetValue(r1, s, "r4");

        }

        private static void GetValue(Regex r1, string s, string mesage)
        {
            Perfomance.ComputeTime(() =>
            {
                var m1 = r1.Match(s);
                if (m1.Success)
                {
                    var ss = m1.Groups["s"].Value;
                    var ssl = ss.Length;
                }
            }, mesage, true);
        }

        public static void RegexTestManymatches()
        {
            Regex QuerySelect = new Regex(@"^\s*(\?(?<p>\S*)\s*)*$");
            string s = @"?personName ?fd ?td tttttttttttt ?orgname ?rom ";
            //while (s.Length<1000000)
            //{
            //    s += s;
            //}
            //var t = s.Length;
            //Perfomance.ComputeTime(() =>
            //{
            //    int i =0;
            //    foreach (var cap in QuerySelect.Match(s).Groups["p"].Captures)
            //    {
            //        i++;
            //    }
            //    var ii=i;
            //},"captures ", true);

            //Regex qs2 = new Regex(@"^\s*\?(?<p>\S*)");
            //string sc = s.Clone() as string;
            //Perfomance.ComputeTime(() =>
            //{
            //    Match m;
            //    int i = 0;
            //    while ((m = qs2.Match(sc)).Success)
            //    {
            //        var cap = m.Groups["p"].Value;
            //        i++;
            //        sc = sc.Remove(0, m.Length);
            //    }
            //    int ii = i;
            //}, "while remove by one ", true);
            Regex qs3 = new Regex(@"\G\s*\?\S*");
            int iii = 0;
            Perfomance.ComputeTime(() => qs3.Replace(s, (m) => 
            { var cap = m.Value;
                iii++;
                return string.Empty; }), "replace ", true);
        }
    }
}
=======
﻿using System;
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
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
