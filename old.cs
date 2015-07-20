using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal class old
    {
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
            Regex r2 = new Regex(@"^(?<s>^[^()]*?(((?'Open'\()[^()]*?)+?((?'Close-Open'\))[^()]*?)+?)*?(?(Open)(?!))$)");
            Regex r3 = new Regex("(?<s>^[^<>]*(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*(?(Open)(?!))$)");
            Regex r4 = new Regex("(?<s>(?>^[^<>]*(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*(?(Open)(?!))$))");
            var s = @"optional { filter (?person=piu_20080<9051791 || 				?person=""svet_100616111408_10844"" || (?person=""pavl_100531115859_2020""||                ?person=""pavl_100531115859_6952"")|| ?person=""svet_100616111408_10864""|| ?person=""w20090506_svetlana_5727"" || ?person=""piu_200809051742"")  ?person http://fogid.net/o/name ?personName. ?s http://fogid.net/o/participant ?person. ?s http://fogid.net/o/in-org ?inorg. <?s <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> http://fogid.net/o/participation. ?inorg http://fogid.net/o/name ?orgname. optional {?s http://fogid.net/o/from-date ?fd}optional {?s http://fogid.net/o/to-date ?td} optional {?s http://fogid.net/o/role ?ro} filter (( bound(?fd) && bound(?td)) && - ?fd + ?td => -(- 1*10+50/10))filter ( lang(?personName)=?personNameLang)filter ( !langMatches(?personNameLang, ""*"") || langMatches(?personNameLang, ""ru"") || langMatches(lang(?personName), ""ru-ru"") >|| langMatches(lang(?personName), ""RU"")|| langMatches(lang(?personName), ""RU-ru""))}<<<<>>>>";
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
            while (s.Length < 1000000)
            {
                s += s;
            }
            // var t = s.Length;
            //Perfomance.ComputeTime(() =>
            //{
            //    int i =0;
            //    foreach (var cap in QuerySelect.Match(s).Groups["p"].Captures)
            //    {
            //        i++;
            //    }
            //    var ii=i;
            //},"captures ", true);

            Regex qs2 = new Regex(@"(\s*\?\S*)");
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
            //Regex qs3 = new Regex(@"\G\s*\?\S*");
            int iii = 0;
            Perfomance.ComputeTime(() => qs2.Replace(s, (m) =>
            {
                var cap = m.Value;
                iii++;
                return String.Empty;
            }), "replace ");
            int ii = 0;
            Perfomance.ComputeTime(() =>
            {
                var ss=qs2.Split(s);
                for (int i = 0; i < ss.Length; i++)
                    ii++;
            }, "split ");
        }

        private static 
            void TestSplit()
        {
            var pattern =
                @"^(?<left>(\(\s*(?<insideLeft>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\))|\S.*)\s*(?<center>\|\||&&)\s*";
            string s =@"(?person=piu_200809051791 || 
                                ?person=ns:svet_100616111408_10844 ||
                (?person=ns:pavl_100531115859_2020||                
                ?person=pavl_100531115859_6952))||
                ?person=""svet_100616111408_10864""||
                ?person=""w20090506_svetlana_5727"" ||
                ?person=""piu_200809051742";
            Regex r2 = new Regex(@"^(?<s>^[^()]*(((?'Open'\()[^()]*)+((?'Close-Open'\))[^()]*)+)*(?(Open)(?!)))");
            Perfomance.ComputeTime(() =>
            {
                if (s.StartsWith("("))
                    Console.WriteLine(s.Substring(0, CloseBrecketIndex(s)));
            }, "for deep ");
            Perfomance.ComputeTime(() =>
            {
                Match m = r2.Match(s.Substring(1));
                if (m.Success)
                    Console.WriteLine(m.Groups["s"].Value);
            }, "reg ");
        }

        private static int CloseBrecketIndex(string s)
        {
            var ss = s.ToCharArray();
            int deep = 0;
            char c;
            const char close = ')';
            const char open = '(';
            for (int j = 0; j < ss.Length; j++)
            {
                if ((c=ss[j]) == close)
                    if (--deep == 0) return j;
                if (c == open) deep++;
                
            }
            throw new Exception(s+" can't close bracket");
        }
    }
}