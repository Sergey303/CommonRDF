using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using sema2012m;

namespace CommonRDF
{
    internal class Query
    {
        public static Regex QuerySelectReg = new Regex(@"select\s+(?<selectGroups>((\?\w+\s+)+|\*))",
            RegexOptions.Compiled);

        public static Regex QueryWhereReg = new Regex(@"where\s+\{(?<whereGroups>([^{}]*\{[^{}]*\}[^{}]*)*|[^{}]*)\}",
            RegexOptions.Compiled);

        public static Regex TripletsReg = new Regex(
            @"((?<s>[^\s]+|'.*')\s+(?<p>[^\s]+|'.*')\s+(?<o>[^\s]+|'.*')\.(\s|$))|optional\s+{\s*(?<os>[^\s]+|'.*')\s+(?<op>[^\s]+|'.*')\s+(?<oo>[^\s]+|'.*')\s*}(\s|$)"
            );

        public List<string> FiterList;
        public GraphBase Gr;
        public List<QueryTripletOptional> Optionals;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        private readonly TValue[] parameters;
        public TValue[] ParametersWithMultiValues;
        public List<string> SelectParameters;
        private readonly List<QueryTriplet> triplets;
        public readonly List<string[]> ParametrsValuesList;

        #region Read

        public Query(string filePath, GraphBase graph)
        {
           // ParametrsValuesList = new List<string[]>();
           //// Gr = graph;
           // SelectParameters = new List<string>();
           // //   var parameterTests = new Dictionary<TValue, List<QueryTriplet>>();
           // // var parametesWithMultiValues = new HashSet<TValue>();
           // triplets = new List<QueryTriplet>();
           // Optionals = new List<QueryTripletOptional>();
            //var paramByName = new Dictionary<string, TValue>();
            //var optParamHasValues = new HashSet<string>();
            //var constsByValue = new Dictionary<string, TValue>();
            using (var f = new StreamReader(filePath))
            {
                var qs = f.ReadToEnd();
                var selectMatch = QuerySelectReg.Match(qs);
                if (selectMatch.Success)
                {
                    string parameters2Select = selectMatch.Groups["selectGroups"].Value.Trim();
                    if (parameters2Select != "*")
                        SelectParameters =
                            parameters2Select.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                var whereMatch = QueryWhereReg.Match(qs);
                if (whereMatch.Success)
                {
                    string tripletsGroup = whereMatch.Groups["whereGroups"].Value;
                    foreach (Match tripletMatch in TripletsReg.Matches(tripletsGroup))
                    {
                        //var sMatch = tripletMatch.Groups["s"];
                        //string pValue;
                        //bool isOptional = false, isData;
                        //string oValue;
                        //if (sMatch.Success)
                        //{
                        //    pValue = tripletMatch.Groups["p"].Value;
                        //    oValue = tripletMatch.Groups["o"].Value;

                        //}
                        //else if ((sMatch = tripletMatch.Groups["os"]).Success)
                        //{
                        //    pValue = tripletMatch.Groups["op"].Value;
                        //    oValue = tripletMatch.Groups["oo"].Value;
                        //    isOptional = true;
                        //}
                        //else throw new Exception("strange query triplet: " + tripletMatch.Value);

                       
                     //   TValue s, p, o;
                     ////   string sValue = sMatch.Value.TrimStart('<').TrimEnd('>');
                     //   bool isNewS = TestParameter("",
                     //       out s, constsByValue, paramByName);
                     //   bool isNewP = TestParameter("",
                     //       out p, constsByValue, paramByName);
                     //   bool isNewO = TestParameter("", out o, constsByValue, paramByName);

                        //s.SetTargetType(true);
                        //if (isData1)
                        //{
                        //    o.SetTargetType(false);
                        //    p.SetTargetType(false);
                        //}
                        //else if (!isNewO)
                        //{
                        //    o.SetTargetType(true);
                        //    p.SetTargetType(true);
                        //}
                        //else
                        //{
                        //    if (p.IsObj != null)
                        //        o.SetTargetType(p.IsObj.Value);
                        //    else if (o.IsObj !=null)
                        //        p.SetTargetType(o.IsObj.Value);
                        //    else //both unkown
                        //    {
                        //        p.SubscribeIsObjSetted(o);
                        //        o.SubscribeIsObjSetted(p);
                        //    }
                        //}
                        //if (isOptional)
                        //    Optionals.Add(new QueryTripletOptional(isNewS, isNewP, isNewO,
                        //        s, p, o,
                        //        HasOpt(isNewS, optParamHasValues, sValue),
                        //        HasOpt(isNewP, optParamHasValues, pValue1),
                        //        HasOpt(isNewO, optParamHasValues, oValue1)));
                        //else
                        //    triplets.Add(new QueryTriplet(isNewS, isNewP, isNewO,
                        //        s, p, o));
                    }
                }
            }
            //QueryTriplet.Gr = Gr;
            //QueryTriplet.Match = Match;
            //QueryTripletOptional.Gr = Gr;
            //QueryTripletOptional.MatchOptional = MatchOptional;
            //parameters = paramByName.Values.ToArray();
            //ParametersNames = paramByName.Keys.ToArray();
        }

        private static bool HasOpt(bool isNew, HashSet<string> optParamHasValues, string spoValue)
        {
            bool hasOptS;
            if (isNew)
            {
                hasOptS = false;
                optParamHasValues.Add(spoValue);
            }
            else
                hasOptS = optParamHasValues.Contains(spoValue);
            return hasOptS;
        }

        private static bool TestParameter(string spoValue, out TValue spo,
            Dictionary<string, TValue> constsByValue, Dictionary<string, TValue> paramByName)
        {
            //if (!spoValue.StartsWith("?"))
            //{
            //    if (!constsByValue.TryGetValue(spoValue, out spo))
            //        constsByValue.Add(spoValue, spo = new TValue {Value = spoValue});
            //}
            //else
            //{
            //    if (paramByName.TryGetValue(spoValue, out spo))
            //        return false;
            //    paramByName.Add(spoValue, spo = new TValue());
            //    return true;
            //}
            spo = null;
            return false;
        }

        #endregion


        #region Run

        public void Run()
        {
            Match(0);
        }


        private void Match(int i)
        {
            if (i == triplets.Count)
            {
                MatchOptional(0);
                return;
            }
            triplets[i].Action(i + 1);
        }

       


        private void MatchOptional(int i)
        {
            if (i == Optionals.Count)
                ParametrsValuesList.Add(parameters.Select(par => par.Value).ToArray());
            else
            {
             Optionals[i].Action(i+1);
            }
        }

       

        #endregion
        
        #region Output in file

        internal void OutputParamsAll(string outPath)
        {
            using (var io = new StreamWriter(outPath, true))
                foreach (var parametrsValues in ParametrsValuesList)
                {
                    for (int i = 0; i < parametrsValues.Length; i++)
                    {
                        io.WriteLine("{0} {1}", ParametersNames[i], parametrsValues[i]);
                    }
                    io.WriteLine();
                }
        }

        internal void OutputParamsBySelect(string outPath)
        {
            var parametrsValuesIndexes = ParametersNames
                .Select((e, i) => new { e, i });
            using (var io = new StreamWriter(outPath, true, Encoding.UTF8))
                foreach (var parametrsValues in ParametrsValuesList)
                {
                    foreach (var i in SelectParameters
                        .Select(p => parametrsValuesIndexes.First(e => e.e == p)))
                    {
                        io.WriteLine("{0}", parametrsValues[i.i]);
                    }
                    io.WriteLine();
                }
        }

        #endregion
    }
}
