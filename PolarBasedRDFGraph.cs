using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PolarBasedRDF;
using PolarDB;
using sema2012m;

namespace CommonRDF
{

    public class PolarBasedRdfGraph : GraphBase
    {
   private readonly PType ptDirects = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("o", new PType(PTypeEnumeration.sstring))
            ));

        private readonly PType ptData = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("d", new PType(PTypeEnumeration.sstring)),
            new NamedType("l", new PType(PTypeEnumeration.sstring))
            ));

        private PaCell directCell, dataCell;
        private readonly string directCellPath;
        private readonly string dataCellPath;
        private FixedIndex<string> directObjIndex, inverseObjIndex, inverseDataIndex, directDataIndex;
        private FixedIndex<SubjPred> spDirectIndex, opDirectIndex, opDataIndex, spDataIndex;

        public PolarBasedRdfGraph(DirectoryInfo path)
        {
            this.path = path.FullName;
            if (!path.Exists) path.Create();
            directCell = new PaCell(ptDirects, directCellPath = Path.Combine(path.FullName, "rdf.direct.pac"),
                File.Exists(directCellPath));
            dataCell = new PaCell(ptData, dataCellPath = Path.Combine(path.FullName, "rdf.data.pac"),
                File.Exists(dataCellPath));
            if (dataCell.IsEmpty || directCell.IsEmpty) return;
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            directDataIndex = new FixedIndex<string>(path + "s of data", dataCell.Root, entry => (string) entry.Field(0).Get());
            directObjIndex = new FixedIndex<string>(path + "s of direct", directCell.Root, entry => (string)entry.Field(0).Get());
            inverseObjIndex = new FixedIndex<string>(path + "o of direct", directCell.Root, entry => (string)entry.Field(2).Get());
            inverseDataIndex = new FixedIndex<string>(path + "o of data", dataCell.Root, entry => (string)entry.Field(2).Get());
            spDataIndex = new FixedIndex<SubjPred>(path + "s and p of data", dataCell.Root,
                entry =>
                    new SubjPred
                    {
                        subj = (string) entry.Field(0).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
            spDirectIndex = new FixedIndex<SubjPred>(path + "s and p of direct", directCell.Root,
                entry =>
                    new SubjPred
                    {
                        subj = (string) entry.Field(0).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
            opDirectIndex = new FixedIndex<SubjPred>("o and p of direct", directCell.Root,
                entry =>
                    new SubjPred
                    {
                        subj = (string) entry.Field(2).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
            opDataIndex = new FixedIndex<SubjPred>("o and p of data", dataCell.Root,
             entry =>
                 new SubjPred
                 {
                     subj = (string)entry.Field(2).Get(),
                     pred = (string)entry.Field(1).Get()
                 });
        }

        public void Load(int tripletsCountLimit, params string[] filesPaths)
        {
            directCell.Close();
            dataCell.Close();

            File.Delete(dataCellPath);
            File.Delete(directCellPath);

            directCell = new PaCell(ptDirects, directCellPath, false);
            dataCell = new PaCell(ptData, dataCellPath, false);

            var directSerialFlow = (ISerialFlow) directCell;
            var dataSerialFlow = (ISerialFlow) dataCell;
            directSerialFlow.StartSerialFlow();
            dataSerialFlow.StartSerialFlow();
            directSerialFlow.S();
            dataSerialFlow.S();
            ReaderRDF.ReaderRDF.ReadFiles(tripletsCountLimit, filesPaths, (id, property, value, isObj, lang) =>
                {
                    if (isObj)
                        directSerialFlow.V(new object[] {id, property, value});
                    else dataSerialFlow.V(new object[] {id, property, value, lang ?? ""});
                });
            directSerialFlow.Se();
            dataSerialFlow.Se();
            directSerialFlow.EndSerialFlow();
            dataSerialFlow.EndSerialFlow();
        }

        internal void LoadIndexes()
        {
            if (directDataIndex == null)
                CreateIndexes();
            directDataIndex.Close();
            directObjIndex.Close();
            inverseObjIndex.Close();
            spDataIndex.Close();
            spDirectIndex.Close();
            opDirectIndex.Close();
            opDataIndex.Close();
            inverseDataIndex.Close();
            CreateIndexes();

            directDataIndex.Load(null);
            directObjIndex.Load(null);
            inverseObjIndex.Load(null);
            inverseDataIndex.Load(null);
            var subjPredComparer = new SubjPredComparer();
            spDataIndex.Load(subjPredComparer);
            spDirectIndex.Load(subjPredComparer);
            opDirectIndex.Load(subjPredComparer);
            opDataIndex.Load(subjPredComparer);
        }


        public string GetItem(string id)
        {
            return
                directObjIndex.GetAllByKey(id)
                    .Select(spo => spo.Type.Interpret(spo.Get()))
                    .Concat(
                        inverseObjIndex.GetAllByKey(id)
                            .Select(spo => spo.Type.Interpret(spo.Get()))
                            .Concat(
                                directDataIndex.GetAllByKey(id)
                                    .Select(spo => spo.Type.Interpret(spo.Get()))))
                    .Aggregate((all, one) => all + one);
        }

        public XElement GetItemByIdBasic(string id, bool addinverse)
        {
            var type =
                spDirectIndex.GetFirstByKey(new SubjPred {pred = ONames.rdftypestring, subj = id});
            XElement res = new XElement("record", new XAttribute("id", id),
                type.offset == long.MinValue ? null : new XAttribute("type", ((object[]) type.Get())[2]),
                directDataIndex.GetAllByKey(id).Select(entry => entry.Get()).Cast<object[]>().Select(v3 =>
                    new XElement("field", new XAttribute("prop", v3[1]),
                        string.IsNullOrEmpty((string) v3[3]) ? null : new XAttribute(ONames.xmllang, v3[3]),
                        v3[2])),
                directObjIndex.GetAllByKey(id).Select(entry => entry.Get()).Cast<object[]>().Select(v2 =>
                    new XElement("direct", new XAttribute("prop", v2[1]),
                        new XElement("record", new XAttribute("id", v2[2])))),
                null);
            // Обратные ссылки
            if (addinverse)
            {
                var query = inverseObjIndex.GetAllByKey(id);
                string predicate = null;
                XElement inverse = null;
                foreach (PaEntry en in query)
                {
                    var rec = (object[]) en.Get();
                    string pred = (string) rec[1];
                    if (pred != predicate)
                    {
                        res.Add(inverse);
                        inverse = new XElement("inverse", new XAttribute("prop", pred));
                        predicate = pred;
                    }
                    string idd = (string) rec[0];
                    inverse.Add(new XElement("record", new XAttribute("id", idd)));
                }
                res.Add(inverse);
            }
            return res;
        }

        public override void Load(params string[] rdfFiles)
        {
            Load(Int32.MaxValue, rdfFiles);
            //CreateIndexes();
            LoadIndexes();
        }

        public override void CreateGraph()
        {
        //  CreateIndexes();
        }

        public override IEnumerable<string> GetEntities()
        {
            var existed = new HashSet<string>();
            foreach (var id in dataCell.Root.Elements().Select(entry => entry.Field(0).Get()).Cast<string>().Where(s => !existed.Contains(s)))
            {
                existed.Add(id);
                yield return id;
            }
        }

        public override IEnumerable<PredicateEntityPair> GetDirect(string id, object nodeInfo = null)
        {
            IEnumerable<PredicateEntityPair> cached;
            if (cacheDirect.TryGetValue(id, out cached)) return cached;
            cacheDirect.Add(id,cached=directObjIndex.GetAllByKey(id).Select(entry => new PredicateEntityPair((string)entry.Field(1).Get(), (string)entry.Field(2).Get())));
            return cached; 
        }

        readonly Dictionary<string, IEnumerable<PredicateEntityPair>> cacheDirect = new Dictionary<string, IEnumerable<PredicateEntityPair>>(),
                                                                      cacheObjInverse=new Dictionary<string, IEnumerable<PredicateEntityPair>>(),
                                                                      cacheDataInverse=new Dictionary<string, IEnumerable<PredicateEntityPair>>();
                                                                        
        readonly Dictionary<string, IEnumerable<PredicateDataTriple>> cacheData = new Dictionary<string, IEnumerable<PredicateDataTriple>>();
        readonly Dictionary<string, IEnumerable<DataLangPair>> cacheDataLangPredicate = new Dictionary<string, IEnumerable<DataLangPair>>();
        readonly Dictionary<string, IEnumerable<string>> cacheDataPredicate = new Dictionary<string, IEnumerable<string>>(),
                                                         cacheDirectPredicate=new Dictionary<string, IEnumerable<string>>(),
                                                         cacheInverseDirectPredicate=new Dictionary<string, IEnumerable<string>>(),
                                                         cacheInverseDataPredicate=new Dictionary<string, IEnumerable<string>>();

        private readonly string path;

        public override IEnumerable<PredicateEntityPair> GetInverse(string id, object nodeInfo = null)
        {
            IEnumerable<PredicateEntityPair> cached;
            if (cacheObjInverse.TryGetValue(id, out cached)) return cached;

            cacheObjInverse.Add(id,
                cached =
                    inverseObjIndex.GetAllByKey(id)
                        .Select(entry => new PredicateEntityPair((string) entry.Field(1).Get(), (string) entry.Field(0).Get())));
            return cached;
        }

        public override IEnumerable<PredicateDataTriple> GetData(string id, object nodeInfo = null)
        {
            IEnumerable<PredicateDataTriple> cached;
            if (cacheData.TryGetValue(id, out cached)) return cached;
            cacheData.Add(id,
                cached =
                    directDataIndex.GetAllByKey(id)
                        .Select(
                            entry =>
                                new PredicateDataTriple((string) entry.Field(1).Get(), (string) entry.Field(2).Get(),
                                    (string) entry.Field(3).Get())));
            return cached;
        }

        public override IEnumerable<PredicateEntityPair> GetSubjectsByData(string data, object nodeInfo = null)
        {
            IEnumerable<PredicateEntityPair> cached;
            if (cacheDataInverse.TryGetValue(data, out cached)) return cached;
            cacheDataInverse.Add(data,
                cached =
                   inverseDataIndex.GetAllByKey(data)
                        .Select(entry => new PredicateEntityPair((string)entry.Field(1).Get(), (string)entry.Field(0).Get())));
            return cached;
        }

        public override IEnumerable<string> GetDirect(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<string> cached;
            if (cacheDirectPredicate.TryGetValue(id+predicate, out cached)) return cached;
            cacheDirectPredicate.Add(id + predicate,
                cached =
                    spDirectIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry => (string) entry.Field(2).Get()));
            return cached;
        }

        public override IEnumerable<string> GetInverse(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<string> cached;
            if (cacheInverseDirectPredicate.TryGetValue(id + predicate, out cached)) return cached;
            cacheInverseDirectPredicate.Add(id + predicate,
                cached =
                    opDirectIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry => (string)entry.Field(0).Get()));
            return cached;
        }

        public override IEnumerable<string> GetData(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<string> cached;
            if (cacheDataPredicate.TryGetValue(id + predicate, out cached)) return cached;
            cacheDataPredicate.Add(id + predicate,
                cached =
                    spDataIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry =>(string)entry.Field(2).Get()));
            return cached;
        }

        public override IEnumerable<string> GetSubjectsByData(string data, string predicate, object nodeInfo = null)
        {
            IEnumerable<string> cached;
            if (cacheInverseDataPredicate.TryGetValue(data + predicate, out cached)) return cached;
            cacheInverseDataPredicate.Add(data + predicate,
                cached =
                    opDataIndex.GetAllByKey(new SubjPred(data, predicate))
                        .Select(entry => (string)entry.Field(0).Get()));
            return cached;
        }

        public override IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<DataLangPair> cached;
            if (cacheDataLangPredicate.TryGetValue(id+predicate, out cached)) return cached;
            cacheDataLangPredicate.Add(id + predicate,
                cached =
                    spDataIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry => new DataLangPair((string)entry.Field(2).Get(), (string)entry.Field(3).Get())));
            return cached;
        }

        public override void GetItembyId(string id)
        {
            throw new NotImplementedException();
        }

        public override void Test()
        {
            string testId = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature13";
            var testDAta = GetData(testId);
            var pred = testDAta.First().predicate;
            var testDAtaPred = GetData(testId, pred);
            var testDataLang = GetDataLangPairs(testId, pred);
            var testInverse = GetInverse(testId);
            var inverseFirst = testInverse.FirstOrDefault();
            if(Equals(inverseFirst, default(PredicateEntityPair))) return;
            var testInversePredicate = GetInverse(testId, inverseFirst.predicate);

        }

        public override string[] SearchByName(string ss)
        {
            throw new NotImplementedException();
        }

        public override object GetNodeInfo(string id)
        {
            throw new NotImplementedException();
        }
    }
}
