﻿using PolarDB;
namespace CommonRDF
{
    class GraphDB : Graph
    {
        private PaCell paGraph;
        private PxCell pxGraph;
        public new void Load(string path)
        {
            if (paGraph != null) paGraph.Close();
            paGraph = new PaCell(tp_graph, path + "\\data.pac", true);
            if (pxGraph != null) pxGraph.Close();
            pxGraph = new PxCell(tp_graph, path + "\\data.pxc", true);
            if (pxGraph.IsEmpty)
            {
                if (paGraph.IsEmpty)
                {
                    base.Load(path);
                }
                pxGraph.Fill2(paGraph.Root.Get().Value);
                paGraph.Close();
            }

        }
        public static readonly PType tp_axe= new PTypeRecord(
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("variants", new PTypeSequence(new PType(PTypeEnumeration.sstring))));
        public static readonly PType tp_graph= new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("rtype", new PType(PTypeEnumeration.sstring)),
                new NamedType("direct", tp_axe),
                new NamedType("inverse", tp_axe),
                new NamedType("data", tp_axe)));
    }
}