using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace lowbotCFR
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch StopWatch = new Stopwatch();
            StopWatch.Start();

            string Path = @"E:\Lowbot\strategy.xml";
            DrawTrainer T = new DrawTrainer(200000000, 6, Path);
            T.main();

            StopWatch.Stop();
            Console.WriteLine("Runtime = {0}", DrawTrainer.GetTime(StopWatch.ElapsedMilliseconds / 1000));
            Console.ReadKey();
        }
    }
}
