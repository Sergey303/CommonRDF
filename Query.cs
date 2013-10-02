using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using sema2012m;

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

            var selectMatch = Re.QuerySelectReg.Match(sparqlString);
            if (selectMatch.Success)
            {
                string parameters2Select = selectMatch.Groups[1].Value.Trim();
                if (parameters2Select != "*")
                    SelectParameters =
                        parameters2Select.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
               sparqlString = sparqlString.Replace(selectMatch.Groups[0].Value, "");
            }
            var whereMatch = Re.QueryWhereReg.Match(sparqlString);
            if (whereMatch.Success)
            {
                string tripletsGroup = whereMatch.Groups[1].Value;
                SparqlBase lastTriplet = null;
                SparqlTriplet.Gr = Gr = graph;
                foreach (Match tripletMatch in Re.TripletsReg.Matches(tripletsGroup))
                {
                    var sMatch = tripletMatch.Groups[2];
                    string pValue;
                    string oValue;
                    if (sMatch.Success)
                    {
                        pValue = tripletMatch.Groups[3].Value;
                        oValue = tripletMatch.Groups[4].Value;
                        lastTriplet = CreateTriplet(sMatch, valuesByName, pValue, oValue, false, lastTriplet);
                    }
                    else if ((sMatch = tripletMatch.Groups[7]).Success)
                    {
                        pValue = tripletMatch.Groups[8].Value;
                        oValue = tripletMatch.Groups[9].Value;
                        lastTriplet = CreateTriplet(sMatch, valuesByName, pValue, oValue, true, lastTriplet);
                    }
                    else if ((sMatch = tripletMatch.Groups[12]).Success)
                    {
                        var filter = sMatch.Value;
                        var filterType = tripletMatch.Groups[11].Value.ToLower();
                        if (filterType == "regex")
                        {
                            var newFilter = new SparqlFilterRegex(filter, lastTriplet);
                            if (!valuesByName.TryGetValue(newFilter.ParameterName, out newFilter.Parameter))
                            {
                                valuesByName.Add(newFilter.ParameterName, newFilter.Parameter = new TValue());
                                throw new NotImplementedException("new parameter in fiter regex");
                            }
                            lastTriplet = newFilter;
                        }
                        else// common filter
                        {
                            lastTriplet = FilterFunctions.Create(filter, valuesByName, lastTriplet);
                        }
                    }
                    else throw new Exception("strange query triplet: " + tripletMatch.Value);



                    
                    if(start==null) start = lastTriplet.Match;
                }
                if (lastTriplet != null)
                    lastTriplet.NextMatch = Last;
            }
          //  sparqlString = sparqlString.Replace(whereMatch.Groups[0].Value, "");
            //QueryTriplet.Gr = Gr;
            //QueryTriplet.Match = Match;
            //QueryTripletOptional.Gr = Gr;
            //QueryTripletOptional.MatchOptional = MatchOptional;
            parameters = valuesByName.Values.Where(v => v.Value == null).ToArray();
            ParametersNames = valuesByName.Where(v => v.Value.Value == null).Select(kv => kv.Key).ToArray();
        }

        private static SparqlBase CreateTriplet(Group sMatch, Dictionary<string, TValue> valuesByName, string pValue, string oValue, bool isOptional, SparqlBase lastTriplet)
        {
            bool isData;
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
                p.SyncIsObjectRole(o);
            if (!isNewP)
                return !isNewS
                    ? (!isNewO
                        ? (isOptional ? lastTriplet : new SampleTriplet(s, p, o, lastTriplet))
                        : (isOptional
                            ? new SelectObjectOprtional(s, p, o, lastTriplet)
                            : new SelectObject(s, p, o, lastTriplet)))
                    : (!isNewO
                        ? (SparqlBase) (isOptional
                            ? new SelectSubjectOpional(s, p, o, lastTriplet)
                            : new SelectSubject(s, p, o, lastTriplet))
                        : (isOptional
                            ? new SelectObjectOprtional(s, p, o,new SelectAllSubjectsOptional(s,  lastTriplet))
                            : new SelectObject(s, p, o,new SelectAllSubjects(s,  lastTriplet))));
            else if (!isNewS)
            {
                if (!isNewO) return new SelectPredicate(s, p, o, lastTriplet);
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
            return null;
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
