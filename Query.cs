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
            , RegexOptions.Compiled);

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

        public Query(string filePath, GraphBase graph)
        {
            ParametrsValuesList = new List<string[]>();
            Gr = graph;
            SelectParameters = new List<string>();
            //   var parameterTests = new Dictionary<TValue, List<QueryTriplet>>();
            // var parametesWithMultiValues = new HashSet<TValue>();
            triplets = new List<QueryTriplet>();
            Optionals=new List<QueryTripletOptional>();
            var paramByName = new Dictionary<string, TValue>();
            var optParamHasValues = new HashSet<string>();
            var constsByValue = new Dictionary<string, TValue>();
            using (var f = new StreamReader(filePath))
            {
                var qs = f.ReadToEnd();
                var selectMatch = QuerySelectReg.Match(qs);
                if (selectMatch.Success)
                {
                    string parameters2Select = selectMatch.Groups["selectGroups"].Value.Trim();
                    if (parameters2Select != "*")
                        SelectParameters = parameters2Select.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                var whereMatch = QueryWhereReg.Match(qs);
                if (whereMatch.Success)
                {
                    string tripletsGroup = whereMatch.Groups["whereGroups"].Value;
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
                            out s, constsByValue, paramByName);
                        bool isNewP = TestParameter(pValue=pValue.TrimStart('<').TrimEnd('>'), 
                            out p, constsByValue, paramByName);
                        bool isNewO = TestParameter(oValue=(isData = oValue.StartsWith("'"))
                            ? oValue.Trim('\'')
                            : oValue.TrimStart('<').TrimEnd('>'), out o, constsByValue, paramByName);

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
                            if (p.IsObj != null)
                                o.SetTargetType(p.IsObj.Value);
                            else if (o.IsObj != null)
                                p.SetTargetType(o.IsObj.Value);
                            else //both unkown
                            {
                                p.SubscribeIsObjSetted(o);
                                o.SubscribeIsObjSetted(p);
                            }
                        }
                        if (isOptional)
                            Optionals.Add(new QueryTripletOptional
                            {
                                S = s,
                                P = p,
                                O = o,
                                IsNewS = isNewS,
                                IsNewP = isNewP,
                                IsNewO = isNewO,
                                HasSOptValue = HasOpt(isNewS, optParamHasValues, sValue),
                                HasPOptValue = HasOpt(isNewP, optParamHasValues, pValue),
                                HasOOptValue = HasOpt(isNewO, optParamHasValues, oValue)
                            });
                        else
                            triplets.Add(new QueryTriplet
                            {
                                S = s,
                                P = p,
                                O = o,
                                IsNewS = isNewS,
                                IsNewP = isNewP,
                                IsNewO = isNewO
                            });
                    }

                }
            }
            parameters = paramByName.Values.ToArray();
            ParametersNames = paramByName.Keys.ToArray();
        }

        public void Run()
        {
            Match(0);
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
            if (!spoValue.StartsWith("?"))
            {
                if (!constsByValue.TryGetValue(spoValue, out spo))
                    constsByValue.Add(spoValue, spo = new TValue { Value = spoValue });
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

        private void Match(int i)
        {
            if (i == triplets.Count)
            {
                MatchOptional(0);
                return;
            }
            var cqt = triplets[i];
            TValue s = cqt.S,
                p = cqt.P,
                o = cqt.O;
            //bool hasValueS = !s.IsNewParametr,
            //    hasValueP = !p.IsNewParametr,
            //    hasValueO = !o.IsNewParametr;

            bool hasValueS = !cqt.IsNewS;
            bool hasValueO = !cqt.IsNewO;
            bool isNotData = true; // !p.State.HasFlag(TState.Data); - syncronized
            bool isObj = false; //TODO Can be setted, but for what?
            if (o.IsObj != null)
                isNotData = isObj = o.IsObj.Value;
            if (!cqt.IsNewP)
            {
                if (hasValueS)
                {
                    if (hasValueO)
                    {
                        if (isNotData && Gr.GetDirect(s.Value, p.Value).Contains(o.Value) ||
                            !isObj && Gr.GetData(s.Value, p.Value).Contains(o.Value))
                            Match(i + 1);
                        return;
                    }
                    //else 
                    //Если o.IsObj не известен, то он не устанавливается, потому что, потом его не изменить 
                    if (isNotData)
                        foreach (string value in Gr.GetDirect(s.Value, p.Value))
                        {
                            isObj = true;
                            o.Value =value;
                            Match(i + 1);
                        }
                    if (isObj) return;
                    foreach (string value in Gr.GetData(s.Value, p.Value))
                    {
                        o.Value = value;
                        Match(i + 1);
                    }
                    return;
                }
                if (hasValueO)
                {
                    if (isNotData)
                        foreach (string value in Gr.GetInverse(o.Value, p.Value))
                        {
                            isObj = true;
                            s.Value = value;
                            Match(i + 1);
                        }
                    if (isObj) return;
                    foreach (string value in GetSubjectsByProperty(p.Value, o, o.Value))
                    {
                        s.Value = value;
                        Match(i + 1);
                    }
                    return;
                }
                // s & o new params
                foreach (string id in Gr.GetEntities())
                {
                    s.Value =id;
                    if(isNotData)
                        foreach (var v in Gr.GetDirect(id, p.Value))
                        {
                            isObj = true;
                            o.Value=v;
                            Match(i + 1);
                        }
                    if (isObj) continue;
                    foreach (var v in Gr.GetData(id, p.Value))
                    {
                        o.Value = v;
                        Match(i + 1);
                    }
                }
                return;
            }// p new param
          
            if (hasValueS)
            {
                if (hasValueO)
                {
                    if (isNotData)
                        foreach (PredicateEntityPair pe in Gr.GetDirect(s.Value)
                            .Where(pe => pe.entity == o.Value))
                        {
                            p.Value = pe.predicate;
                            Match(i+1);
                        }
                    if (isObj) return;
                    foreach (var pd in Gr.GetData(s.Value)
                        .Where(pe => pe.data == o.Value)) //TODO lang
                    {
                        p.Value = pd.predicate;
                        Match(i + 1);
                    }
                    return;
                }
                if (isNotData)
                    foreach (PredicateEntityPair axe in Gr.GetDirect(s.Value))
                    {
                        p.Value = axe.predicate;
                        o.Value =  axe.entity;
                        Match(i + 1);
                    }
                if (isObj) return;
                foreach (var axe in Gr.GetData(s.Value))
                {
                    p.Value = axe.predicate;
                    o.Value = axe.data;
                    Match(i + 1);
                }
                return;
            }
            if (hasValueO)
            {
                if (isNotData)
                    foreach (PredicateEntityPair axe in Gr.GetInverse(o.Value))
                    {
                        p.Value =axe.predicate;
                        s.Value=axe.entity;
                        Match(i + 1);
                    }
                if (isObj) return;
                foreach (PredicateDataTriple axe in Gr.GetData(o.Value))
                {
                    p.Value = axe.predicate;
                    s.Value = axe.data;
                    Match(i + 1);
                }
                return;
            }
            throw new NotImplementedException();
        }

        private void MatchOptional(int i)
        {
            if (i == Optionals.Count)
                ParametrsValuesList.Add(parameters.Select(par => par.Value).ToArray());
            else
            {
                var current = Optionals[i];
                bool hasFixedValueS = !current.IsNewS;
                bool hasFixedValueO = !current.IsNewO;
                if (!current.IsNewP)
                {
                    if (hasFixedValueS)
                    {
                        if (hasFixedValueO)
                        {
                            MatchOptional(i + 1);
                            return;
                        }


                        var known = current.S.Value;
                        var unKnown = current.O;
                        if (current.HasOOptValue)
                        {
                            MatchOptional(i + 1);
                            string oldValue = unKnown.Value;
                            foreach (var newOptV in (
                                unKnown.IsObj == null
                                    ? Gr.GetData(known, current.P.Value).Concat(Gr.GetDirect(known, current.P.Value))
                                    : unKnown.IsObj.Value ? Gr.GetDirect(known, current.P.Value) : Gr.GetData(known, current.P.Value))
                                .Where(newOptV => newOptV != oldValue))
                            {
                                unKnown.Value = newOptV;
                                MatchOptional(i + 1);
                            }
                            unKnown.Value = oldValue;
                            return;
                        }
                        bool any = false;
                        foreach (var newOptV in (unKnown.IsObj == null
                            ? Gr.GetData(known, current.P.Value).Concat(Gr.GetDirect(known, current.P.Value))
                            : unKnown.IsObj.Value ? Gr.GetDirect(known, current.P.Value) : Gr.GetData(known, current.P.Value)))
                        {
                            any = true;
                            unKnown.Value = newOptV;
                            MatchOptional(i + 1);
                        }
                        if (!any)
                        {
                            unKnown.Value = string.Empty;
                            MatchOptional(i + 1);
                        }
                        return;
                    }
                }
                throw new NotImplementedException();
            }
        }
        public IEnumerable<string> GetSubjectsByProperty(string predicate, TValue o, string data)
        {
            if (predicate == ONames.p_name)
                return Gr.SearchByName(data).Where(id => Gr.GetData(id, ONames.p_name).Contains(data));
            Axe pre;
            throw new NotImplementedException();
            return null;
            //gr.Dics.Where(id_item =>
            //(pre =
            //    (o.IsObj!=null
            //        ? id_item.Value.direct.Concat(id_item.Value.data)
            //        : (o.IsObj.Value
            //            ? id_item.Value.direct
            //            : id_item.Value.data))
            //        .FirstOrDefault(axe => axe.predicate == predicate)) != null
            //&& pre.variants.Contains(data))
            //.Select(id_item => id_item.Key);

        }

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
