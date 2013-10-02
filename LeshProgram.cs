using System.IO;

namespace CommonRDF
{
    class LeshProgram
    {
        private readonly GraphBase gr;
        public LeshProgram(GraphBase gr)
        {
            this.gr = gr;
        }

        public void Run()
        {
            Query query = null;
            var text = File.ReadAllText(@"..\..\query.txt");

            Perfomance.ComputeTime(() =>
                //@"..\..\query.txt"
            {
                query = new Query(text, gr);
            }, "read query ", true);

            Perfomance.ComputeTime(()=>query.Match(), "run query ", true);

            if (query.SelectParameters.Count == 0)
                query.OutputParamsAll(@"..\..\Output.txt");
            else
                query.OutputParamsBySelect(@"..\..\Output.txt");
        }
    }
}
