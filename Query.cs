
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
        public readonly string[] ParametersNames;
      public readonly string[] SelectParameters;
        public readonly List<Dictionary<string, string>> ParametrsValuesList = new List<Dictionary<string, string>>();
        private int limit = -1, offset = -1;
        private Delegate orderBy;
        private string[] results;
        private readonly StartNode start;
        private readonly bool describe;
        private readonly List<Func<Dictionary<string, string>, string>> construct;
        #region Read

        public Query(StreamReader stream, GraphBase graph):this(stream.ReadToEnd(), graph)
        {
        }

        public Query(string sparqlString, GraphBase graph)
        {
             
            sparqlString = sparqlString.TrimStart();
            var prefixesMatch = Reg.QueryPrefix.Match(sparqlString);
            for (int i = 0; i < prefixesMatch.Groups["shortName"].Captures.Count; i++)
                prefixes.Add(prefixesMatch.Groups["shortName"].Captures[i].Value, prefixesMatch.Groups["url"].Captures[i].Value);
            sparqlString = sparqlString.Remove(0, prefixesMatch.Length).TrimStart();
            var m = Reg.QuerySelect.Match(sparqlString);
            if (m.Success)
            {
                isDistinct = m.Groups["dist"].Success;
                isReduce = m.Groups["red"].Success;
                describe = m.Groups["descr"].Success;
                var captureCollection = m.Groups["p"].Captures;
                if (!m.Groups["all"].Success)
                    SelectParameters = new string[captureCollection.Count];
                for (int i = 0; i < SelectParameters.Length; i++)
                    SelectParameters[i] = captureCollection[i].Value;
                sparqlString = sparqlString.Remove(0, m.Length).TrimStart();
            }
            else if ((m = Reg.QueryConstruct.Match(sparqlString)).Success)
            {
                construct = new List<Func<Dictionary<string, string>, string>>
                {
                    CreateConstructTriplet(m.Groups["firstS"].Value, m.Groups["firstP"].Value, m.Groups["firstO"].Value)
                };

                for (int i = 0; i < m.Groups["s"].Captures.Count; i++)
                    construct.Add(CreateConstructTriplet(m.Groups["s"].Captures[i].Value,
                        m.Groups["p"].Captures[i].Value, m.Groups["o"].Captures[i].Value));
                sparqlString = sparqlString.Remove(0, m.Length).TrimStart();
            }
             m = Reg.QueryWhere.Match(sparqlString);
            start = new StartNode();
            if (m.Success)
            {
                SparqlNodeBase.Gr = Gr = graph;
               var ends= ParseWherePattern(start, m.Groups["insideWhere"].Value, false);
                foreach (var end in ends)
                    end.NextMatch = Last;
                sparqlString = sparqlString.Remove(0, m.Length).TrimStart();
            }
            if ((m = Reg.OrderClause.Match(sparqlString)).Success)
            {
                var orders = new Func<IEnumerable<Dictionary<string, string>>, IEnumerable<Dictionary<string, string>>>[m.Groups["query"].Captures.Count];
                for (int i = 0; i < m.Groups["query"].Captures.Count; i++)
                {
                    var orderExpr = m.Groups["query"].Captures[i].Value;
                    if (orderExpr.ToLower().StartsWith("desk"))
                    {
                        var paramName = orderExpr.Remove(0, 4).Trim();
                        if (paramName.StartsWith("xsd:double(str("))
                        {
                            paramName = paramName.Remove(0, 15).TrimEnd(' ', ')');
                            orders[i] = enumerable => enumerable.OrderByDescending(dictionary =>
                            {
                                double douleValue;
                                double.TryParse(dictionary[paramName].Replace(".", ","), out douleValue);
                                return douleValue;
                            });
                        }
                        else
                        orders[i]=enumerable => enumerable.OrderByDescending(dictionary => dictionary[paramName]);
                    }
                    else
                    {
                        string paramName = orderExpr.ToLower().StartsWith("ask") ? orderExpr.Remove(0, 3).Trim() : orderExpr.Trim();
                        if (paramName.StartsWith("xsd:double(str("))
                        {
                            paramName = paramName.Remove(0, 15).TrimEnd(' ', ')');
                            orders[i] = enumerable => enumerable.OrderBy(dictionary =>
                            {
                                double douleValue;
                                double.TryParse(dictionary[paramName].Replace(".", ","), out douleValue);
                                return douleValue;
                            });
                        }
                        else
                        orders[i]=enumerable => enumerable.OrderBy(dictionary => dictionary[paramName]);
                    }
                }
                orderBy = Delegate.Combine(orders);
                sparqlString = sparqlString.Remove(0, m.Length).TrimStart();
            }
            sparqlString = sparqlString.ToLower();
            if ((m = Reg.Offset.Match(sparqlString)).Success && int.TryParse(m.Groups["count"].Value, out offset))
                sparqlString = sparqlString.Remove(0, m.Length);
            if ((m = Reg.Limit.Match(sparqlString)).Success && int.TryParse(m.Groups["count"].Value, out limit)){}
              //  sparqlString = sparqlString.Remove(0, m.Length);
            ParametersNames = valuesByName.Where(v => v.Value.Value == string.Empty).Select(kv => kv.Key).ToArray();
        }

        private static Func<Dictionary<string, string>, string> CreateConstructTriplet(string s, string p, string o)
        {
            bool isSParam = s.StartsWith("?");
            bool isPParam = p.StartsWith("?");
            bool isSOaram = o.StartsWith("?");
            Func<Dictionary<string, string>, string> func;
            if (isSParam)
                if (isPParam)
                    if (isSOaram)
                        func = dictionary => dictionary[s] + " " + dictionary[p] + " " + dictionary[o];
                    else func = dictionary => dictionary[s] + " " + dictionary[p] + " " + o;
                else if (isSOaram)
                    func = dictionary => dictionary[s] + " " + p + " " + dictionary[o];
                else func = dictionary => dictionary[s] + " " + p + " " + o;
            else if (isPParam)
                if (isSOaram)
                    func = dictionary => s + " " + dictionary[p] + " " + dictionary[o];
                else func = dictionary => s + " " + dictionary[p] + " " + o;
            else if (isSOaram)
                func = dictionary => s + " " + p + " " + dictionary[o];
            else func = dictionary => s + " " + p + " " + o;
            return func;
        }

        protected IEnumerable<SparqlNodeBase>  ParseWherePattern(SparqlNodeBase root, string input, bool isOptionals)
        {
            if (string.IsNullOrWhiteSpace(input)) return new [] {root};
                 Match m = Reg.Filter.Match(input);
            if (m.Success)
            {
                input = input.Remove(0, m.Length);
                string filterType = m.Groups[1].Value.ToLower();
                if (filterType == "regex")
                {
                    var newFilter = new SparqlFilterRegex(m.Groups["filter"].Value);
                    if (!valuesByName.TryGetValue(newFilter.ParameterName, out newFilter.Parameter))
                    {
                        valuesByName.Add(newFilter.ParameterName, newFilter.Parameter = new SparqlVariable());
                        throw new NotImplementedException("new parameter in filter regex");
                    }
                   root.NextMatch= newFilter.Match;
                    return ParseWherePattern(newFilter, input, isOptionals);
                }
                if (filterType == "langmatches")
                {
                    var parametrs = m.Groups["filter"].Value.Split(',');
                    var next=this.LangMatch(false, isOptionals, parametrs[0].Trim(), parametrs[1].Trim());
                    root.NextMatch = next.Match;
                    return ParseWherePattern(next, input, isOptionals);
                }
                // common filter
                    return
                        this.AndOrExpression(root, m.Groups["filter"].Value, isOptionals, false)
                            .SelectMany(f => ParseWherePattern(f, input, isOptionals));
            }
            if ((m = Reg.Triplet.Match(input)).Success)

            {
                var next = CreateTriplet(root, m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, isOptionals);
                return new[] {next};
            }
            m = Reg.Union.Match(input);
            if (m.Success)
            {
                input = input.Remove(0, m.Length);
                var left = m.Groups["inside"].Value;
                var alternatives = m.Groups["insideAlter"].Captures.Cast<Capture>().Select(c => c.Value).ToArray();                
                var aLternativesChains = new ALternativesChains(left, (s, nodeStartAlter) => ParseWherePattern(nodeStartAlter,s, isOptionals), valuesByName, alternatives);
               root.NextMatch= aLternativesChains.Match;
                if (string.IsNullOrWhiteSpace(input)) return aLternativesChains.Ends;
                return aLternativesChains.Ends.SelectMany(last => ParseWherePattern(last, input, isOptionals));
            }
            m = Reg.TripletDot.Match(input);
            if (m.Success)
            {
                input = input.Remove(0, m.Length);
                SparqlNodeBase next = root;
                for (int i = 0; i < m.Groups[1].Captures.Count; i++)
                   next = CreateTriplet(next, m.Groups[2].Captures[i].Value, m.Groups[3].Captures[i].Value, m.Groups[4].Captures[i].Value, isOptionals);
                return ParseWherePattern(next,input, isOptionals);
            }
            // if (!isOptionals) //optional insidde opional
            m = Reg.TripletOptional.Match(input);
            if (m.Success)
            {
                input = input.Remove(0, m.Length);
                var copy = new Dictionary<string, SparqlVariable>(valuesByName);
                var optionalSparqlTriplets = ParseWherePattern(root, m.Groups["inside"].Value, true).Cast<OptionalSparqlTripletBase>().ToList();
                var newParameters=valuesByName.Where(pair => string.IsNullOrEmpty(pair.Value.Value) && !copy.ContainsKey(pair.Key)).Select(pair => pair.Value).ToArray();
                foreach (var sparqlNodeBase in optionalSparqlTriplets)
                    sparqlNodeBase.Parameters = newParameters;
                return
                    optionalSparqlTriplets.SelectMany(inside => ParseWherePattern(inside, input, false));
            }

           
            return null;
        }
        private readonly bool isReduce;
        private readonly bool isDistinct;

        #endregion


        #region Run

        public bool Match()
        {
            return start.Match();
        }

        private bool Last()
        {
            ParametrsValuesList.Add(ParametersNames.ToDictionary(pName => pName, pName=>valuesByName[pName].Value));
            return true;
        }

        #endregion
        
        #region Output in file

        public void Output(string outFilePath)
        {
            using (var io = new StreamWriter(outFilePath, true, Encoding.UTF8))
            {
                io.WriteLine("start");
                    foreach (var result in Results)
                        io.WriteLine(result);
                io.WriteLine("end");
                io.WriteLine();
            }
        }
       


        public string[] Results
        {
            get
            {
                if (results != null) return results;
                IEnumerable<string> prepareRresults;
                IEnumerable<Dictionary<string, string>> orderedParametrsValuesList = orderBy != null
                    ? (IEnumerable<Dictionary<string, string>>)orderBy.DynamicInvoke(ParametrsValuesList)
                    : ParametrsValuesList;
                if (construct != null)
                    prepareRresults =
                        orderedParametrsValuesList.SelectMany(dictionary => construct.Select(func => func(dictionary)));
                else if (describe)
                {
                    if (SelectParameters == null)
                        prepareRresults = orderedParametrsValuesList.SelectMany(dict =>
                            dict.Values.SelectMany(s => 
                            Gr.GetDirect(s)
                              .Select(pair => s + " " + pair.predicate + " " + pair.entity)
                              .Concat(Gr.GetData(s)
                                        .Select(predValLan => s + " " + predValLan.predicate + " " + predValLan.data))
                              .Concat(Gr.GetInverse(s)
                                        .Select(predVal => predVal.entity + " " + predVal.predicate + " " + s))));
                    else
                        prepareRresults = orderedParametrsValuesList.SelectMany(dict =>
                            SelectParameters.Select(pName => dict[pName])
                                .SelectMany(s =>
                            Gr.GetDirect(s)
                                    .Select(pair => s + " " + pair.predicate + " " + pair.entity)
                                    .Concat(Gr.GetData(s)
                                            .Select(predValLan => s + " " + predValLan.predicate + " " + predValLan.data))
                                    .Concat(Gr.GetInverse(s)
                                            .Select(predVal => predVal.entity + " " + predVal.predicate + " " + s))));
                 
                }
                else
                {
                    if (SelectParameters == null)
                        prepareRresults = orderedParametrsValuesList.Select(par => string.Join(" ", par.Values));
                    else
                        prepareRresults =
                            orderedParametrsValuesList.Select(
                                dict => string.Join(" ", SelectParameters.Select(pName => dict[pName])));
                }

                    if (isDistinct)
                        prepareRresults = prepareRresults.Distinct();
                    else if (isReduce) //TODO Reduce
                        prepareRresults = prepareRresults.Distinct();
              // if(order)

                    //TODO order of limit offset
                    if (offset != -1)
                        prepareRresults = prepareRresults.Skip(offset);
                    if (limit != -1)
                        prepareRresults = prepareRresults.Take(limit);

                    return results = prepareRresults.ToArray();
            }
        }
        #endregion

     

    }
}
