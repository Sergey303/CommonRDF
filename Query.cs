﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            TValue.gr = graph;
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

                        if (isData)
                            ptriplet.O.SetTargetTypeData();
                        else if(!ptriplet.O.IsNewParametr)
                            ptriplet.O.State|=TState.Obj;
                        ptriplet.S.SetTargetTypeObj();

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
            ObserveQuery(0);
        }

        public TValue[] Parameters { get; set; }

        private static TValue TestParameter(string spo, Dictionary<string, TValue> paramByName)
        {
            TValue value;
            if (!spo.StartsWith("?")) return new TValue { Value = spo };
            if (paramByName.TryGetValue(spo, out value)) return value;
            paramByName.Add(spo, value = new TValue());
            value.IsNewParametr = true;
            return value;
        }

        private void ObserveQuery(int i)
        {
            if (i == triplets.Length)
            {
                foreach (var parameter in Parameters.Where(p => p.IsNewParametr))
                    parameter.State |= TState.IsOpen;
                ObserveOptional(0);
                foreach (var parameter in Parameters.Where(p => p.State.HasFlag(TState.IsOpen)))
                    parameter.State &= ~TState.IsOpen;
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

                ObserveTriplet(i, !p.IsNewParametr, !s.IsNewParametr, !o.IsNewParametr, s, p, o);
            }
        }

        private void ObserveTriplet(int i, bool hasValueP, bool hasValueS, bool hasValueO, TValue s, TValue p, TValue o)
        {
            if (hasValueP)
            {
                if (hasValueS)
                {
                    if (hasValueO)
                    {
                        if (DirectAxeContains(s.Item, p.Value, o))
                            ObserveQuery(i + 1);
                        return;
                    }
                    //else
                    foreach (string values in DirectAndDataAxeValues(s.Item, p.Value, o))
                    {
                        o.SetValue(values);
                        ObserveQuery(i + 1);
                    }
                    o.IsNewParametr = true;
                    return;
                }
                if (hasValueO)
                {
                    if (o.State.HasFlag(TState.Data)) //Data predicate, S-param, O has value
                    {
                        foreach (var itm in GetSubjectsByProperty(p.Value, o.Value))
                            s.SetValue(itm.Key, itm.Value);
                        s.IsNewParametr = true;
                        return;
                    }
                    //else
                    foreach (string values in InverseAxeValues(o.Item, p.Value))
                    {
                        s.SetValue(values);
                        ObserveQuery(i + 1);
                    }
                    s.IsNewParametr = true;
                    return;
                }
                // s & o new params
                foreach (var iditm in GetSubjectsByProperty(p.Value))
                {
                    var pre = DirectAndDataAxeValues(iditm.Value, p.Value, o);

                    s.SetValue(iditm.Key, iditm.Value);
                    foreach (var v in pre)
                    {
                        o.SetValue(v);
                        ObserveQuery(i + 1);
                    }
                }
                s.IsNewParametr = true;
                o.IsNewParametr = true;
                return;
            }
            throw new NotImplementedException();
            // p & (s or o) new params
            if (!hasValueS || !hasValueO) //p  & (s xor o) new params
            {
                //if (!isNPs && !isNPo) 
                //    foreach (var property in
                //        (isNPo ? item.direct : item.inverse)
                //            .Where(property => property.Variants.Contains(p.V)))
                //    {
                //        p.SetValue(property.Predicate, isOpt);
                //        ObserveQuery(i + 1);
                //    }
                //else // p
                //{
                //    var properties = isNPs ? item.inverse : item.direct;
                //    foreach (var prp in properties)
                //        ObserveTriplet(i, s, p.SetValue(prp.Predicate, isOpt), o,
                //            isNPs, false, isNPo, isOpt, item, prp, false, false);
                //}
                //p.DropValue(false);
            }
            else // all params new
            {
            }
        }


        private void ObserveOptional(int i)
        {
            if (i == Optionals.Length)
                parametrsValuesList.Add(Parameters.Select(par => par.Value).ToArray());
            else
            {
                var current = Optionals[i];
                ObserveOptionalTriplet(current.S, current.P, current.O, i,
                    !current.S.State.HasFlag(TState.IsOpen),
                    !current.P.State.HasFlag(TState.IsOpen),
                    !current.O.State.HasFlag(TState.IsOpen));
            }
        }

        public Graph gr;
        //   private static readonly Dictionary<Triplet<string>, bool> Cache = new Dictionary<Triplet<string>, bool>();
        public void ObserveOptionalTriplet(TValue s, TValue p, TValue o, int i,
            bool hasFixedValueS, bool hasFixedValueP, bool hasFixedValueO)
        {
            if (hasFixedValueP)
            {
                IEnumerable<string> newValues;
                if (hasFixedValueS)
                {
                    if (hasFixedValueO)
                        ObserveOptional(i + 1);
                    else
                    {

                        string oldValue = o.IsNewParametr ? null : o.Value;
                        if ((newValues = DirectAndDataAxeValues(s.Item, p.Value, o).ToList()).Any())
                        {
                            if (oldValue != null)
                            {
                                ObserveOptional(i + 1);
                                newValues = newValues.Where(v => !ReferenceEquals(v, oldValue));
                            }
                            foreach (var oNewValue in newValues)
                            {
                                o.SetValue(oNewValue);
                                ObserveOptional(i + 1);
                            }
                            if (oldValue != null) o.SetValue(oldValue);
                            else o.IsNewParametr = true;

                        }
                        else
                        {
                            o.SetValue(string.Empty);
                            ObserveOptional(i + 1);
                            o.IsNewParametr = true;
                        }
                    }
                }
                else if (hasFixedValueO)
                {
                    string oldValue = s.IsNewParametr ? null : s.Value;
                    if ((newValues = InverseAxeValues(s.Item, p.Value).ToList()).Any())
                    {
                        if (oldValue != null)
                        {
                            ObserveOptional(i + 1);
                            newValues = newValues.Where(v => !ReferenceEquals(v, oldValue));
                        }
                        foreach (var oNewValue in newValues)
                        {
                            s.SetValue(oNewValue);
                            ObserveOptional(i + 1);
                        }
                        if (oldValue != null) s.SetValue(oldValue);
                        else s.IsNewParametr = true;
                    }
                    else
                    {
                        s.SetValue(string.Empty);
                        ObserveOptional(i + 1);
                        o.IsNewParametr = true;
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<string, RecordEx>> GetSubjectsByProperty(string predicate, string data = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> DirectAndDataAxeValues(RecordEx item, string predicate, TValue parameter)
        {
            Axe pre;
            if (!parameter.State.HasFlag(TState.Data))
            {
                pre = item.direct.FirstOrDefault(d => d.predicate == predicate);
                if (pre != null)
                {
                    parameter.State |= TState.Obj;
                    foreach (var value in pre.variants)
                        yield return value;
                }
            }
            if (parameter.State.HasFlag(TState.Obj)) yield break;
            pre = item.data.FirstOrDefault(d => d.predicate == predicate);
            if (pre == null) yield break; //|| !pre.variants.Any()
            parameter.SetTargetTypeData();
            foreach (var value in pre.variants)
                yield return value;
        }
        public IEnumerable<string> InverseAxeValues(RecordEx item, string predicate)
        {
            var pre = item.inverse.FirstOrDefault(d => d.predicate == predicate);
            if (pre == null) yield break;
            foreach (var value in pre.variants)
                yield return value;
        }


        public bool DirectAxeContains(RecordEx item, string predicate, TValue parameter)
        {
            bool containsInData = false, containsInObj = false;
            Axe pre;
            if (!parameter.State.HasFlag(TState.Obj)
                && (pre = item.data.FirstOrDefault(d => d.predicate == predicate)) != null
                && (containsInData = pre.variants.Contains(parameter.Value)))
                parameter.State |= TState.Data;
            if (!parameter.State.HasFlag(TState.Data)
                && (pre = item.direct.FirstOrDefault(d => d.predicate == predicate)) != null
                && (containsInObj = pre.variants.Contains(parameter.Value)))
                parameter.State |= TState.Obj;
            return containsInData || containsInObj;
        }

        public bool InverseAxeContains(RecordEx item, string predicate, string value)
        {
            var pre = item.inverse.FirstOrDefault(d => d.predicate == predicate);
            return pre != null && pre.variants.Contains(value);
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
