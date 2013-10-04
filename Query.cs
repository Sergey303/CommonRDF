using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal class Query : SparqlChain
    {
        public GraphBase Gr;
       // public List<QueryTripletOptional> Optionals;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        private readonly TValue[] parameters;
        public TValue[] ParametersWithMultiValues;
        public readonly List<string> SelectParameters=new List<string>();
        public readonly List<string[]> ParametrsValuesList = new List<string[]>();

        #region Read

        public Query(StreamReader stream, GraphBase graph):this(stream.ReadToEnd(), graph) { }
        public Query(string sparqlString, GraphBase graph)
        {
            var valuesByName = new Dictionary<string, TValue>();

            var selectMatch = Reg.QuerySelect.Match(sparqlString);
            if (selectMatch.Success)
            {
                string parameters2Select = selectMatch.Groups[1].Value.Trim();
                if (parameters2Select != "*")
                    SelectParameters =
                        parameters2Select.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
               sparqlString = sparqlString.Replace(selectMatch.Groups[0].Value, "");
            }
            var whereMatch = Reg.QueryWhere.Match(sparqlString);
            if (whereMatch.Success)
            {
                string tripletsGroup = whereMatch.Groups[1].Value;
                SparqlTriplet.Gr = Gr = graph;
                Match tripletMatch;
                while (tripletsGroup!=string.Empty)
                {
                    if ((tripletMatch = Reg.Triplet.Match(tripletsGroup)).Success)
                        CreateTriplet(tripletMatch.Groups[1].Value,
                            tripletMatch.Groups[2].Value,
                            tripletMatch.Groups[3].Value, valuesByName, false);
                    else if ((tripletMatch = Reg.TripletOptional.Match(tripletsGroup)).Success)
                        CreateTriplet(tripletMatch.Groups[1].Value,
                            tripletMatch.Groups[2].Value,
                            tripletMatch.Groups[3].Value, valuesByName, true);
                    else if ((tripletMatch=Reg.Filter.Match(tripletsGroup)).Success)
                    {
                        var filter = tripletMatch.Groups["filter"].Value;
                        var filterType = tripletMatch.Groups[1].Value.ToLower();
                        if (filterType == "regex")
                        {
                            var newFilter = new SparqlFilterRegex(filter);
                            if (!valuesByName.TryGetValue(newFilter.ParameterName, out newFilter.Parameter))
                            {
                                valuesByName.Add(newFilter.ParameterName, newFilter.Parameter = new TValue());
                                throw new NotImplementedException("new parameter in fiter regex");
                            }
                            Add(newFilter);
                        }
                        else // common filter
                        {
                            this.CreateFilterChain(filter, valuesByName);
                        }
                    }
                    else throw new Exception("strange query triplet: " + tripletMatch.Value);
                    tripletsGroup=tripletsGroup.Remove(0, tripletMatch.Length);
                }
                NextMatch = Last;
            }
          //  sparqlString = sparqlString.Replace(whereMatch.Groups[0].Value, "");
            //QueryTriplet.Gr = Gr;
            //QueryTriplet.Match = Match;
            //QueryTripletOptional.Gr = Gr;
            //QueryTripletOptional.MatchOptional = MatchOptional;
            parameters = valuesByName.Values.Where(v => v.Value == null).ToArray();
            ParametersNames = valuesByName.Where(v => v.Value.Value == null).Select(kv => kv.Key).ToArray();
        }

        private void CreateTriplet(string sValue, string pValue, string oValue, Dictionary<string, TValue> valuesByName, bool isOptional)
        {
            bool isData;
            TValue s, p, o;
            bool isNewS = TestParameter(sValue.TrimStart('<').TrimEnd('>'),
                out s, valuesByName);
            bool isNewP = TestParameter(pValue.TrimStart('<').TrimEnd('>'),
                out p, valuesByName);
            bool isNewO = TestParameter((isData = oValue.StartsWith("'"))
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
