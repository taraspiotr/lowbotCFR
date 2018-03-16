using CommandLine;
using CommandLine.Text;
using System;
using System.Diagnostics;
using System.IO;

namespace lowbotCFR
{
    internal class Program
    {
        private class Options
        {
            [Option('f', "file", DefaultValue = "",
              HelpText = "Input file to be processed.")]
            public string InputFile { get; set; }

            [Option('v', "verbose", DefaultValue = false,
              HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [Option('t', "threads", Required = true,
                HelpText = "How many threads shoud be used")]
            public int Threads { get; set; }

            [Option('i', "iterations", Required = true,
                HelpText = "Set number of iterations")]
            public int Iterations { get; set; }

            [Option('c', "cards", DefaultValue = 13,
                HelpText = "Set number of cards in a deck")]
            public int NumCards { get; set; }

            [Option('s', "suits", DefaultValue = 4,
                HelpText = "Set number of suits in a deck")]
            public int NumSuits { get; set; }

            [Option('h', "hand", DefaultValue = 2,
                HelpText = "Set number of cards in a hand")]
            public int HandCards { get; set; }

            [Option('d', "draws", DefaultValue = 1,
                HelpText = "Set number of draws")]
            public int NumDraws { get; set; }

            [Option('r', "rounds", DefaultValue = 2,
                HelpText = "Set number of small bet rounds")]
            public int SBRounds { get; set; }

            [Option('p', "cap", DefaultValue = 2,
                HelpText = "Set CAP")]
            public int Cap { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }

        private static void Main(string[] args)
        {
            Stopwatch StopWatch = new Stopwatch();
            StopWatch.Start();
            Trainer T = new Trainer(50000000, 6, new Draw(hand_cards:1));
            T.main();
            //SzuTrainer K = new SzuTrainer(2000000, 6, new Szu());
            //K.main();
            ////BucketHands.Bucket(100, 2.5);

            StopWatch.Stop();

            //string History = "(JT)rc(AK)cc(22)asdf(Q3)fasdf(-)";
            //Razz Razz = new Razz(13, 4, 5, 2, 3);
            //Console.WriteLine(Razz.GetCurrentPlayer(History));
            //Console.ReadKey();

            //int num_buckets = 100;
            //Stopwatch StopWatch = new Stopwatch();
            //StopWatch.Start();
            //int i = 0;

            //while (true)
            //{
            //    Console.WriteLine("================ ITERATION #{0} ================", i + 1);
            //    Random rnd = new Random();
            //    double[] range1 = GenerateRange(num_buckets, rnd);
            //    double[] range2 = GenerateRange(num_buckets, rnd);

            //    SzuTrainer T = new SzuTrainer(1000000, 6, new Szu(), range1, range2, num_buckets);
            //    double util = T.main();
            //    double[] bucketsUtil_zero = new double[num_buckets];
            //    double[] bucketsUtil_one = new double[num_buckets];
            //    for (int k = 0; k < num_buckets; k++)
            //    {
            //        bucketsUtil_zero[k] = SzuTrainer.buckets_data[k][1];
            //        bucketsUtil_one[k] = SzuTrainer.buckets_data[k][3];
            //    }
            //    using (StreamWriter w = File.AppendText(@"E:\Lowbot\Xrange.csv"))
            //    {
            //        w.WriteLine(String.Join(";", range1) + ";" + String.Join(";", range2));
            //    }
            //    using (StreamWriter w = File.AppendText(@"E:\Lowbot\Yrange.csv"))
            //    {
            //        w.WriteLine(String.Join(";", bucketsUtil_zero) + ";" + String.Join(";", bucketsUtil_one));
            //    }
            //    Console.WriteLine("Average runtime = {0}", DrawTrainer.GetTime(StopWatch.ElapsedMilliseconds / ((i + 1) * 1000)));
            //    Console.WriteLine("Runtime = {0}", DrawTrainer.GetTime(StopWatch.ElapsedMilliseconds / 1000));
            //    i++;
            //}

            //StopWatch.Stop();
            //Console.WriteLine("Runtime = {0}", DrawTrainer.GetTime(StopWatch.ElapsedMilliseconds / 1000));
            //Console.ReadKey();

            //Draw Draw = new Draw(13, 4, 5, 0, 0, 0);
            //string Deck = Draw.GenerateDeckFull();
            //string Hand = Deck.Substring(0, 2 * Draw.HAND_CARDS);
            //Console.WriteLine(Hand);
            //Hand = Draw.SortHandFull(Hand);
            //Console.WriteLine(Hand);
            //Console.ReadKey();

            //var options = new Options();
            //if (CommandLine.Parser.Default.ParseArguments(args, options))
            //{
            //    Stopwatch StopWatch = new Stopwatch();
            //    StopWatch.Start();

            //    DrawTrainer T = new DrawTrainer(options.Iterations, options.Threads, new Draw(options.NumCards, options.NumSuits, options.HandCards, options.NumDraws, options.SBRounds, options.Cap), options.InputFile);
            //    T.main();

            //    StopWatch.Stop();
            //    Console.WriteLine("Runtime = {0}", DrawTrainer.GetTime(StopWatch.ElapsedMilliseconds / 1000));
            //}
            Console.ReadKey();
        }

        private static double[] GenerateRange(int num_buckets, Random rnd)
        {
            double[] range = new double[num_buckets];
            double sum = 0.0;
            double median_sum = 0.0;
            bool median_flag = false;

            for (int i = 0; i < num_buckets; i++)
            {
                range[i] = rnd.NextDouble();
                sum += range[i];
            }
            for (int i = 0; i < num_buckets; i++)
            {
                range[i] /= sum;
                median_sum += range[i];
                if (median_sum >= 0.5 && !median_flag)
                {
                    Console.WriteLine("Range median = {0}", i);
                    median_flag = true;
                }
            }

            return range;
        }
    }
}