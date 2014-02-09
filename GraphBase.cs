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
        public abstract void Load(params string[] rdfFiles);
        public abstract void CreateGraph();
        public abstract IEnumerable<string> GetEntities();
        public abstract IEnumerable<PredicateEntityPair> GetDirect(string id, object nodeInfo = null);
        public abstract IEnumerable<PredicateEntityPair> GetInverse(string id, object nodeInfo = null);
        public abstract IEnumerable<PredicateDataTriple> GetData(string id, object nodeInfo = null);
        public abstract IEnumerable<PredicateEntityPair> GetSubjectsByData(string data, object nodeInfo = null);
        public abstract IEnumerable<string> GetDirect(string id, string predicate, object nodeInfo = null);
        public abstract IEnumerable<string> GetInverse(string id, string predicate, object nodeInfo = null);
        public abstract IEnumerable<string> GetData(string id, string predicate, object nodeInfo = null);
        public abstract IEnumerable<string> GetSubjectsByData(string data, string predicate, object nodeInfo = null);
        public abstract IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate, object nodeInfo = null);
        public abstract void GetItembyId(string id);
        public abstract void  Test();
        public abstract string[] SearchByName(string ss);

        public static Regex LangRegex = new Regex("@([^@]{1,5})$");
        public static DataLangPair SplitLang(string dataLang)
        {
            string lang = null;
           return new DataLangPair(LangRegex.Replace(dataLang, match =>
           {
               lang = match.Groups[1].Value;
               return string.Empty;
           }), lang);
        }

        #region Object Node InputMethods

        public abstract object GetNodeInfo(string id);

        #endregion

    }
}