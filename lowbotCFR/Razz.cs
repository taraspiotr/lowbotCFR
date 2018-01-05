using System;
using System.Collections.Generic;
using System.Linq;

namespace lowbotCFR
{
    internal class Razz
    {
        public readonly int NUM_CARDS;
        public readonly int NUM_SUITS;
        public readonly int NUM_STREETS;
        public readonly int SB_ROUNDS;
        public readonly int CAP;
        public const double ANTE = 0.15;
        public const double BRING_IN = 0.45;
        public const double SMALL_BET = 1.0;
        public const double BIG_BET = 2.0;
        public const string DRAW = "DRAW";
        public const string LAST_DRAW = "LAST_DRAW";
        public const string TERMINAL_FOLD = "TERMINAL_FOLD";
        public const string TERMINAL_CALL = "TERMINAL_CALL";

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
                {0, 'A' }
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
                {'A', 0 }
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

        public Razz(int num_cards, int num_suits, int num_streets, int sb_rounds, int cap)
        {
            NUM_CARDS = num_cards;
            NUM_SUITS = num_suits;
            NUM_STREETS = num_streets;
            SB_ROUNDS = sb_rounds;
            CAP = cap;
        }

        public string GetLegalActions(string History)
        {

            string[] Split = History.Split('(', ')').Where(e => e != "").ToArray();

            string LastRound = Split[Split.Length - 1];


            if (Split.Length == 1)
                return "fr";

            if (Split.Length % 2 == 1)
            {
                return "cr";
            }

            if (LastRound[LastRound.Length - 1] == 'f')
                return TERMINAL_FOLD;

            if (LastRound[LastRound.Length - 1] == 'c')
            {
                if (LastRound.Length == 1)
                    return "cr";
                if (Split.Length / 2 < NUM_STREETS)
                {
                    if (Split.Length / 2 == NUM_STREETS - 1)
                        return LAST_DRAW;
                    else
                        return DRAW;
                }
                else
                    return TERMINAL_CALL;
            }

            if (LastRound[LastRound.Length - 1] == 'r')
            {
                if (LastRound.Count(e => e == 'r') < CAP)
                    return "fcr";
                else
                    return "fc";
            }

            return "fcr";
        }

        public int GetCurrentPlayer(string History)
        {
            string[] Split = History.Split('(', ')').Where(e => e != "").ToArray();
            string LastRound = Split[Split.Length - 1];

            if (Split.Length % 2 == 1)
            {
                string hand1 = "";
                string hand2 = "";
                for (int i = 0; i < Split.Length && i < (NUM_STREETS-1)*2 ; i += 2)
                {
                    hand1 += Split[i][0];
                    hand2 += Split[i][1];
                }
                int player = (CompareHands(hand2, hand1) + 1) / 2;
                if (Split.Length == 1)
                    return 1 - player;
                return player;
            }

            return 1 - LastRound.Length % 2;
        }

        public double GetPotContribution(string History, int Player)
        {
            string[] Rounds = History.Split('(', ')').Where(e => e != "").Where((e, i) => i % 2 == 0).ToArray();

            if (Rounds.Length == 1 && Rounds.First() == "rf" && Player == 0)
                return SMALL_BET / 2;
            if (Rounds.Length == 1 && Rounds.First() == "rcf" && Player == 1)
                return SMALL_BET;

            double Pot = 0;

            for (int i = 0; i < Rounds.Length; i++)
            {
                double Bet = (i < SB_ROUNDS) ? SMALL_BET : BIG_BET;
                double RoundPot = Rounds[i].Count(e => e == 'r') * Bet;
                if (i == Rounds.Length - 1 && Rounds.Last().Last() == 'f' && Player == Rounds.Last().Length % 2 && RoundPot > 0)
                    RoundPot -= Bet;
                Pot += RoundPot;
            }

            return Pot;
        }

        public string GenerateDeck(int seed = -1)
        {
            Random rng;
            if (seed == -1)
                rng = new Random();
            else
                rng = new Random(seed);

            char[] Deck = new char[NUM_CARDS * NUM_SUITS];
            for (int i = 0; i < NUM_CARDS; i++)
                for (int j = 0; j < NUM_SUITS; j++)
                    Deck[j * NUM_CARDS + i] = ValuesToSigns[i + 1];

            for (int i = Deck.Length - 1; i > 0; i--)
            {
                int SwapIndex = rng.Next(i + 1);
                char tmp = Deck[i];
                Deck[i] = Deck[SwapIndex];
                Deck[SwapIndex] = tmp;
            }

            return new string(Deck);
        }

