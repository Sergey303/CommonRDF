using System;
using System.Diagnostics;
using System.IO;

namespace CommonRDF
{
    public static class Perfomance
    {
        private static Stopwatch timer = new Stopwatch();

        /// <summary>
        /// Выводит в консоль время исполнения
        /// </summary>
        /// <param name="action">тестируемый метод</param>
        /// <param name="mesage"></param>
        /// <param name="outputFile">if true, write result at file</param>
        public static void ComputeTime(this Action action, string mesage, bool outputFile = false)
        {
            timer.Restart();
            action.Invoke();
            timer.Stop();
            if (!outputFile)
                Console.WriteLine("{0} {1}ms", mesage, timer.Elapsed.TotalMilliseconds);
            else
<<<<<<< HEAD
                using (StreamWriter file = new StreamWriter(@"..\Perfomance.txt", true))
=======
                using (StreamWriter file = new StreamWriter(@"F:\projects\CommonRDF\Perfomance.txt", true))
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
                    file.WriteLine("{0} {1}ms", mesage, timer.Elapsed.TotalMilliseconds);
        }
    }
}