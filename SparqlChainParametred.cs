using System;
using System.Collections.Generic;

namespace CommonRDF
{
    internal class SparqlChainParametred : SparqlChain
    {
        protected readonly Dictionary<string, string> prefixes = new Dictionary<string, string>();
        internal readonly Dictionary<string, TValue> valuesByName = new Dictionary<string, TValue>();

        public SparqlChainParametred(SparqlChainParametred parent)
        {
            valuesByName = parent.valuesByName;
            prefixes = parent.prefixes;
        }

        protected SparqlChainParametred()
        {
           
        }

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

        protected bool TestParameter(string spoValue, out TValue spo)
        {
            if (!spoValue.StartsWith("?"))
            {
                if (!valuesByName.TryGetValue(spoValue, out spo))
                    valuesByName.Add(spoValue, spo = new TValue { Value = spoValue });
            }
            else
            {
                if (valuesByName.TryGetValue(spoValue, out spo))
                    return false;
                valuesByName.Add(spoValue, spo = new TValue());
                return true;
            }
            return false;
        }
    }
}