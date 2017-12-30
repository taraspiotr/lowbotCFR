using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace lowbotCFR
{
    internal class DrawTrainer
    {
        private readonly int iterations;
        private readonly int num_threads;
        private readonly string path;
        private Stopwatch watch;
        private static SerializableDictionary<String, Node> NodeMap = new SerializableDictionary<string, Node>();

        public DrawTrainer(int iter, int nt)
        {
            iterations = iter;
            num_threads = nt;
        }

        public DrawTrainer(int iter, int nt, string file)
        {
            iterations = iter;
            num_threads = nt;
            path = file;
            using (XmlReader reader = XmlReader.Create(path))
            {
                NodeMap.ReadXml(reader);
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
            string Deck = Draw.GenerateDeck();
            string Hand1 = Draw.SortHand(Deck.Substring(0, Draw.HAND_CARDS));
            string Hand2 = Draw.SortHand(Deck.Substring(Draw.HAND_CARDS, Draw.HAND_CARDS));

            double Util = CFR(Deck, "r", Hand1, Hand2, 1, 1);

            if (ID == 0 && i % 1000 == 0)
            {
                Console.Write("\rProgress: {0}%\tEstimated time left: {1}\t\t\t\t\t", i * 100 / iter, GetTime((watch.ElapsedMilliseconds / 1000) * (iter - i) / i));

            }

            if (i % bu == 0)
                SaveToFile("strategy_backup.xml");

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

        private double CFR(string Deck, string History, string Hand1, string Hand2, double p0, double p1)
        {
            int Player = Draw.GetCurrentPlayer(History);
            int Opponent = 1 - Player;
            string PlayerHand = (Player == 0) ? Hand1 : Hand2;
            string OpponentHand = (Opponent == 0) ? Hand1 : Hand2;
            string Actions = Draw.GetLegalActions(History);

            if (Actions == Draw.TERMINAL_FOLD)
                return Draw.GetPotContribution(History, Opponent);

            if (Actions == Draw.TERMINAL_CALL)
                return Draw.GetPotContribution(History, Opponent) * Draw.CompareHands(PlayerHand, OpponentHand);

            if (p0 == 0 && p1 == 0)
                return 0;

            string InfoSet = Draw.CreateInfoSet(History, PlayerHand);

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
                    NextHistory = History + Actions[i];

                Util[i] = (Player == 0) ? -CFR(Deck, NextHistory, NewHand, OpponentHand, p0 * Strategy[i], p1) : -CFR(Deck, NextHistory, OpponentHand, NewHand, p0, p1 * Strategy[i]);
                if (Draw.GetCurrentPlayer(History) == Draw.GetCurrentPlayer(NextHistory))
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

        public void main()
        {
            List<Task<double>> Tasks = new List<Task<double>>();
            double Util = 0.0;
            for (int i = 0; i < num_threads; i++)
            {
                int temp = i;
                Tasks.Add(Task.Factory.StartNew<double>(() => Train(iterations / num_threads, temp, 1000000 / num_threads)));
            }
            foreach (Task<double> T in Tasks)
                Util += T.Result;

            Console.WriteLine("\nAverage game value: {0}", Util / iterations);
            SaveToFile("strategy.xml");
        }
    }
}