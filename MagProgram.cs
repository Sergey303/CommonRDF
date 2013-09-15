using System;

namespace CommonRDF
{
    class MagProgram
    {
        private Graph gr;
        public MagProgram(Graph gr)
        {
            this.gr = gr;
        }
        public void Run()
        {
            DateTime tt0 = DateTime.Now;
            string id = "piu_200809051791";
            SimpleSparql sims = new SimpleSparql(gr.Dics, id);
            Console.WriteLine("Test ok duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
