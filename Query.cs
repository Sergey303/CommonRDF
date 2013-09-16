using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using sema2012m;

namespace CommonRDF
{
    class Query
    {
        public QueryTriplet[] triplets;
        public List<string> SelectParameters;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        public List<string> FiterList;
        public QueryTriplet[] Optionals;
        public static Regex QuerySelectReg = new Regex(@"select\s+(?<selectGroups>((\?\w+\s+)+|\*))", RegexOptions.Compiled);
        public static Regex QueryWhereReg = new Regex(@"where\s+\{(?<whereGroups>([^{}]*\{[^{}]*\}[^{}]*)*|[^{}]*)\}", RegexOptions.Compiled);
        public static Regex TripletsReg = new Regex(
            @"((?<s>[^\s]+|'.*')\s+(?<p>[^\s]+|'.*')\s+(?<o>[^\s]+|'.*')\.(\s|$))|optional\s+{\s*(?<os>[^\s]+|'.*')\s+(?<op>[^\s]+|'.*')\s+(?<oo>[^\s]+|'.*')\s*}(\s|$)"
            , RegexOptions.Compiled);

        public TValue[] ParametersWithMultiValues;
        private List<string[]> parametrsValuesList = new List<string[]>();

        public Query(string filePath, Graph graph)
        {
            gr = graph;
            SelectParameters = new List<string>();
            //   var parameterTests = new Dictionary<TValue, List<QueryTriplet>>();
            // var parametesWithMultiValues = new HashSet<TValue>();
            var tripletsList = new List<QueryTriplet>();
            var paramByName = new Dictionary<string, TValue>();
            var optionals = new List<QueryTriplet>();
            using (var f = new StreamReader(filePath))
            {
                var qs = f.ReadToEnd();
                var selectMatch = QuerySelectReg.Match(qs);
                if (selectMatch.Success)
                {
                    string parameters = selectMatch.Groups["selectGroups"].Value.Trim();
                    if (parameters != "*")
                        SelectParameters = parameters.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                var whereMatch = QueryWhereReg.Match(qs);
                if (whereMatch.Success)
                {
                    string tripletsGroup = whereMatch.Groups["whereGroups"].Value;
                    foreach (Match tripletMatch in TripletsReg.Matches(tripletsGroup))
                    {
                        var s = tripletMatch.Groups["s"];
                        string p, o;
                        var ptriplet = new QueryTriplet();
                        bool isOptional = false, isData;
                        if (s.Success)
                        {
                            p = tripletMatch.Groups["p"].Value;
                            o = tripletMatch.Groups["o"].Value;
                        }
                        else if ((s = tripletMatch.Groups["os"]).Success)
                        {
                            p = tripletMatch.Groups["op"].Value;
                            o = tripletMatch.Groups["oo"].Value;
                            isOptional = true;
                        }
                        else throw new Exception("strange query triplet: " + tripletMatch.Value);
                        ;

                        ptriplet.S = TestParameter(s.Value.TrimStart('<').TrimEnd('>'), paramByName);
                        ptriplet.P = TestParameter(p.TrimStart('<').TrimEnd('>'), paramByName);
                        ptriplet.O = TestParameter((isData = o.StartsWith("'"))
                            ? o.Trim('\'') : o.TrimStart('<').TrimEnd('>')
                            , paramByName);

                        ptriplet.S.SetTargetType(true);
                        if (isData)
                        {
                            ptriplet.O.SetTargetType(false);
                            ptriplet.P.SetTargetType(false);
                        }
                        else if (!ptriplet.O.IsNewParameter)
                        {
                            ptriplet.O.SetTargetType(true);
                            ptriplet.P.SetTargetType(true);
                        }
                        else
                        {
                            if (ptriplet.P.IsObj != null)
                                ptriplet.O.SetTargetType(ptriplet.P.IsObj.Value);
                            else if (ptriplet.O.IsObj != null)
                                ptriplet.P.SetTargetType(ptriplet.O.IsObj.Value);
                            else //both unkown
                            {
                                ptriplet.P.SubscribeIsObjSetted(ptriplet.O);
                                ptriplet.O.SubscribeIsObjSetted(ptriplet.P);
                            }
                        }



                        if (isOptional)
                            optionals.Add(ptriplet);
                        else
                            tripletsList.Add(ptriplet);
                    }

                }
            }
            triplets = tripletsList.ToArray();
            Parameters = paramByName.Values.ToArray();
            ParametersNames = paramByName.Keys.ToArray();
            Optionals = optionals.ToArray();
        }

        public void Run()
        {
            Match(0);
        }

        public TValue[] Parameters { get; set; }

        private static TValue TestParameter(string spo, Dictionary<string, TValue> paramByName)
        {
            TValue value;
            if (!paramByName.TryGetValue(spo, out value))
                paramByName.Add(spo, value = new TValue());
            if (!spo.StartsWith("?"))
                value.Value = spo;
            else value.IsNewParameter = true;
            return value;
        }

        private void Match(int i)
        {
            if (i == triplets.Length)
            {
                foreach (var parameter in Parameters.Where(p => p.IsNewParameter))
                    parameter.IsOpen = true;
                MatchOptional(0);
                foreach (var parameter in Parameters.Where(p => p.IsOpen))
                    parameter.IsOpen = false;
            }
            else
            {
                var cqt = triplets[i];
                TValue s = cqt.S,
                    p = cqt.P,
                    o = cqt.O;
                //bool hasValueS = !s.IsNewParametr,
                //    hasValueP = !p.IsNewParametr,
                //    hasValueO = !o.IsNewParametr;

                MatchTriplet(i, !p.IsNewParameter, !s.IsNewParameter, !o.IsNewParameter, s, p, o);
            }
        }

        private void MatchTriplet(int i, bool hasValueP, bool hasValueS, bool hasValueO, TValue s, TValue p, TValue o)
        {
            if (hasValueP)
            {
                if (hasValueS)
                {
                    IEnumerable<string> enumerable = null;
                    ///не уверен в правильности конкатенации
                    if (o.IsObj == null) enumerable = gr.GetDirect(s.Value, p.Value).Concat(gr.GetData(s.Value, p.Value));
                    else if (o.IsObj.Value) enumerable = gr.GetDirect(s.Value, p.Value);
                    else if (!o.IsObj.Value) enumerable = gr.GetData(s.Value, p.Value);

                    if (hasValueO)
                    {
                        if (enumerable.Contains(o.Value))
                            Match(i + 1);
                        return;
                    }
                    //else 
                    ///Если o.IsObj не известен, то он не устанавливается, потому что, потом его не изменить 
                    foreach (string values in enumerable)
                    {
                        o.SetValue(values);
                        Match(i + 1);
                    }
                    o.IsNewParameter = true;
                    return;
                }
                if (hasValueO)
                {
                    if (o.IsObj != null && !o.IsObj.Value) //Data object and predicate(pre computed to de equal), S-param, O has value
                    {
                        foreach (var itm in GetSubjectsByProperty(p.Value, o, o.Value))
                        {
                            s.SetValue(itm);
                            Match(i+1);
                        }
                        s.IsNewParameter = true;
                        return;
                    }
                    //else
                    foreach (string values in gr.GetInverse(o.Value, p.Value))
                    {
                        s.SetValue(values);
                        Match(i + 1);
                    }
                    s.IsNewParameter = true;
                    return;
                }
                // s & o new params
                foreach (KeyValuePair<string, Axe> iditm in GetSubjectsByProperty(p.Value, o))
                {
                    s.SetValue(iditm.Key);
                    foreach (var v in iditm.Value.variants)
                    {
                        o.SetValue(v);
                        Match(i + 1);
                    }
                }
                s.IsNewParameter = true;
                o.IsNewParameter = true;
                return;
            }
            // p & (s or o) new params
            bool isNotData = true; /// !p.State.HasFlag(TState.Data); - syncronized
            bool isObj = false;
            if (o.IsObj != null)
                isNotData = isObj = o.IsObj.Value;
            if (hasValueS)
            {
                if (hasValueO)
                {
                    return;
                }
                if (isNotData)
                    foreach (PredicateEntityPair axe in gr.GetDirect(s.Value))
                    {
                        p.SetValue(axe.predicate);
                        o.SetValue(axe.entity);
                        Match(i + 1);
                    }
                if (isObj) return;
                foreach (var axe in GetData(s.Value))
                {
                    p.SetValue(axe.predicate);
                    o.SetValue(axe.data);
                    Match(i + 1);
                }

                return;
            }
            if (hasValueO)
            {
                if (isNotData)
                    foreach (PredicateEntityPair axe in gr.GetInverse(o.Value))
                    {
                        p.SetValue(axe.predicate);
                        s.SetValue(axe.entity);
                        Match(i + 1);
                    }
                if (isObj) return;
                foreach (PredicateDataTriple axe in GetData(o.Value))
                {
                    p.SetValue(axe.predicate);
                    s.SetValue(axe.data);
                    Match(i + 1);
                }

                return;
            }
            throw new NotImplementedException();
        }


        private void MatchOptional(int i)
        {
            if (i == Optionals.Length)
                parametrsValuesList.Add(Parameters.Select(par => par.Value).ToArray());
            else
            {
                var current = Optionals[i];
                MatchptionalTriplet(current.S, current.P, current.O, i,
                    !current.S.IsOpen,
                    !current.P.IsOpen,
                    !current.O.IsOpen);
            }
        }

        public Graph gr;
        //   private static readonly Dictionary<Triplet<string>, bool> Cache = new Dictionary<Triplet<string>, bool>();
        public void MatchptionalTriplet(TValue s, TValue p, TValue o, int i,
            bool hasFixedValueS, bool hasFixedValueP, bool hasFixedValueO)
        {
            if (hasFixedValueP)
            {
                IEnumerable<string> newValues;
                if (hasFixedValueS)
                {
                    if (hasFixedValueO)
                        MatchOptional(i + 1);
                    else
                    {

                        string oldValue = o.IsNewParameter ? null : o.Value;
                        if ((newValues = gr.GetData(s.Value, p.Value).Concat(gr.GetDirect(s.Value, p.Value)).ToList()).Any())
                        {
                            if (oldValue != null)
                            {
                                MatchOptional(i + 1);
                                newValues = newValues.Where(v => !ReferenceEquals(v, oldValue));
                            }
                            foreach (var oNewValue in newValues)
                            {
                                o.SetValue(oNewValue);
                                MatchOptional(i + 1);
                            }
                            if (oldValue != null) o.SetValue(oldValue);
                            else o.IsNewParameter = true;

                        }
                        else
                        {
                            o.SetValue(string.Empty);
                            MatchOptional(i + 1);
                            o.IsNewParameter = true;
                        }
                    }
                }
                else if (hasFixedValueO)
                {
                    string oldValue = s.IsNewParameter ? null : s.Value;
                    if ((newValues = gr.GetInverse(o.Value, p.Value).ToList()).Any())
                    {
                        if (oldValue != null)
                        {
                            MatchOptional(i + 1);
                            newValues = newValues.Where(v => !ReferenceEquals(v, oldValue));
                        }
                        foreach (var oNewValue in newValues)
                        {
                            s.SetValue(oNewValue);
                            MatchOptional(i + 1);
                        }
                        if (oldValue != null) s.SetValue(oldValue);
                        else s.IsNewParameter = true;
                    }
                    else
                    {
                        s.SetValue(string.Empty);
                        MatchOptional(i + 1);
                        o.IsNewParameter = true;
                    }
                }
            }
        }

        public IEnumerable<string> GetSubjectsByProperty(string predicate, TValue o, string data)
        {
            if (predicate == ONames.p_name)
                return gr.SearchByN4(data).Where(id => gr.GetData(id, ONames.p_name).Contains(data));
            Axe pre;
            return gr.Dics.Where(id_item =>
                (pre =
                    (o.IsObj == null
                        ? id_item.Value.direct.Concat(id_item.Value.data)
                        : (o.IsObj.Value
                            ? id_item.Value.direct
                            : id_item.Value.data))
                        .FirstOrDefault(axe => axe.predicate == predicate)) != null
                && pre.variants.Contains(data))
                .Select(id_item => id_item.Key);

        }

        public IEnumerable<KeyValuePair<string, Axe>> GetSubjectsByProperty(string predicate, TValue o)
        {
            Axe axe = null;
            if (o.IsObj == null)
                return
                gr.Dics.Where(
                    id_item => DirectPredicates(id_item.Value, predicate, o, ref axe))
                    .Select(id_item => new KeyValuePair<string, Axe>(id_item.Key, axe));
            return
                gr.Dics.Where(
                    id_item => (axe = o.IsObj.Value
                        ? id_item.Value.direct.FirstOrDefault(d => d.predicate == predicate)
                        : id_item.Value.data.FirstOrDefault(d => d.predicate == predicate)) != null)
                    .Select(id_item => new KeyValuePair<string, Axe>(id_item.Key, axe));
        }

        private static bool DirectPredicates(RecordEx item, string predicate, TValue o, ref Axe pre)
        {
            pre = item.direct.FirstOrDefault(d => d.predicate == predicate);
            if (pre != null)
            {
                o.IsObj = false; // (preDir == null) - must be true
                return true;
            }
            pre = item.direct.FirstOrDefault(d => d.predicate == predicate);
            if (pre != null)
            {
                o.IsObj = true;
                return true;
            }
            return false;
        }

        IEnumerable<PredicateDataTriple> GetData(string id)
        {
            RecordEx rec;
            if (gr.Dics.TryGetValue(id, out rec))
            {
                var qu = rec.data.SelectMany(axe =>
                {
                    string predicate = axe.predicate;
                    return axe.variants.Select(v =>
                    {
                        var substr = v.Split(new[] { '@' });
                        return new PredicateDataTriple(predicate, substr[0], substr.Length == 1 ? "" : substr.Last());
                    });
                });
                return qu;
            }
            return Enumerable.Empty<PredicateDataTriple>();
        }
        internal void OutputParamsAll(string outPath)
        {
            using (StreamWriter io = new StreamWriter(outPath, true))
                foreach (var parametrsValues in parametrsValuesList)
                {
                    for (int i = 0; i < parametrsValues.Length; i++)
                    {
                        io.WriteLine(String.Format("{0} {1}",
                            ParametersNames[i],
                           parametrsValues[i]));
                    }
                    io.WriteLine();
                }
        }

        internal void OutputParamsBySelect(string outPath)
        {
            var parametrsValuesIndexes = ParametersNames
                            .Select((e, i) => new { e, i });
            using (var io = new StreamWriter(outPath, true, Encoding.UTF8))
                foreach (var parametrsValues in parametrsValuesList)
                {
                    foreach (var i in SelectParameters
                        .Select(p => parametrsValuesIndexes.First(e => e.e == p)))
                    {
                        io.WriteLine(String.Format("{0}",
                            parametrsValues[i.i]));
                    }
                    io.WriteLine();
                }
        }
    }
}
