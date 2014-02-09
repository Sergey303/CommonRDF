using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonRDF
{
    internal partial class SparqlChainParametred
    {
        protected readonly Dictionary<string, string> prefixes = new Dictionary<string, string>();
        internal readonly Dictionary<string, SparqlVariable> valuesByName = new Dictionary<string, SparqlVariable>();

        protected internal string TestDataConst(string oValue, ref bool isData)
        {
            if (oValue.StartsWith("'") && oValue.EndsWith("'") && !oValue.Trim('\'').Contains("'"))
                oValue = oValue.Trim('\'');
            else if (oValue.StartsWith("\"") && oValue.EndsWith("\"") && !oValue.Trim('"').Contains("\""))
                oValue = oValue.Trim('"');
            else
            {
                isData = false;
                oValue = ReplaceNamespacePrefix(oValue);
            }
            return oValue;
        }

        internal string ReplaceNamespacePrefix(string oValue)
        {
            var nsO = oValue.Split(':');
            if (oValue.StartsWith("<") || nsO.Length != 2)
                return oValue.TrimStart('<').TrimEnd('>');
            if (nsO[0].StartsWith("http")) return oValue;

            string nsUri;
            if (!prefixes.TryGetValue(nsO[0].Trim(), out nsUri))
                throw new Exception("неизвестное пространство имён " + nsO[0]);
            return nsUri + nsO[1].Trim();
        }

        protected bool TestParameter(string spoValue, out SparqlVariable spo)
        {
            if (!spoValue.StartsWith("?"))
            {
                if (!valuesByName.TryGetValue(spoValue, out spo))
                    valuesByName.Add(spoValue, spo = new SparqlVariable {Value = spoValue});
            }
            else
            {
                if (valuesByName.TryGetValue(spoValue, out spo))
                {
                    if (spo.Value == "hasParellellValue")
                    {
                        spo.Value = string.Empty;
                        return true;
                    }
                    return false;
                }
                valuesByName.Add(spoValue, spo = new SparqlVariable());
                return true;
            }
            return false;
        }

        protected SparqlNodeBase CreateTriplet(SparqlNodeBase root, string sValue, string pValue, string oValue,
            bool isOptional)
        {
            bool isData = true;
            SparqlVariable s, p, o;
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
            if (isOptional)
            {
                OptionalSparqlTripletBase newNode = null;
                if (!isNewP)
                    if (!isNewS)
                        if (!isNewO)
                            newNode = new SampleTripletBaseOptional(s, p, o);
                        else newNode = new SelectObjectOprtional(s, p, o);
                    else if (!isNewO)
                        newNode = new SelectSubjectOpional(s, p, o);
                    else
                    {
                        newNode = new SelectAllSubjectsOptional(s);
                        root.NextMatch = newNode.Match;
                        root = newNode;
                        newNode = new SelectObjectOprtional(s, p, o);
                    }
                else if (!isNewS)
                {
                    if (!isNewO)
                        newNode = new SelectPredicateOptional(s, p, o);
                    else throw new NotImplementedException();
                    //newNode = new SelectAllPredicatesBySub(s, p, o);
                }
                else if (!isNewO)
                {
                    //newNode = new SelectPredicateByObj(s, p, o);
                    throw new NotImplementedException();
                }
                else
                {
                    newNode = new SelectAllSubjectsOptional(s);
                    root.NextMatch = newNode.Match;
                    root = newNode;
                    newNode = new SelectPredicateOptional(s, p, o);
                }
                if (root is OptionalSparqlTripletBase)
                    (root as OptionalSparqlTripletBase).NextOptionalFailMatch = newNode.OptionalFailMatch;
                root.NextMatch = newNode.Match;
                return newNode;
            }
            else
            {
                SparqlNodeBase newNode = null;
                if (!isNewP)
                    if (!isNewS)
                        if (!isNewO)
                            newNode = new SampleTriplet(s, p, o);
                        else newNode = new SelectObject(s, p, o);
                    else if (!isNewO) newNode = new SelectSubject(s, p, o);
                    else
                    {
                        newNode = new SelectAllSubjects(s);
                        root.NextMatch = newNode.Match;
                        root = newNode;
                        newNode = new SelectObject(s, p, o);
                    }
                else if (!isNewS)
                {
                    if (!isNewO)
                        newNode = new SelectPredicate(s, p, o);
                    else

                        newNode = new SelectAllPredicatesBySub(s, p, o);
                }
                else if (!isNewO)
                {
                    newNode = new SelectPredicateByObj(s, p, o);
                }
                else
                {
                    newNode = new SelectAllSubjects(s);
                    root.NextMatch = newNode.Match;
                    root = newNode;
                    newNode = new SelectPredicate(s, p, o);
                }
                root.NextMatch = newNode.Match;
                return newNode;
            }
        }
    }
  


    /// <summary>
    /// filter expr1 || expr2
    /// { expr1 } union { expr2 }
    /// </summary>
    internal class ALternativesChains : SparqlNodeBase
    {
        public readonly List<SparqlNodeBase> nodes;
        public readonly List<SparqlNodeBase> Ends;
        public readonly SparqlVariable[][] exclusiveParams; 
        public ALternativesChains(string first, Func<string, SparqlNodeBase, IEnumerable<SparqlNodeBase>> nextParseAction,
            Dictionary<string, SparqlVariable> valuesByName,
            params string[] alternatives)
        {
            //запомним какие параметры уже были
            var copy = new Dictionary<string, SparqlVariable>(valuesByName);
            var startFirst = new StartNode();
            nodes=new List<SparqlNodeBase>();
            Ends = new List<SparqlNodeBase>();
            exclusiveParams=new SparqlVariable[alternatives.Length][];
            Ends.AddRange(nextParseAction(first, startFirst));
            nodes.Add(startFirst);
            for (int i = 0; i < alternatives.Length; i++)
            {
                // с помощью копии узнаём какие ПАРАМЕТРЫ были добавлены.
                exclusiveParams[i] = valuesByName.Where(p => string.IsNullOrWhiteSpace(p.Value.Value) && !copy.ContainsKey(p.Key)).Select(pair => pair.Value).ToArray();
                foreach (var parameter in exclusiveParams[i])
                {
                    //помечаем их
                    parameter.Value = "hasParellellValue";
                }
                var startAlter = new StartNode();

                //новые параметры в одной ветви будут новыми и во второй
                Ends.AddRange(nextParseAction(alternatives[i], startAlter));
                nodes.Add(startAlter);
                
                //снимаем метку.
                foreach (var parameter in exclusiveParams[i])
                    parameter.Value = string.Empty;
            }
        }

        /// <summary>
        /// Метод, проверяющий соответствующие части фильтра. 
        /// Должен вызвать Match() у обоих внутренних фильтров, т.к. они возможно лишь присваивают ?newP="newV"
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            if(nodes.Count==0) return false;
            int i=0;
            bool any = nodes[0].Match(); 
            foreach (var node in nodes.Skip(1))
            {
                //добавленые парамеры надо почистить, на случай если они не получат значения в других ветвях
                foreach (var p in exclusiveParams[i])
                    p.Value = string.Empty;
                i++;
                any = node.Match() || any;
            }
            return any;
        }
    }

    }