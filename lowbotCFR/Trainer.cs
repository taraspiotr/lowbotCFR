using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using MLApp;

namespace lowbotCFR
{
    public static class StaticRandom
    {
        private static int seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static int Rand()
        {
            return random.Value.Next();
        }
    }

    internal class Trainer
    {
        private readonly int iterations;
        private readonly int num_threads;
        private readonly string path;
        private Stopwatch watch;
        private static SerializableDictionary<String, Node> NodeMap;
        private Draw Draw;
        private readonly double[] range1;
        private readonly double[] range2;
        private static List<string>[] buckets;
        private MLApp.MLApp MATLAB = new MLApp.MLApp();
        private readonly int NUM_BUCKETS;

        //public static double[] bucketsUtil_zero;
        //public static double[] bucketsUtil_one;
        //public static int[] bucketsCount_zero;
        //public static int[] bucketsCount_one;
        public static ConcurrentDictionary<int, double[]> buckets_data;

        private static bool bucketFlag = false;
        private static Random rnd;
        private static int won;
        private static int lost;

        public Trainer(int iter, int nt, Draw d, int num_buckets)
        {
            NodeMap = new SerializableDictionary<string, Node>();
            iterations = iter;
            num_threads = nt;
            Draw = d;
            rnd = new Random();
            buckets = BucketHands.GetBuckets(num_buckets);
            NUM_BUCKETS = num_buckets;
            MATLAB.Execute(@"cd C:\lowbotCFR\MATLAB");
        }

        public Trainer(int iter, int nt, Draw d, double[] r1, double[] r2, int num_buckets)
        {
            NodeMap = new SerializableDictionary<string, Node>();
            iterations = iter;
            num_threads = nt;
            Draw = d;
            rnd = new Random();
            range1 = r1;
            range2 = r2;
            bucketFlag = true;
            won = 0;
            lost = 0;
            buckets = BucketHands.GetBuckets(num_buckets);
            NUM_BUCKETS = num_buckets;
            buckets_data = new ConcurrentDictionary<int, double[]>();
            MATLAB.Execute(@"cd C:\lowbotCFR\MATLAB");

            for (int i = 0; i < num_buckets; i++)
            {
                buckets_data[i] = new double[4];
                for (int j = 0; j < 4; j++)
                    buckets_data[i][j] = 0.0;
            }
        }

        public Trainer(int iter, int nt, Draw d, string file, int num_buckets)
        {
            NodeMap = new SerializableDictionary<string, Node>();
            iterations = iter;
            num_threads = nt;
            Draw = d;
            rnd = new Random();
            path = file;
            if (path != "")
            {
                using (XmlReader reader = XmlReader.Create(path))
                {
                    NodeMap.ReadXml(reader);
                }
            }
            buckets = BucketHands.GetBuckets(num_buckets);
            NUM_BUCKETS = num_buckets;
            MATLAB.Execute(@"cd C:\lowbotCFR\MATLAB");
        }

        public void SaveToFile(string FileName)
        {
            string Path = @"E:\Lowbot\" + FileName;
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            settings.NewLineOnAttributes = true;
            settings.ConformanceLevel = ConformanceLevel.Auto;
            using (XmlWriter writer = XmlWriter.Create(Path, settings))
            {
                NodeMap.WriteXml(writer);
            }
            Path = @"E:\Lowbot\Strategy\" + FileName;
            using (TextWriter tw = new StreamWriter(Path))
            {
                foreach (Node n in NodeMap.Values)
                    tw.WriteLine(n.ToString());
            }
        }

        private int GetBucket(double[] range)
        {
            double r = rnd.NextDouble();
            double probSum = 0.0;
            int i = 0;
            while (i < range.Length - 1)
            {
                probSum += range[i];
                if (r < probSum)
                    break;
                i++;
            }

            return i;
        }

        private int GetBucket(string Hand)
        {
            for (int i = 0; i < buckets.Length; ++i)
            {
                if (buckets[i].Contains(Hand))
                    return i;
            }

            return -1;
        }

