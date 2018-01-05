using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace lowbotCFR
{
    internal class SzuTrainer
    {
        private readonly int iterations;
        private readonly int num_threads;
        private readonly string path;
        private Stopwatch watch;
        private static SerializableDictionary<String, Node> NodeMap;
        private Szu Szu;
        private readonly double[] range1;
        private readonly double[] range2;
        private static List<string>[] buckets;

        public SzuTrainer(int iter, int nt, Szu d)
        {
            NodeMap = new SerializableDictionary<string, Node>();
            iterations = iter;
            num_threads = nt;
            Szu = d;
        }

        public SzuTrainer(int iter, int nt, Szu d, double[] r1, double[] r2, int num_buckets)
        {
            NodeMap = new SerializableDictionary<string, Node>();
            iterations = iter;
            num_threads = nt;
            Szu = d;
            range1 = r1;
            range2 = r2;
            buckets = BucketHands.GetBuckets(num_buckets);
        }

        public SzuTrainer(int iter, int nt, Szu d, string file)
        {
            NodeMap = new SerializableDictionary<string, Node>();
            iterations = iter;
            num_threads = nt;
            Szu = d;
            path = file;
            if (path != "")
            {
                using (XmlReader reader = XmlReader.Create(path))
                {
                    NodeMap.ReadXml(reader);
                }
            }
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
            Random rng = new Random();
            double r = rng.NextDouble();
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
            string Deck = Szu.GenerateDeck();
            string Hand1 = Szu.SortHand(Deck.Substring(0, Szu.HAND_CARDS));
            string Hand2 = Szu.SortHand(Deck.Substring(Szu.HAND_CARDS, Szu.HAND_CARDS));
            //Random rnd = new Random();
            //int b1 = GetBucket(range1); int b2 = GetBucket(range2);
            //string Hand1 = buckets[b1][rnd.Next(buckets[b1].Count)];
            //string Hand2 = buckets[b2][rnd.Next(buckets[b2].Count)];

            double Util = CFR("", "r", Hand1, Hand2, 0.5, 1, 1, 1);

            if (ID == 0)
            {
                if (i % 1 == 0)
                    Console.Write("\rProgress: {0}%\tEstimated time left: {1}\t\t\t\t\t", (long)i * 100 / iter, GetTime((watch.ElapsedMilliseconds / 1000) * (iter - i) / i));

                if (i % bu == 0)
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
            int Player = Szu.GetCurrentPlayer(History);
            int Opponent = 1 - Player;
            string PlayerHand = (Player == 0) ? Hand1 : Hand2;
            string OpponentHand = (Opponent == 0) ? Hand1 : Hand2;

            double PlayerPot = (Player == 0) ? Pot1 : Pot2;
            double OpponentPot = (Opponent == 0) ? Pot1 : Pot2;

            string Actions = Szu.GetLegalActions(History);

            if (Actions == Szu.TERMINAL_FOLD)
                return OpponentPot;
            //return Szu.GetPotContribution(History, Opponent);

            if (Actions == Draw.TERMINAL_CALL)
                return OpponentPot * Szu.CompareHands(PlayerHand, OpponentHand);
            //return Szu.GetPotContribution(History, Opponent) * Szu.CompareHands(PlayerHand, OpponentHand);

            if (p0 == 0 && p1 == 0)
                return 0;

            //string InfoSet = Draw.CreateInfoSet(History, PlayerHand);
            string InfoSet = PlayerHand.Substring(PlayerHand.Length - Szu.HAND_CARDS, Szu.HAND_CARDS) + History;

            //if (InfoSet == "44rrc(20)65")
            //    Console.WriteLine("HERE!!!");

            Node Node = null;
            int NumActions;

            if (Actions == Draw.DRAW || Actions == Draw.LAST_DRAW)
                NumActions = (int)Math.Pow(2, Szu.HAND_CARDS);
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
                    int NumDraw = Szu.DrawCards(History, Deck, PlayerHand, ref NewHand, i);
                    if (Actions == Szu.DRAW)
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
                if (Szu.GetCurrentPlayer(History) == Szu.GetCurrentPlayer(NextHistory))
                    Util[i] = -Util[i];

                NodeUtil += Strategy[i] * Util[i];
            }

            for (int i = 0; i < NumActions; i++)
            {
                double Regret = Util[i] - NodeUtil;
                Node.RegretSum[i] += Regret * ((Player == 0) ? p1 : p0);
            }

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

            Console.WriteLine("\nAverage game value: {0}", Util / iterations);
            SaveToFile("strategy.xml");

            return Util / iterations;
        }
    }
}