        public string SortHand(string HandS)
        {
            char[] Hand = HandS.ToCharArray();
            int[] HandValues = new int[Hand.Length];
            for (int i = 0; i < Hand.Length; i++)
                HandValues[i] = SignsToValues[Hand[i]];
            HandValues = HandValues.OrderByDescending(e => e).ToArray();

            for (int i = 0; i < Hand.Length; i++)
                Hand[i] = ValuesToSigns[HandValues[i]];

            return new string(Hand);
        }

        public int CompareHands(string Hand1, string Hand2)
        {
            int[] Value1 = GetHandValue(Hand1);
            int[] Value2 = GetHandValue(Hand2);

            for (int i = 0; i < Value1.Length; i++)
            {
                if (Value1[i] < Value2[i])
                    return 1;
                else if (Value1[i] > Value2[i])
                    return -1;
            }

            return 0;
        }

        public string CreateInfoSet(string History, string Hand)
        {
            string InfoSet = "";
            //TODO

            return InfoSet;
        }

        private int[] GetHandValue(string Hand)
        {
            Hand = SortHand(Hand);
            int[] HandValues = new int[Hand.Length];
            for (int i = 0; i < Hand.Length; i++)
                HandValues[i] = SignsToValues[Hand[i]];

            var Counts = HandValues
                .GroupBy(e => e)
                .OrderByDescending(e => e.Count())
                .ThenByDescending(e => e.Key)
                .ToList();

            switch (Hand.Length)
            {
                case 7:
                    string tempHand = "";
                    int max_count = 1;
                    while (tempHand.Length < 5)
                    {
                        foreach (var c in Counts)
                        {
                            if (c.Count() == max_count)
                                tempHand += c.Key;
                            else if (c.Count() == 0)
                            {
                                max_count++;
                                break;
                            }
                        }
                    }
                    return GetHandValue(tempHand.Substring(0, 5));

                case 1:
                    return new int[] { Counts[0].Key };

                case 2:
                    if (Counts[0].Count() == 2) // Pair
                        return new int[] { 1, Counts[0].Key, 0 };
                    return new int[] { 0, Counts[0].Key, Counts[1].Key };

                case 3:
                    if (Counts[0].Count() == 3) // Three of a kind
                        return new int[] { 8, Counts[0].Key, 0, 0 };
                    if (Counts[0].Count() == 2) // Pair
                        return new int[] { 6, Counts[0].Key, Counts[1].Key, 0 };

                    return new int[] { 5, Counts[0].Key, Counts[1].Key, Counts[2].Key };

                case 4:
                    if (Counts[0].Count() == 4) // Four of a kind
                        return new int[] { 10, Counts[0].Key, 0, 0, 0 };
                    if (Counts[0].Count() == 3) // Three of a kind
                        return new int[] { 8, Counts[0].Key, Counts[1].Key, 0, 0 };
                    if (Counts[0].Count() == 2 && Counts[1].Count() == 2) // Two pairs
                        return new int[] { 7, Counts[0].Key, Counts[1].Key, 0, 0 };
                    if (Counts[0].Count() == 2) // Pair
                        return new int[] { 6, Counts[0].Key, Counts[1].Key, Counts[2].Key, 0 };

                    return new int[] { 5, Counts[0].Key, Counts[1].Key, Counts[2].Key, Counts[3].Key };

                case 5:
                    if (Counts[0].Count() == 4) // Four of a kind
                        return new int[] { 10, Counts[0].Key, 0, 0, 0, 0 };
                    if (Counts[0].Count() == 3 && Counts[1].Count() == 2) // Full house
                        return new int[] { 9, Counts[0].Key, Counts[1].Key, 0, 0, 0 };
                    if (Counts[0].Count() == 3) // Three of a kind
                        return new int[] { 8, Counts[0].Key, Counts[1].Key, Counts[2].Key, 0, 0 };
                    if (Counts[0].Count() == 2 && Counts[1].Count() == 2) // Two pairs
                        return new int[] { 7, Counts[0].Key, Counts[1].Key, Counts[2].Key, 0, 0 };
                    if (Counts[0].Count() == 2) // Pair
                        return new int[] { 6, Counts[0].Key, Counts[1].Key, Counts[2].Key, Counts[3].Key, 0 };

                    return new int[] { 5, Counts[0].Key, Counts[1].Key, Counts[2].Key, Counts[3].Key, Counts[4].Key };

                default:
                    return new int[] { 0 };
            }
        }
    }
}