        private double[] GetRanges(string History)
        {
            double[] Range1 = new double[NUM_BUCKETS];
            double[] Range2 = new double[NUM_BUCKETS];

            for (int i = 0; i < buckets.Length; ++i)
            {
                for (int j = 0; j < buckets[i].Count; ++j)
                {
                    double p0 = 1; double p1 = 1;
                    for (int k = 1; k < History.Length; ++k)
                    {
                        string tempHist = History.Substring(0, k);
                        char action = History[k];

                        int Player = Draw.GetCurrentPlayer(History);
                        int Opponent = 1 - Player;

                        string Actions = Draw.GetLegalActions(History);
                        double mod = NodeMap[buckets[i][j] + History].GetAverageStrategy()[Actions.IndexOf(action)];

                        if (Player == 0)
                            p0 *= mod;
                        else
                            p1 *= mod;

                    }
                    Range1[i] += p0 / buckets[i].Count;
                    Range2[i] += p1 / buckets[i].Count;
                }
            }
            double sum1 = 0; double sum2 = 0;
            for (int i = 0; i < NUM_BUCKETS; ++i)
            {
                sum1 += Range1[i];
                sum2 += Range2[i];
            }
            for (int i = 0; i < NUM_BUCKETS; ++i)
            {
                Range1[i] /= sum1;
                Range2[i] /= sum2;
            }

            return Range1.Concat(Range2).ToArray();
        }

        private double Train(int iter, int ID, int bu)
        {
            if (ID == 0)
            {
                watch = new Stopwatch();
                watch.Start();
            }
            double Util = 0.0;

            for (int i = 1; i <= iter; i++)
            {
                Util += Iteration(i, iter, ID, bu);
            }
            if (ID == 0)
            {
                watch.Stop();
            }
            return Util;
        }

        private double Iteration(int i, int iter, int ID, int bu)
        {
            string Hand1, Hand2;
            string Deck;
            int b1 = 0, b2 = 0;

            if (!bucketFlag)
            {
                Deck = Draw.GenerateDeck();
                Hand1 = Draw.SortHand(Deck.Substring(0, Draw.HAND_CARDS));
                Hand2 = Draw.SortHand(Deck.Substring(Draw.HAND_CARDS, Draw.HAND_CARDS));
            }
            else
            {
                Deck = "";
                b1 = GetBucket(range1);
                b2 = GetBucket(range2);
                Hand1 = buckets[b1][rnd.Next(buckets[b1].Count)];
                Hand2 = buckets[b2][rnd.Next(buckets[b2].Count)];
                //if (Hand1 == Hand2)
                //    Console.WriteLine("fahsdfasdf");
            }

            double Util = -CFR(Deck, "r", Hand1, Hand2, 0.5, 1, 1, 1);

            if (bucketFlag)
            {
                buckets_data[b1][0] += 1;
                buckets_data[b1][1] += Util;
                buckets_data[b2][2] += 1;
                buckets_data[b2][3] -= Util;
                //Console.WriteLine("\n{0}, {1}, {2}, {3}, {4}, {5}", b1, b2, buckets_data[b1][0] += 1, buckets_data[b1][1] += Util, buckets_data[b2][2] += 1, buckets_data[b2][3] -= Util);
                int outcome = Draw.CompareHands(Hand1, Hand2);
                if (outcome > 0)
                    won += 2;
                else if (outcome < 0)
                    lost += 2;
                else
                {
                    won += 1;
                    lost += 1;
                }
            }

            if (ID == 0)
            {
                if (i % 1 == 0)
                    Console.Write("\rProgress: {0}%\tEstimated time left: {1}\t\t\t\t\t", (long)i * 100 / iter, GetTime((watch.ElapsedMilliseconds / 1000) * (iter - i) / i));

                if (i % bu == 0 && !bucketFlag)
                    SaveToFile("strategy_backup.xml");
            }

            return Util;
        }

        public static string GetTime(long seconds)
        {
            long minutes = seconds / 60;
            seconds %= 60;
            long hours = minutes / 60;
            minutes %= 60;

            string time = seconds.ToString() + " seconds";
            if (minutes > 0 || hours > 0)
                time = minutes.ToString() + " minutes " + time;
            if (hours > 0)
                time = hours.ToString() + " hours " + time;

            return time;
        }



