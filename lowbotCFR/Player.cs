using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace lowbotCFR
{
    internal class Player
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

        public Player(int iter, int nt, Draw d, string file, int num_buckets)
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

        private double CFR(string Deck, string History, string Hand1, string Hand2, double Pot1, double Pot2, int Gamer)
        {
            int Player = Draw.GetCurrentPlayer(History);
            int Opponent = 1 - Player;
            string PlayerHand = (Player == 0) ? Hand1 : Hand2;
            string OpponentHand = (Opponent == 0) ? Hand1 : Hand2;

            double PlayerPot = (Player == 0) ? Pot1 : Pot2;
            double OpponentPot = (Opponent == 0) ? Pot1 : Pot2;

            string Actions = Draw.GetLegalActions(History);

            if (Actions == Draw.TERMINAL_FOLD)
                return (Player == Gamer) ? OpponentPot : -OpponentPot;

            if (Actions == Draw.TERMINAL_CALL)
                return (Player == Gamer) ? OpponentPot * Draw.CompareHands(PlayerHand, OpponentHand) : -OpponentPot * Draw.CompareHands(PlayerHand, OpponentHand);

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

            int i;

            if (Player == Gamer)
            {
                Console.WriteLine("Choose action from " + Actions + ":");
                i = Convert.ToInt32(Console.ReadKey());
            }
            else
                i = GetAction(Node);

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

            return (Player == 0) ? CFR(Deck, NextHistory, NewHand, OpponentHand, NewPot, OpponentPot, Gamer) : CFR(Deck, NextHistory, OpponentHand, NewHand, OpponentPot, NewPot, Gamer);
        }

        private int GetAction(Node Node)
        {
            double rand = rnd.NextDouble();
            double sum = 0.0;

            for (int i = 0; i < Node.Actions.Length; ++i)
            {
                sum += Node.GetAverageStrategy()[i];
                if (rand < sum)
                    return i;
            }

            return -1;
        }

        public double main()
        {
            int hand_num = 0;
            double result = 0.0;
            string Deck = Draw.GenerateDeck();


            while (true)
            {
                hand_num += 1;
                Console.WriteLine("Hand number " + hand_num.ToString());
                Deck = Draw.ShuffleDeck(Deck);
                result += CFR(Deck, "", Deck.Substring(0, Draw.HAND_CARDS), Deck.Substring(Draw.HAND_CARDS, Draw.HAND_CARDS), 0.5, 1, hand_num % 2);
                Console.WriteLine("Total result: " + result.ToString() + "\n");
            }
        }
    }
}