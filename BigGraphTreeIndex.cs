using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BinaryTree;
using PolarDB;

namespace CommonRDF
{
    class BigGraphIndexTree : GraphBase
    {
        private PxCell graphPxCell;
        private BTree indexTree;
        private readonly GraphTripletsTree graphTripletsTree;
        public override void Load(params string[] rdfFiles)
        {
            graphTripletsTree.Load(rdfFiles);
        }

        public BigGraphIndexTree(string path)
        {

            if (path[path.Length - 1] != '\\' && path[path.Length - 1] == '/') path = path + "/";
            this.path = path;
            filePathIndexes = this.path + "indexes.pxc";
            graphPath = path + "graph.pxc";
            InitCells();
            graphTripletsTree=new GraphTripletsTree(path);
        }

        private void InitCells()
        {
            //   string filePathN4 = path + "n4_x.pxc";
            if (!File.Exists(graphPath) || !File.Exists(filePathIndexes)) return;

            graphPxCell = new PxCell(TreePType, graphPath);
            indexTree=new BTree(ptIndexTree, elementDepth,
                filePathIndexes);
        }

        public override void CreateGraph()
        {
            if (indexTree != null) indexTree.Close();
            if (graphPxCell != null) graphPxCell.Close();
            if (File.Exists(filePathIndexes)) File.Delete(filePathIndexes);
            if (File.Exists(graphPath)) File.Delete(graphPath);
            string tempGraphfilePath = path + "gprah.pac";
            if (File.Exists(tempGraphfilePath)) File.Delete(tempGraphfilePath);
            graphTripletsTree.CreateGraph();
            var offsetById = new Dictionary<string, long>();
            var createTreeCell = new PaCell(TreePType, tempGraphfilePath, false);
            var buffer = new SerialBuffer(createTreeCell);
            buffer.StartSerialFlow();
            buffer.S();
            foreach (var idInfo in graphTripletsTree.GetEntitiesWithIdNodeInfo())
            {
                //if (offsetById.ContainsKey(idInfo.Id)) offsetById[idInfo.Id] = buffer.TotalVolume;
                //else offsetById.Add(idInfo.Id, buffer.TotalVolume);
                var direct = graphTripletsTree.GetDirect(idInfo.Id, idInfo.NodeInfo)
                    .GroupBy(pair => pair.predicate)
                    .Select(pair => new object[]
                    {
                        pair.Key.GetHashCode(),
                        pair.Select(entityPair => new object[] {entityPair.entity, -1L})
                            .ToArray()
                    })
                    .ToArray();
                var inverse = graphTripletsTree.GetInverse(idInfo.Id, idInfo.NodeInfo)
                    .GroupBy(pair => pair.predicate)
                    .Select(pair => new object[]
                    {
                        pair.Key.GetHashCode(),
                        pair.Select(entityPair => new object[] {entityPair.entity, -1L})
                            .ToArray()
                    })
                    .ToArray();
                var data = graphTripletsTree.GetData(idInfo.Id, idInfo.NodeInfo)
                    .GroupBy(pair => pair.predicate)
                    .Select(pair => new object[]
                    {
                        pair.Key.GetHashCode(),
                        pair.Select(dataPair => new object[] {dataPair.data, dataPair.lang})
                            .ToArray(),
                            
                    })
                    .ToArray();
                var newNode = new object[] {direct, inverse, data, idInfo.Id};
                buffer.V(newNode);
            }
            buffer.Se();
            buffer.EndSerialFlow();
            graphPxCell = new PxCell(TreePType, graphPath, false);
            graphPxCell.Fill2(createTreeCell.Root.Get());

            foreach (var node in graphPxCell.Root.Elements())
            {
                var id = node.Field(3).Get() as string;
                if (offsetById.ContainsKey(id)) offsetById[id] = node.offset;
                else offsetById.Add(id,  node.offset);
            }
            foreach (var node in graphPxCell.Root.Elements())
            {
                foreach (var idOffsetEntry in 
                    node
                        .Field(0)
                        .Elements()
                        .Concat(
                            node
                                .Field(1)
                                .Elements())
                        .SelectMany(direct => direct
                            .Field(1)
                            .Elements()))
                {
                    var id = (string) idOffsetEntry.Field(0).Get();
                    long offsetSubId = 0;
                    if (!offsetById.TryGetValue(id, out offsetSubId)) continue;
                    idOffsetEntry.Field(1).Set(offsetSubId);
                }
            }
            indexTree= offsetById.Select(index=>new
                                            {
                                                hash=index.Key.GetHashCode(),
                                                offset=index.Value,
                                                id=index.Key
                                            })
                              .GroupBy(indexe=>indexe.hash)
                              .OrderBy(index => index.Key)
                              .Select(group => new object[] 
                                            {
                                                group.Key, 
                                                group.Select(index=>new object[]{index.id, index.offset})
                                                     .ToArray()
                                            })
                              .ToBTree(ptIndexTree, filePathIndexes, elementDepth, o => (int)(((object[])o)[0]));


             createTreeCell.Close();
            File.Delete(tempGraphfilePath);
            graphPxCell.Close();
            indexTree.Close();
            InitCells();
        }

