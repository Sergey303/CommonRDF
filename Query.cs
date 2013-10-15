using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal class Query : SparqlChainParametred
    {
        public GraphBase Gr;
       // public List<QueryTripletOptional> Optionals;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        private readonly TValue[] parameters;
        public TValue[] ParametersWithMultiValues;
        public readonly string[] SelectParameters = new string[0];
        public readonly List<string[]> ParametrsValuesList = new List<string[]>();


        #region Read

        public Query(StreamReader stream, GraphBase graph):this(stream.ReadToEnd(), graph)
        {
        }

        public Query(string sparqlString, GraphBase graph)
        {
            sparqlString = Reg.QueryPrefix.Replace(sparqlString, prefixMatch =>
            {
                prefixes.Add(prefixMatch.Groups[1].Value, prefixMatch.Groups[2].Value);
                return string.Empty;
            });

            var selectMatch = Reg.QuerySelect.Match(sparqlString);
            if (selectMatch.Success)
                if (selectMatch.Groups[1].Value != "*")
                {
                    CaptureCollection captureCollection = selectMatch.Groups["p"].Captures;
                    SelectParameters = new string[captureCollection.Count];
                    for (int i = 0; i < SelectParameters.Length; i++)
                        SelectParameters[i] = captureCollection[i].Value;
                    sparqlString = sparqlString.Remove(0, selectMatch.Length);
                }

            var whereMatch = Reg.QueryWhere.Match(sparqlString);
            if (whereMatch.Success)
            {
                
                SparqlTriplet.Gr = Gr = graph;
                ParseWherePattern(whereMatch.Groups["insideWhere"].Value, false);
                NextMatch = Last;
            }
            parameters = valuesByName.Values.Where(v => v.Value == string.Empty).ToArray();
            ParametersNames = valuesByName.Where(v => v.Value.Value == string.Empty).Select(kv => kv.Key).ToArray();
        }

        private void ParseWherePattern(string input, bool isOptionals)
        {
            while (!string.IsNullOrWhiteSpace(input))
            {
                Match m;
                if ((m = Reg.Triplet.Match(input)).Success)
                {
                    CreateTriplet(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, isOptionals);
                    return;
                }
                input = Reg.TripletDot.Replace(input, m1 =>
                {
                    int count = m1.Groups[1].Captures.Count;
                    for (int i = 0; i < count; i++)
                        CreateTriplet(m1.Groups[2].Captures[i].Value, m1.Groups[3].Captures[i].Value,
                            m1.Groups[4].Captures[i].Value, isOptionals);
                    return string.Empty;
                });
                if (!isOptionals) //optional insidde opional
                    input = Reg.TripletOptional.Replace(input, m1 =>
                    {
                        ParseWherePattern(m1.Groups["inside"].Value, true);
                        return string.Empty;
                    });
                input = Reg.Filter.Replace(input, m1 =>
                {
                    var filter = m1.Groups["filter"].Value;
                    var filterType = m1.Groups[1].Value.ToLower();
                    if (filterType == "regex")
                    {
                        var newFilter = new SparqlFilterRegex(filter);
                        if (!valuesByName.TryGetValue(newFilter.ParameterName, out newFilter.Parameter))
                        {
                            valuesByName.Add(newFilter.ParameterName, newFilter.Parameter = new TValue());
                            throw new NotImplementedException("new parameter in filter regex");
                        }
                        Add(newFilter);
                    }
                    else // common filter
                    {
                        this.AndOrExpression(filter, isOptionals, false);
                    }
                    return string.Empty;
                });
            }
        }


        private void CreateTriplet(string sValue, string pValue, string oValue, bool isOptional)
        {
            bool isData=true;
            TValue s, p, o;
            if (pValue == "a") pValue = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
            bool isNewS = TestParameter(ReplaceNamespacePrefix(sValue), out s);
            bool isNewP = TestParameter(ReplaceNamespacePrefix(pValue), out p);
            bool isNewO = TestParameter(TestDataConst(oValue, ref isData), out o);

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
                p.SyncIsObjectRole(o);
            if (!isNewP)
                if (!isNewS)
                    if (!isNewO)
                        if (isOptional) return;
                        else Add(new SampleTriplet(s, p, o));
                    else if (isOptional) Add(new SelectObjectOprtional(s, p, o));
                    else Add(new SelectObject(s, p, o));
                else if (!isNewO)
                    Add(isOptional
                        ? new SelectSubjectOpional(s, p, o)
                        : new SelectSubject(s, p, o));
                else if (isOptional) Add(new SelectAllSubjectsOptional(s), new SelectObjectOprtional(s, p, o));
                else Add(new SelectAllSubjects(s), new SelectObject(s, p, o));
            else if (!isNewS)
            {
                if (!isNewO) Add( new SelectPredicate(s, p, o));
                else
                {
                    //Action = SelectPredicateObject;
                }
            }
            else if (!isNewO)
            {
                //Action = SelectPredicateSubject;
            }
            else
            {
                //Action = SelectAll;
            }
        }

        private readonly SparqlChainParametred sparqlChainParametred;

        #endregion


        #region Run

        


        private bool Last()
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
