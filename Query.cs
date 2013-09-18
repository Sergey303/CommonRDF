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
        public List<QueryTriplet> triplets;
        public List<string> SelectParameters;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        public List<string> FiterList;
        public List<QueryTripletOptional> Optionals;

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
            triplets = new List<QueryTriplet>();
            Optionals=new List<QueryTripletOptional>();
            var paramByName = new Dictionary<string, TValue>();
            var optParamByName = new HashSet<string>();
            var constsByValue = new Dictionary<string, TValue>();
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

                        TValue sTValue;
                        TValue pTvalue;
                        TValue oTValue;

                        string sValue = s.Value.TrimStart('<').TrimEnd('>');
                        bool isNewS = TestParameter(sValue,
                            out sTValue, constsByValue, paramByName);
                        bool isNewP = TestParameter(p=p.TrimStart('<').TrimEnd('>'), 
                            out pTvalue, constsByValue, paramByName);
                        bool isNewO = TestParameter(o=(isData = o.StartsWith("'"))
                            ? o.Trim('\'')
                            : o.TrimStart('<').TrimEnd('>'), out oTValue, constsByValue, paramByName);

                        sTValue.SetTargetType(true);
                        if (isData)
                        {
                            oTValue.SetTargetType(false);
                            pTvalue.SetTargetType(false);
                        }
                        else if (!isNewO)
                        {
                            oTValue.SetTargetType(true);
                            pTvalue.SetTargetType(true);
                        }
                        else
                        {
                            if (pTvalue.IsObj != null)
                                oTValue.SetTargetType(pTvalue.IsObj.Value);
                            else if (oTValue.IsObj != null)
                                pTvalue.SetTargetType(oTValue.IsObj.Value);
                            else //both unkown
                            {
                                pTvalue.SubscribeIsObjSetted(oTValue);
                                oTValue.SubscribeIsObjSetted(pTvalue);
                            }
                        }
                        if (isOptional)
                        {
                            bool hasOptO, hasOptS, hasOptP;
                            if(!(hasOptS =optParamByName.Contains(sValue)))
                            optParamByName.Add(sValue);
                            if (!(hasOptP = optParamByName.Contains(p)))
                                optParamByName.Add(sValue);
                            if (!(hasOptO = optParamByName.Contains(o)))
                                optParamByName.Add(sValue);
                                Optionals.Add(new QueryTripletOptional
                                {
                                    S = sTValue,
                                    P = pTvalue,
                                    O = oTValue,
                                    IsNewS = isNewS,
                                    IsNewP = isNewP,
                                    IsNewO = isNewO,
                                    HasSOptValue = hasOptS,
                                    HasPOptValue = hasOptP,
                                    HasOOptValue = hasOptO
                                });
                        }
                        else  triplets.Add(new QueryTriplet
                        {
                            S=sTValue, P=pTvalue, O=oTValue,
                            IsNewS = isNewS,
                            IsNewP = isNewP,
                            IsNewO = isNewO
                        }); 


                     
                    }

                }
            }
            Parameters = paramByName.Values.ToArray();
            ParametersNames = paramByName.Keys.ToArray();
        }

        public void Run()
        {
            Match(0);
        }

        public TValue[] Parameters { get; set; }

        private static bool TestParameter(string spo, out TValue value, 
            Dictionary<string, TValue> constsByValue, Dictionary<string, TValue> paramByName)
        {
            if (!spo.StartsWith("?"))
            {
                if (!constsByValue.TryGetValue(spo, out value))
                    constsByValue.Add(spo, value = new TValue { Value = spo });
            }
            else
            {
                if (paramByName.TryGetValue(spo, out value))
                    return false; 
                paramByName.Add(spo, value = new TValue()); 
                return true;
            }
            return false;
        }

        private void Match(int i)
        {
            if (i == triplets.Count)
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

                MatchTriplet(i, !cqt.IsNewS, !cqt.IsNewP, !cqt.IsNewO, s, p, o);
            }
        }

        private void MatchTriplet(int i, bool hasValueS, bool hasValueP, bool hasValueO, TValue s, TValue p, TValue o)
        {
            bool isNotData = true; /// !p.State.HasFlag(TState.Data); - syncronized
            bool isObj = false; //TODO Can be setted, but for what?
            if (o.IsObj != null)
                isNotData = isObj = o.IsObj.Value;
            if (hasValueP)
            {
                if (hasValueS)
                {
                    if (hasValueO)
                    {
                        if (isNotData && gr.GetDirect(s.Value, p.Value).Contains(o.Value) ||
                            !isObj && gr.GetData(s.Value, p.Value).Contains(o.Value)) Match(i + 1);
                            return;
                    }
                    //else 
                    //Если o.IsObj не известен, то он не устанавливается, потому что, потом его не изменить 
                    if (isNotData)
                        foreach (string value in gr.GetDirect(s.Value, p.Value))
                        {
                            isObj = true;
                        o.Value =value;
                        Match(i + 1);
                    }
                    if (isObj) return;
                    foreach (string value in gr.GetData(s.Value, p.Value))
                    {
                        o.Value = value;
                        Match(i + 1);
                    }
                    return;
                }
                if (hasValueO)
                {
                    if (isNotData)
                        foreach (string value in gr.GetInverse(o.Value, p.Value))
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
                foreach (string id in gr.GetEntities())
                {
                    s.Value =id;
                    if(isNotData)
                    foreach (var v in gr.GetDirect(id, p.Value))
                    {
                        isObj = true;
                        o.Value=v;
                        Match(i + 1);
                    }
                    if (isObj) continue;
                    foreach (var v in gr.GetData(id, p.Value))
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
                        foreach (PredicateEntityPair pe in gr.GetDirect(s.Value)
                            .Where(pe => pe.entity == o.Value))
                        {
                            p.Value = pe.predicate;
                            Match(i+1);
                        }
                    if (isObj) return;
                    foreach (var pd in gr.GetData(s.Value)
                        .Where(pe => pe.data == o.Value)) //TODO lang
                    {
                        p.Value = pd.predicate;
                        Match(i + 1);
                    }
                    return;
                }
                if (isNotData)
                    foreach (PredicateEntityPair axe in gr.GetDirect(s.Value))
                    {
                        p.Value = axe.predicate;
                        o.Value =  axe.entity;
                        Match(i + 1);
                    }
                if (isObj) return;
                foreach (var axe in gr.GetData(s.Value))
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
                return;
            }
            throw new NotImplementedException();
        }


        private void MatchOptional(int i)
        {
            if (i == Optionals.Count)
                parametrsValuesList.Add(Parameters.Select(par => par.Value).ToArray());
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
                    bool any = false;
                    foreach (var newOptV in (unKnown.IsObj == null
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
                    return;
                } 
                }
                throw new NotImplementedException();
            }
        }

        public GraphBase gr;
        //   private static readonly Dictionary<Triplet<string>, bool> Cache = new Dictionary<Triplet<string>, bool>();

        public IEnumerable<string> GetSubjectsByProperty(string predicate, TValue o, string data)
        {
            if (predicate == ONames.p_name)
                return gr.SearchByName(data).Where(id => gr.GetData(id, ONames.p_name).Contains(data));
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
