using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace lowbotCFR
{
    internal class BucketHands
    {
        private readonly Dictionary<int, char> ValuesToSigns = new Dictionary<int, char>
            {
                {1, '2' },
                {2, '3' },
                {3, '4' },
                {4, '5' },
                {5, '6' },
                {6, '7' },
                {7, '8' },
                {8, '9' },
                {9, 'T' },
                {10, 'J' },
                {11, 'Q' },
                {12, 'K' },
                {13, 'A' }
            };

        private readonly Dictionary<char, int> SignsToValues = new Dictionary<char, int>
            {
                {'2', 1 },
                {'3', 2 },
                {'4', 3 },
                {'5', 4 },
                {'6', 5 },
                {'7', 6 },
                {'8', 7 },
                {'9', 8 },
                {'T', 9 },
                {'J', 10 },
                {'Q', 11 },
                {'K', 12 },
                {'A', 13 }
            };

        private readonly Dictionary<int, char> SuitsToSigns = new Dictionary<int, char>
            {
                {1, 'c' },
                {2, 'd' },
                {3, 'h' },
                {4, 's' }
            };

        private readonly Dictionary<char, int> SignsToSuits = new Dictionary<char, int>
            {
                {'c', 1 },
                {'d', 2 },
                {'h', 3 },
                {'s', 4 }
            };

        public static List<string>[] Bucket(int NUM_BUCKETS, double lambda)
        {
            List<string>[] Buckets = new List<string>[NUM_BUCKETS];
            for (int i = 0; i < NUM_BUCKETS; i++)
                Buckets[i] = new List<string>();
            List<string> Hands = new List<string>();
            List<string>[] BucketsDistinct = new List<string>[NUM_BUCKETS];
            Draw Draw = new Draw(13, 4, 5, 0, 0, 0);
            int counter = 0;
            string Deck = Draw.GenerateDeck();
            for (int i = 0; i < Deck.Length - 4; i++)
            {
                for (int j = i + 1; j < Deck.Length - 3; j++)
                {
                    for (int k = j + 1; k < Deck.Length - 2; k++)
                    {
                        for (int l = k + 1; l < Deck.Length - 1; l++)
                        {
                            for (int m = l + 1; m < Deck.Length; m++)
                            {
                                string Hand = Deck[i].ToString() + Deck[j] + Deck[k] + Deck[l] + Deck[m];
                                Hand = Draw.SortHand(Hand);
                                Hands.Add(Hand);
                                counter++;
                                Console.Write("\rAdded hand number {0}", counter);
                            }
                        }
                    }
                }
            }
            Hands = Hands
                .OrderBy(i => Draw.GetHandValue(i)[0])
                .ThenBy(i => Draw.GetHandValue(i)[1])
                .ThenBy(i => Draw.GetHandValue(i)[2])
                .ThenBy(i => Draw.GetHandValue(i)[3])
                .ThenBy(i => Draw.GetHandValue(i)[4])
                .ThenBy(i => Draw.GetHandValue(i)[5])
                .ToList();
            Console.WriteLine("\nSorted {0} hands", Hands.Count);
            //int BucketSize = (int)Math.Ceiling((double)Hands.Count / NUM_BUCKETS);
            double sum = 0.0;
            for (int i = 0; i < NUM_BUCKETS; i++)
            {
                sum += Math.Exp(-lambda * i / NUM_BUCKETS);
            }
            double scale = Hands.Count / sum;
            int p = 0;

            for (int i = 0; i < NUM_BUCKETS; i++)
            {
                Console.Write("\rFilling Bucket {0}", i);
                int BucketSize = (int)Math.Ceiling(Math.Exp(-lambda * i / NUM_BUCKETS) * scale);
                BucketsDistinct[i] = Hands.GetRange(p, (i < NUM_BUCKETS - 1) ? BucketSize : Hands.Count - p).Distinct().ToList();
                Directory.CreateDirectory(@"E:\Lowbot\Buckets" + NUM_BUCKETS.ToString());
                TextWriter tw = new StreamWriter(@"E:\Lowbot\Buckets" + NUM_BUCKETS.ToString() + @"\Bucket" + i.ToString() + ".txt");
                //foreach (String s in Hands.GetRange(i*BucketSize, (i < NUM_BUCKETS - 1) ? BucketSize : Hands.Count - i*BucketSize))
                foreach (String s in BucketsDistinct[i])
                    tw.WriteLine(s);
                tw.Close();
                p += BucketSize;
            }
            Console.WriteLine("\nFinished");

            return BucketsDistinct;
            //for (int i = 0; i < 1000000; i++)
            //{
            //    Console.WriteLine(i);
            //    string Cards = Draw.GenerateDeck();
            //    string Hand = Cards.Substring(0, 5);
            //    Hand = Draw.SortHand(Hand);
            //    if (!Hands.Contains(Hand))
            //    {
            //        string Deck = Cards.Substring(5);
            //        int counter = 0;
            //        for (int j = 0; j < 10000; j++)
            //        {
            //            string Villain = Draw.ShuffleDeck(Deck).Substring(0, 5);
            //            if (Draw.CompareHands(Hand, Villain) > -1)
            //                counter++;
            //        }
            //        int index = counter * NUM_BUCKETS / 10000;
            //        if (index == NUM_BUCKETS)
            //            index--;
            //        Buckets[index].Add(Hand);
            //        Hands.Add(Hand);

            //    }
            //}

            //for (int i = 0; i < NUM_BUCKETS; i++)
            //{
            //    TextWriter tw = new StreamWriter(@"E:\Lowbot\Buckets\Bucket" + i.ToString() + ".txt");
            //    foreach (String s in Buckets[i])
            //        tw.WriteLine(s);
            //    tw.Close();
            //}
        }

        public static List<string>[] GetBuckets(int NUM_BUCKETS)
        {
            List<string>[] Buckets = new List<string>[NUM_BUCKETS];
            for (int i = 0; i < NUM_BUCKETS; i++)
                Buckets[i] = new List<string>();
            for (int i = 0; i < NUM_BUCKETS; i++)
            {
                Console.Write("\rGetting Bucket {0}", i);
                TextReader tw = new StreamReader(@"E:\Lowbot\Buckets" + NUM_BUCKETS.ToString() + @"\Bucket" + i.ToString() + ".txt");
                //foreach (String s in Hands.GetRange(i*BucketSize, (i < NUM_BUCKETS - 1) ? BucketSize : Hands.Count - i*BucketSize))

                string line;
                while ((line = tw.ReadLine()) != null)
                {
                    Buckets[i].Add(line);
                }
                tw.Close();
            }
            Console.WriteLine("\nFinished");
            return Buckets;
        }
    }
}