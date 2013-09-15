using System;
using System.Diagnostics;
using System.IO;

namespace CommonRDF
{
    class LeshProgram
    {
        private Graph gr;
        public LeshProgram(Graph gr)
        {
            this.gr = gr;
        }
        public void Run()
        {
            Stopwatch timer = new Stopwatch();
            timer.Restart();
         var query = new Query(@"..\..\query.txt", gr);
            timer.Stop();
            using (var f = new StreamWriter(@"..\..\Output.txt", false))
                f.WriteLine("read query time {0}ms {1}ticks, memory {2}"
       , timer.ElapsedMilliseconds, timer.ElapsedTicks / 10000L,
       GC.GetTotalMemory(true) / (1024L * 1024L));

            timer = new Stopwatch();
            timer.Start();
            query.Run();
            timer.Stop();
            using (var f = new StreamWriter(@"..\..\Output.txt", true))
                f.WriteLine("run query time {0}ms {1}ticks, memory {2}"
      , timer.ElapsedMilliseconds, timer.ElapsedTicks / 10000L,
      GC.GetTotalMemory(true) / (1024L * 1024L));

            if (query.SelectParameters.Count == 0)
                query.OutputParamsAll(@"..\..\Output.txt");
            else
                query.OutputParamsBySelect(@"..\..\Output.txt");

        }
    }
}
