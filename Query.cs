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
       // public List<QueryTripletOptional> Optionals;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        private readonly TValue[] parameters;
        public TValue[] ParametersWithMultiValues;
        public List<string> SelectParameters;
        private readonly List<SparqlTriplet> triplets;
        public readonly List<string[]> ParametrsValuesList;

        #region Read

        public Query(string filePath, GraphBase graph)
        {
            ParametrsValuesList = new List<string[]>();
           SelectParameters = new List<string>();
            triplets = new List<SparqlTriplet>();
            var valuesByName = new Dictionary<string, TValue>();
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
                    SparqlTriplet newTriplet = null, lastTriplet = null;
                    SparqlTriplet.Gr = Gr=graph;
                    foreach (Match tripletMatch in TripletsReg.Matches(tripletsGroup))
                    {
                        var sMatch = tripletMatch.Groups["s"];
                        string pValue;
                        bool isOptional = false, isData;
                        string oValue;
                        if (sMatch.Success)
                        {
                            pValue = tripletMatch.Groups["p"].Value;
                            oValue = tripletMatch.Groups["o"].Value;

                        }
                        else if ((sMatch = tripletMatch.Groups["os"]).Success)
                        {
                            pValue = tripletMatch.Groups["op"].Value;
                            oValue = tripletMatch.Groups["oo"].Value;
                            isOptional = true;
                        }
                        else throw new Exception("strange query triplet: " + tripletMatch.Value);


                        TValue s, p, o;
                           string sValue = sMatch.Value.TrimStart('<').TrimEnd('>');
                        bool isNewS = TestParameter(sValue,
                            out s, valuesByName);
                        bool isNewP = TestParameter(pValue = pValue.TrimStart('<').TrimEnd('>'),
                            out p, valuesByName);
                        bool isNewO = TestParameter(oValue = (isData = oValue.StartsWith("'"))
                            ? oValue.Trim('\'')
                            : oValue.TrimStart('<').TrimEnd('>'), out o, valuesByName);

                        s.SetTargetType(true);
                        if (isData)
                        {
                            o.SetTargetType(false);
                            p.SetTargetType(false);
                        }
                        else if (!isNewO)
                        {
                            o.SetTargetType(true);
                            p.SetTargetType(true);
                        }
                        else
                        {
                           p.SyncIsObjectRole(o);
                        }
                        if (!isNewP)
                            if (!isNewS)
                            {
                                newTriplet = !isNewO
                                    ? (SparqlTriplet)
                                        new SampleTriplet { S = s, P = p, O = o, IsOption = isOptional,
                                            HasNodeInfoS = s.SetNodeInfo }
                                    : new SelectObject { S = s, P = p, O = o, IsOption = isOptional,
                                        HasNodeInfoS = s.SetNodeInfo };
                                s.SetNodeInfo = true;
                            }
                            else if (!isNewO)
                            {
                                newTriplet = new SelectSubject
                                {
                                    S = s,
                                    P = p,
                                    O = o,
                                    IsOption = isOptional,
                                    HasNodeInfoO = o.SetNodeInfo
                                };
                                o.SetNodeInfo = true;
                            }
                            else
                            {
                                newTriplet = new SelectAllSubjects { S = s, P = p, O = o, IsOption = isOptional,
                                    NextMatch = new SelectObject { S = s, P = p,O = o, IsOption = isOptional,
                                                    HasNodeInfoS = s.SetNodeInfo }.Match };
                                s.SetNodeInfo = true;
                            }
                        else if (!isNewS)
                            if (!isNewO) newTriplet = new SelectPredicate();
                            else
                            {
                                //Action = SelectPredicateObject;
                                
                            }
                        else if (!isNewO)
                        {
                            //Action = SelectPredicateSubject;
                        }
                        else
                        {
                            //Action = SelectAll;
                        }
                        var isObj = p.IsObject ?? o.IsObject;
                        if (isObj != null)
                            newTriplet.IsNotDataRole = newTriplet.IsObjectRole = isObj.Value;
                        if (lastTriplet != null)
                            lastTriplet.NextMatch = newTriplet.Match;
                        lastTriplet = newTriplet;
                            triplets.Add(newTriplet);
                    }
                    if (lastTriplet != null)
                        lastTriplet.NextMatch = Match;
                }
            }
            //QueryTriplet.Gr = Gr;
            //QueryTriplet.Match = Match;
            //QueryTripletOptional.Gr = Gr;
            //QueryTripletOptional.MatchOptional = MatchOptional;
            parameters = valuesByName.Values.Where(v=>v.Value==null).ToArray();
            ParametersNames = valuesByName.Where(v => v.Value.Value == null).Select(kv=>kv.Key).ToArray();
        }

        private static bool TestParameter(string spoValue, out TValue spo, Dictionary<string, TValue> paramByName)
        {
            if (!spoValue.StartsWith("?"))
            {
                if (!paramByName.TryGetValue(spoValue, out spo))
                    paramByName.Add(spoValue, spo = new TValue { Value = spoValue });
            }
            else
            {
                if (paramByName.TryGetValue(spoValue, out spo))
                    return false;
                paramByName.Add(spoValue, spo = new TValue());
                return true;
            }
            return false;
        }

        #endregion


        #region Run

        public bool Run()
        {
            return triplets.Count != 0 && triplets[0].Match();
        }


        private bool Match()
        {
            ParametrsValuesList.Add(parameters.Select(par => par.Value).ToArray());
            return true;
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