        private double CFR(string Deck, string History, string Hand1, string Hand2, double Pot1, double Pot2, double p0, double p1)
        {
            int Player = Draw.GetCurrentPlayer(History);
            int Opponent = 1 - Player;
            string PlayerHand = (Player == 0) ? Hand1 : Hand2;
            string OpponentHand = (Opponent == 0) ? Hand1 : Hand2;

            double PlayerPot = (Player == 0) ? Pot1 : Pot2;
            double OpponentPot = (Opponent == 0) ? Pot1 : Pot2;

            string Actions = Draw.GetLegalActions(History);

            if (History[History.Length - 1] == ')')
            {
                double[] ranges = GetRanges(History);
                int b = GetBucket(PlayerHand);
                object result = null;
                MATLAB.Feval("nnet", 2, out result, ranges);
                double[] res = result as double[];
                return res[Player * NUM_BUCKETS + b];
            }

            if (Actions == Draw.TERMINAL_FOLD)
                return OpponentPot;

            if (Actions == Draw.TERMINAL_CALL)
                return OpponentPot * Draw.CompareHands(PlayerHand, OpponentHand);

            if (p0 == 0 && p1 == 0)
                return 0;
            
            string InfoSet = PlayerHand.Substring(PlayerHand.Length - Draw.HAND_CARDS, Draw.HAND_CARDS) + History;

            Node Node = null;
            int NumActions;

            if (Actions == Draw.DRAW || Actions == Draw.LAST_DRAW)
                NumActions = (int)Math.Pow(2, Draw.HAND_CARDS);
            else
                NumActions = Actions.Length;
            Node = new Node();
            Node.Init(NumActions, Actions, InfoSet);

            if (!NodeMap.TryAdd(InfoSet, Node))
            {
                Node = NodeMap[InfoSet];
                Node.Count++;
            }

            double[] Strategy = GetStrategy(Node, (Player == 0) ? p0 : p1);

            double[] Util = new double[NumActions];
            double NodeUtil = 0.0;

            for (int i = 0; i < NumActions; i++)
            {
                string NextHistory;
                double NewPot = PlayerPot;
                string NewHand = String.Copy(PlayerHand);
                if (Actions == Draw.DRAW || Actions == Draw.LAST_DRAW)
                {
                    int NumDraw = Draw.DrawCards(History, Deck, PlayerHand, ref NewHand, i);
                    if (Actions == Draw.DRAW)
                        NextHistory = History + "(" + Convert.ToString(NumDraw);
                    else
                        NextHistory = History + Convert.ToString(NumDraw) + ")";
                }
                else
                {
                    NextHistory = History + Actions[i];
                    if (Actions[i] == 'r')
                        NewPot = OpponentPot * 2;
                    else if (Actions[i] == 'p')
                        NewPot = OpponentPot * 3;
                    else if (Actions[i] == 'c')
                        NewPot = OpponentPot;
                }

                Util[i] = (Player == 0) ? -CFR(Deck, NextHistory, NewHand, OpponentHand, NewPot, OpponentPot, p0 * Strategy[i], p1) : -CFR(Deck, NextHistory, OpponentHand, NewHand, OpponentPot, NewPot, p0, p1 * Strategy[i]);
                if (Draw.GetCurrentPlayer(History) == Draw.GetCurrentPlayer(NextHistory))
                    Util[i] = -Util[i];

                NodeUtil += Strategy[i] * Util[i];
            }

            for (int i = 0; i < NumActions; i++)
            {
                double Regret = Util[i] - NodeUtil;
                Node.RegretSum[i] += Regret * ((Player == 0) ? p1 : p0);
            }

            Node.NodeUtil += NodeUtil;
            return NodeUtil;
        }

        public double[] GetStrategy(Node Node, double RealizationWeight)
        {
            double NormalizingSum = 0.0;
            double[] Strategy = new double[Node.NumActions];

            for (int i = 0; i < Node.NumActions; i++)
            {
                Strategy[i] = (Node.RegretSum[i] > 0) ? Node.RegretSum[i] : 0;
                NormalizingSum += Strategy[i];
            }

            for (int i = 0; i < Node.NumActions; i++)
            {
                if (NormalizingSum > 0)
                    Strategy[i] /= NormalizingSum;
                else
                    Strategy[i] = 1.0 / Node.NumActions;

                Node.StrategySum[i] += RealizationWeight * Strategy[i];
            }

            return Strategy;
        }

        public double main()
        {
            List<Task<double>> Tasks = new List<Task<double>>();
            double Util = 0.0;
            for (int i = 0; i < num_threads; i++)
            {
                int temp = i;
                Tasks.Add(Task.Factory.StartNew<double>(() => Train(iterations / num_threads, temp, iterations / (20 * num_threads))));
            }
            foreach (Task<double> T in Tasks)
                Util += T.Result;

            if (bucketFlag)
            {
                for (int i = 0; i < buckets_data.Keys.Count; i++)
                {
                    if (buckets_data[i][0] != 0)
                        buckets_data[i][1] /= buckets_data[i][0];
                    if (buckets_data[i][2] != 0)
                        buckets_data[i][3] /= buckets_data[i][2];
                }
                double equity = (double)won / (won + lost);
                Console.WriteLine("\nEquity = {0}%, chop value = {1}", (int)(equity * 100), 2 * equity - 1);
            }
            Console.WriteLine("\nAverage game value: {0}", Util / iterations);
            if (!bucketFlag)
                SaveToFile("strategy.xml");


            Console.WriteLine("\n\nLets play!!!\n\n");



            return Util / iterations;
        }
    }
}