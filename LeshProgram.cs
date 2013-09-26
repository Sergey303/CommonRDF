using System;
using System.Diagnostics;
using System.IO;

namespace CommonRDF
{
    class LeshProgram
    {
        private GraphBase gr;
        public LeshProgram(GraphBase gr)
        {
            this.gr = gr;
        }

        public void Run()
        {
            Query query = null;
            Perfomance.ComputeTime(() =>
                query = new Query(@"..\..\query.txt", gr), "read query");


            Perfomance.ComputeTime(()=>query.Run(), "run query");

            if (query.SelectParameters.Count == 0)
                query.OutputParamsAll(@"..\..\Output.txt");
            else
                query.OutputParamsBySelect(@"..\..\Output.txt");

        }
    }
}
