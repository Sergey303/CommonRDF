using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    public struct PredicateEntityPair
    {
        public string predicate;
        public string entity;
        public PredicateEntityPair(string predicate, string entity)
        {
            this.predicate = predicate;
            this.entity = entity;
        }
    }
    public struct PredicateDataTriple
    {
        public string predicate;
        public string data;
        public string lang;
        public PredicateDataTriple(string predicate, string data, string lang)
        {
            this.predicate = predicate;
            this.data = data;
            this.lang = lang;
        }
    }
    public struct DataLangPair
    {
        public string data;
        public string lang;
        public DataLangPair(string data, string lang) { this.data = data; this.lang = lang; }
    }
    public abstract class GraphBase
    {
        public abstract IEnumerable<string> GetEntities();
        public abstract IEnumerable<PredicateEntityPair> GetDirect(string id);
        public abstract IEnumerable<PredicateEntityPair> GetInverse(string id);
        public abstract IEnumerable<PredicateDataTriple> GetData(string id);
        public abstract IEnumerable<string> GetDirect(string id, string predicate);
        public abstract IEnumerable<string> GetInverse(string id, string predicate);
        public abstract IEnumerable<string> GetData(string id, string predicate);
        public abstract IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate);
        public abstract void GetItembyId(string id);
        public abstract void Test();
        public abstract void Load(params string[] rdfFiles);
        public abstract string[] SearchByName(string ss);

        public static Regex LangRegex = new Regex("^(.*)@([^@]{1,5})$", RegexOptions.Compiled);
        public static DataLangPair SplitLang(string dataLang)
        {
            var m = LangRegex.Match(dataLang);
            return m.Success
                ? new DataLangPair(m.Groups[0].Value, m.Groups[1].Value) 
                :new DataLangPair(dataLang, null);
        }
    }
}