        public override IEnumerable<string> GetEntities()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<PredicateEntityPair> GetDirect(string id, object nodeInfo = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<PredicateEntityPair> GetInverse(string id, object nodeInfo = null)
        {
            throw new NotImplementedException();
        }
        
           public override IEnumerable<string> GetDirect(string id, string predicate, object nodeInfo = null)
           {
               foreach (GraphTripletsTree.IdNodeInfo idOffset in GetDirectWithNodeInfo(id, predicate, nodeInfo))
               {
                   if(!idInfoCache.ContainsKey(idOffset.Id)) idInfoCache.Add(idOffset.Id, idOffset.NodeInfo);
                   yield return idOffset.Id;
               }
           }

        public IEnumerable<GraphTripletsTree.IdNodeInfo> GetDirectWithNodeInfo(string id, string predicate, object nodeInfo = null)
           {
               return GetProperty(id, 0, nodeInfo, predicate.GetHashCode());
           }
        public override IEnumerable<string> GetInverse(string id, string predicate, object nodeInfo = null)
        {
            foreach (GraphTripletsTree.IdNodeInfo idNodeInfo in GetProperty(id, 1, nodeInfo, predicate.GetHashCode()))
            {
                if (!idInfoCache.ContainsKey(idNodeInfo.Id)) idInfoCache.Add(idNodeInfo.Id, idNodeInfo.NodeInfo);
                yield return idNodeInfo.Id;
            }
        }

        public override IEnumerable<string> GetData(string id, string predicate, object nodeInfo = null)
        {
            PxEntry found = GetEntryByOffset((nodeInfo is long?) ? ((long?)nodeInfo).Value : GetOffestById(id));
            if (found.offset == Int64.MinValue || found.IsEmpty) return Enumerable.Empty<string>();
            int hashCode = predicate.GetHashCode();
            return found.Field(2).Elements()
                .Where(pRec => (int)pRec.Field(0).Get()==hashCode)
                .Select(pRec =>
                    pRec.Field(1)
                        .Elements()
                        .Select(offEn => (string)offEn.Field(0).Get()))
                .SelectMany(resultsGroup => resultsGroup);
        }

        public override IEnumerable<PredicateDataTriple> GetData(string id, object nodeInfo = null)
        {
            PxEntry found = GetEntryByOffset((nodeInfo is long?) ? ((long?)nodeInfo).Value : GetOffestById(id));
            if (found.offset == Int64.MinValue || found.IsEmpty) return Enumerable.Empty<PredicateDataTriple>();
            return found.Field(2).Elements()
                .Select(pRec =>
                    pRec.Field(1)
                        .Elements()
                        .Select(
                            offEn =>
                                new PredicateDataTriple(GetPredicateByHash((long)pRec.Field(0).Get()),
                                    (string)offEn.Field(0).Get(),
                                    (string)offEn.Field(1).Get())))
                .SelectMany(resultsGroup => resultsGroup);
        }
        string GetPredicateByHash(long hash)
        {
            return "";
        }
        private IEnumerable<GraphTripletsTree.IdNodeInfo> GetProperty(string id, int direction,
            object node = null, int? predicateSC = null)
        {
            PxEntry found = GetEntryByOffset((node is long?) ? ((long?) node).Value : GetOffestById(id));
            if (found.offset == Int64.MinValue || found.IsEmpty) return Enumerable.Empty<GraphTripletsTree.IdNodeInfo>();
            IEnumerable<PxEntry> pxEntries = found.Field(direction).Elements();
            if (predicateSC != null)
                pxEntries = pxEntries
                    .Where(pRec => (int) pRec.Field(0).Get() == predicateSC.Value);
            return pxEntries
                .Select(pRec =>
                    pRec.Field(1)
                        .Elements()
                        .Select(
                            offEn =>
                                new GraphTripletsTree.IdNodeInfo((string) offEn.Field(0).Get(),
                                    (long) offEn.Field(1).Get())))
                .SelectMany(resultsGroup => resultsGroup);
            // Еще отбраковка tri => tri.p == predicate 
        }

        internal long GetOffestById(string id)
        {
            if (idInfoCache.ContainsKey(id)) return idInfoCache[id];
            int e_hs = id.GetHashCode();
            PxEntry found = indexTree.BinarySearch(element =>
            {
            //    int v = (int)element.Field(0).Get();
              //  return v < e_hs ? -1 : (v == e_hs ? 0 : 1);
                return e_hs - (int)element.Field(0).Get();
            });
            if (found.offset == Int64.MinValue) return Int64.MinValue;
            var idOffsetEntry =
                found
                .Field(1)
                    .Elements()
                    .FirstOrDefault(idOffsetEn => (string) idOffsetEn.Field(0).Get() == id);
            if (idOffsetEntry.Typ == null) return -1;
            long offestById = (long)idOffsetEntry.Field(1).Get();
            idInfoCache.Add(id, offestById);
            return offestById;
        }
        internal PxEntry GetEntryByOffset(long offset)
        {
            PxEntry any = graphPxCell.Root.Element(0);
            any.offset = offset;
            return any;
        }
        public override IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate, object nodeInfo = null)
        {
            return GetData(id, predicate, nodeInfo).Select(SplitLang);
        }

        

