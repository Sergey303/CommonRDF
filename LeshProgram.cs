using System.IO;
using System.Linq;

namespace CommonRDF
{
    internal class LeshProgram
    {
        private readonly GraphBase gr;

        public LeshProgram(GraphBase gr)
        {
            this.gr = gr;

        }

        public void Run()
        {
            gr.GetData(string.Empty);
            gr.GetDirect(string.Empty);
            gr.GetInverse(string.Empty);
            gr.GetSubjectsByData(string.Empty);
            // Perfomance.ComputeTime(RunQueries, "first query first run", true);

            foreach (  var file in new DirectoryInfo(@"..\..\\sparql data\queries").GetFiles())
            {
                Perfomance.ComputeTime(() =>
                {
                    Query q = new Query(File.ReadAllText(file.FullName), gr);
                    q.Match();
                    var result = q.Results;
                },file.Name, true);
            }
           
            //Perfomance.ComputeTime(() =>
            //{
            //    for (int i = 0; i < 1000; i++)
            //    {
            //        runQueries();
            //    }
            //}, "1000 runs of first query", true);
        }

        private void RunQueries()
        {
            var queries = new DirectoryInfo(@"..\..\\sparql data\queries").GetFiles()
                .Select(path =>
                {
                    //if(Path.GetExtension(path).ToLower()!=".rq") continue;"*.rq"
                    var text = File.ReadAllText(path.FullName);
                    Query query = null;
                 //   Perfomance.ComputeTime(() =>
                    {
                         query = new Query(text, gr);
                    } //, "read query ", true);
                    return new {query, path};
                })
                .ToArray();
            foreach (var qp in queries)
            {
                qp.query.Match();
                qp.query.Output(Path.ChangeExtension(qp.path.FullName.Replace("queries","results"), ".txt"));
            }
        }
    }
}
