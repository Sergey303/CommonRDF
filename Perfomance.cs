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
        public static void ComputeTime(this Action action, string mesage)
        {
            timer.Restart();
            action.Invoke();
            timer.Stop();
//            using (StreamWriter file = new StreamWriter(@"F:\projects\CommonRDF\Perfomance.txt", true))
//            {
////  Console.WriteLine("{0} {1}", 
                            Console.WriteLine("{0} {1}ticks", mesage, timer.Elapsed.Ticks/10000L );
//            }
        }
    }
}