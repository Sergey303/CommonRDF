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
        public QueryTriplet[] triplets;
        public List<string> SelectParameters;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        public List<string> FiterList;
        public QueryTriplet[] Optionals;

        public static Regex QuerySelectReg = new Regex(@"select\s+(?<selectGroups>((\?\w+\s+)+|\*))",
            RegexOptions.Compiled);

        public static Regex QueryWhereReg = new Regex(@"where\s+\{(?<whereGroups>([^{}]*\{[^{}]*\}[^{}]*)*|[^{}]*)\}",
            RegexOptions.Compiled);

        public static Regex TripletsReg = new Regex(
            @"((?<s>[^\s]+|'.*')\s+(?<p>[^\s]+|'.*')\s+(?<o>[^\s]+|'.*')\.(\s|$))|optional\s+{\s*(?<os>[^\s]+|'.*')\s+(?<op>[^\s]+|'.*')\s+(?<oo>[^\s]+|'.*')\s*}(\s|$)"
            , RegexOptions.Compiled);

        public TValue[] ParametersWithMultiValues;
        private List<string[]> parametrsValuesList = new List<string[]>();

        public Query(string filePath, GraphBase graph)
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
                        SelectParameters = parameters.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
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
                            ? o.Trim('\'')
                            : o.TrimStart('<').TrimEnd('>')
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
                MatchOptional(0);
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
                    //не уверен в правильности конкатенации
                    if (o.IsObj == null)
                        enumerable = gr.GetDirect(s.Value, p.Value).Concat(gr.GetData(s.Value, p.Value));
                    else if (o.IsObj.Value) enumerable = gr.GetDirect(s.Value, p.Value);
                    else if (!o.IsObj.Value) enumerable = gr.GetData(s.Value, p.Value);

                    if (hasValueO)
                    {
                        if (enumerable.Contains(o.Value))
                            Match(i + 1);
                        return;
                    }
                    //else 
                    //Если o.IsObj не известен, то он не устанавливается, потому что, потом его не изменить 
                    o.IsNewParameter = false;
                    foreach (string value in enumerable)
                    {
                        o.Value =value;
                        Match(i + 1);
                    }
                    o.IsNewParameter = true;
                    return;
                }
                if (hasValueO)
                {
                    s.IsNewParameter = false;
                    var SValues = o.IsObj == null
                        ? gr.GetInverse(o.Value, p.Value).Concat(GetSubjectsByProperty(p.Value, o, o.Value))
                        : o.IsObj.Value ? gr.GetInverse(o.Value, p.Value) : GetSubjectsByProperty(p.Value, o, o.Value);

                    foreach (string value in SValues)
                    {
                        s.Value = value;
                        Match(i + 1);
                    }
                    s.IsNewParameter = true;
                    return;
                }
                // s & o new params
                Func<string, string, IEnumerable<string>> oVallues;
                if (o.IsObj == null)
                    oVallues = (id, pv) => gr.GetDirect(id, pv).Concat(gr.GetData(id, pv));
                else if (o.IsObj.Value)
                    oVallues = (id, pv) => gr.GetDirect(id, pv);
                else oVallues = (id, pv) => gr.GetData(id, pv);
                s.IsNewParameter = false;
                o.IsNewParameter = false; 
                foreach (string id in gr.GetEntities())
                {
                    s.Value =id;
                    foreach (var v in oVallues(id, p.Value))
                    {
                        o.Value=v;
                        Match(i + 1);
                    }
                }
                s.IsNewParameter = true;
                o.IsNewParameter = true;
                return;
            }
            // p new param
            p.IsNewParameter = false;
            bool isNotData = true; /// !p.State.HasFlag(TState.Data); - syncronized
            bool isObj = false; //TODO Can be setted, but for what?
            if (o.IsObj != null)
                isNotData = isObj = o.IsObj.Value;
            if (hasValueS)
            {
                if (hasValueO)
                {
                    if (isNotData)
                        foreach (PredicateEntityPair pe in gr.GetDirect(s.Value)
                            .Where(pe => pe.entity == o.Value))
                        {
                            p.Value = pe.predicate;
                            Match(i+1);
                        }
                    if (!isObj)
                        foreach (var pd in gr.GetData(s.Value)
                            .Where(pe => pe.data == o.Value)) //TODO lang
                        {
                            p.Value = pd.predicate;
                            Match(i + 1);
                        }
                    p.IsNewParameter = true;
                    return;
                }
                o.IsNewParameter = false;
                if (isNotData)
                    foreach (PredicateEntityPair axe in gr.GetDirect(s.Value))
                    {
                        p.Value = axe.predicate;
                        o.Value =  axe.entity;
                        Match(i + 1);
                    }
                if (!isObj)
                foreach (var axe in gr.GetData(s.Value))
                {
                    p.Value = axe.predicate;
                    o.Value = axe.data;
                    Match(i + 1);
                }
                p.IsNewParameter = true;
                o.IsNewParameter = true;
                return;
            }
            s.IsNewParameter = false;

            if (hasValueO)
            {
                if (isNotData)
                    foreach (PredicateEntityPair axe in gr.GetInverse(o.Value))
                    {
                        p.Value =axe.predicate;
                        s.Value=axe.entity;
                        Match(i + 1);
                    }
                if (isObj) return;
                foreach (PredicateDataTriple axe in gr.GetData(o.Value))
                {
                    p.Value = axe.predicate;
                    s.Value = axe.data;
                    Match(i + 1);
                }
                s.IsNewParameter = true;
                p.IsNewParameter = true;
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
                bool hasFixedValueS = !current.S.IsNewParameter;
                bool hasFixedValueO = !current.O.IsNewParameter;
                if (!current.P.IsNewParameter)
                {
                    if (hasFixedValueS && hasFixedValueO)
                    {
                        MatchOptional(i + 1);
                        return;
                    }
                    var known = hasFixedValueS ? current.S.Value : current.O.Value;
                    var unKnown = hasFixedValueS ? current.O : current.S;
                    if (unKnown.HasOptValue)
                    {
                        MatchOptional(i + 1);
                        string oldValue = unKnown.Value;
                        foreach (var newOptV in (
                            hasFixedValueO ? gr.GetInverse(known, current.P.Value) :
                                unKnown.IsObj == null
                                    ? gr.GetData(known, current.P.Value).Concat(gr.GetDirect(known, current.P.Value))
                                    : unKnown.IsObj.Value ? gr.GetDirect(known, current.P.Value) : gr.GetData(known, current.P.Value))
                            .Where(newOptV => newOptV != oldValue))
                        {
                            unKnown.Value = newOptV;
                            MatchOptional(i + 1);
                        }
                        unKnown.Value = oldValue;
                        return;
                    }
                    unKnown.HasOptValue = true;
                    bool any = false;
                    foreach (var newOptV in (hasFixedValueO ? gr.GetInverse(known, current.P.Value) :
                        unKnown.IsObj == null
                            ? gr.GetData(known, current.P.Value).Concat(gr.GetDirect(known, current.P.Value))
                            : unKnown.IsObj.Value ? gr.GetDirect(known, current.P.Value) : gr.GetData(known, current.P.Value)))
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
                    unKnown.HasOptValue = false;
                    return;
                }
                throw new NotImplementedException();
            }
        }

        public GraphBase gr;
        //   private static readonly Dictionary<Triplet<string>, bool> Cache = new Dictionary<Triplet<string>, bool>();

        public IEnumerable<string> GetSubjectsByProperty(string predicate, TValue o, string data)
        {
            if (predicate == ONames.p_name)
                return gr.SearchByN4(data).Where(id => gr.GetData(id, ONames.p_name).Contains(data));
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