        public override void GetItembyId(string id)
        {
       
        }

        public override void Test()
        {
            int i=BTree.H(indexTree.Root);
            i++;

        }

        public override string[] SearchByName(string ss)
        {
            throw new NotImplementedException();
        }

        public override object GetNodeInfo(string id)
        {
            var info=  GetOffestById(id);
            return info;
        }

        readonly Dictionary<string, long> idInfoCache=new Dictionary<string, long>();

        private readonly PType ptIndexTree =
            new PTypeRecord(
                new NamedType("hash", new PType(PTypeEnumeration.integer)),
                new NamedType("nodeIdOffetsInGraph",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("id", new PType(PTypeEnumeration.sstring)),
                            new NamedType("offsetInGraph", new PType(PTypeEnumeration.longinteger))))));
        public static PType TreePType = new PTypeSequence(new PTypeRecord(
            new NamedType("direct",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("values", 
                                new PTypeSequence(
                                    new PTypeRecord(
                                        new NamedType("value", new PType(PTypeEnumeration.sstring)),
                                        new NamedType("offset", new PType(PTypeEnumeration.longinteger)))))))),
                new NamedType("inverse",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("values", 
                                new PTypeSequence(
                                    new PTypeRecord(
                                        new NamedType("value", new PType(PTypeEnumeration.sstring)),
                                        new NamedType("offset", new PType(PTypeEnumeration.longinteger)))))))),
                new NamedType("data",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("value", 
                    new PTypeSequence(
                                new PTypeRecord(
                                        new NamedType("value", new PType(PTypeEnumeration.sstring)),
                                        new NamedType("lang", new PType(PTypeEnumeration.sstring)))))))),
                new NamedType("id", new PType(PTypeEnumeration.sstring))));

        private string path;
        private string graphPath;
        private string filePathIndexes;
        private static readonly Func<object, PxEntry, int> elementDepth = (o, entry) => (int) ((long) ((object[]) o)[0] - (long) entry.Field(0).Get());
    }
}
