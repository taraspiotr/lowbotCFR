using System;
using System.Collections.Generic;
using System.Linq;

namespace lowbotCFR
{
    internal class Draw
    {
        public const int NUM_CARDS = 5;
        public const int NUM_SUITS = 4;
        public const int HAND_CARDS = 2;
        public const string DRAW = "DRAW";
        public const string LAST_DRAW = "LAST_DRAW";
        public const string TERMINAL_FOLD = "TERMINAL_FOLD";
        public const string TERMINAL_CALL = "TERMINAL_CALL";
        public const int NUM_DRAWS = 1;
        public const int SB_ROUNDS = 2;
        public const int CAP = 2;
        public const double SMALL_BET = 1.0;
        public const double BIG_BET = 2.0;


        private static readonly Dictionary<int, char> ValuesToSigns = new Dictionary<int, char>
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

        private static readonly Dictionary<char, int> SignsToValues = new Dictionary<char, int>
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

        private static readonly Dictionary<int, char> SuitsToSigns = new Dictionary<int, char>
            {
                {1, 'c' },
                {2, 'd' },
                {3, 'h' },
                {4, 's' }
            };

        private static readonly Dictionary<char, int> SignsToSuits = new Dictionary<char, int>
            {
                {'c', 1 },
                {'d', 2 },
                {'h', 3 },
                {'s', 4 }
            };

        public static string GetLegalActions(string History)
        {
            string[] Split = History.Split('(', ')').Where(e => e != "").ToArray();

            string LastRound = Split.Last();

            if (Split.Length % 2 == 0)
            {
                if (LastRound.Length == 1)
                    return LAST_DRAW;
                else
                    return "fcr";
            }

            if (LastRound.Last() == 'f')
                return TERMINAL_FOLD;

            if (Split.Length == 1 && LastRound == "rc")
                return "fcr";

            if (LastRound.Length == 1)
                return "fcr";

            if (LastRound.Last() == 'c')
            {
                if (Split.Length / 2 < NUM_DRAWS)
                    return DRAW;
                else
                    return TERMINAL_CALL;
            }


            if (LastRound.Last() == 'r')
            {
                if (LastRound.Count(e => e == 'r') < CAP)
                    return "fcr";
                else
                    return "fc";
            }


            return "fcr";
        }

        public static int GetCurrentPlayer(string History)
        {
            string[] Split = History.Split('(', ')').Where(e => e != "").ToArray();
            string LastRound = Split.Last();
            string Actions = GetLegalActions(History);

            if (Actions == DRAW)
                return 1;
            if (Actions == LAST_DRAW)
                return 0;

            return 1 - LastRound.Length % 2;
        }

        public static double GetPotContribution(string History, int Player)
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

        public static string GenerateDeck(int seed = -1)
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

        public static string SortHand(string HandS)
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

        public static int CompareHands(string Hand1, string Hand2)
        {
            Hand1 = Hand1.Substring(Hand1.Length - HAND_CARDS, HAND_CARDS);
            Hand2 = Hand2.Substring(Hand2.Length - HAND_CARDS, HAND_CARDS);

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

        public static string CreateInfoSet(string History, string Hand)
        {
            string InfoSet = Hand.Substring(0, HAND_CARDS);
            int index = HAND_CARDS;
            int p = 0;

            while (p < History.Length)
            {
                InfoSet += History[p];
                if (History[p] == ')' && index < Hand.Length)
                {
                    InfoSet += Hand.Substring(index, HAND_CARDS);
                    index += HAND_CARDS;
                }
                p++;
            }

            return InfoSet;
        }

        public static int DrawCards(string History, string Cards, string Hand, ref string AfterHand, int ActionNumber)
        {
            string[] Draws = History.Split('(', ')').Where(e => e != "").Where((e, i) => i % 2 == 1).ToArray();
            int LastCard = 2 * HAND_CARDS;
            foreach (char c in String.Join("", Draws))
                LastCard += (int)Char.GetNumericValue(c);

            string s = Convert.ToString(ActionNumber, 2);
            string Action = new string('0', HAND_CARDS - s.Length) + s;

            string OldHand = Hand.Substring(Hand.Length - HAND_CARDS, HAND_CARDS);
            string NewHand = "";

            for (int i = 0; i < HAND_CARDS; i++)
            {
                if (Action[i] == '0')
                    NewHand += OldHand[i];
                else
                {
                    NewHand += Cards[LastCard];
                    LastCard++;
                }
            }

            AfterHand = Hand + Draw.SortHand(NewHand);
            return Action.Count(e => e == '1');
        }

        private static int[] GetHandValue(string Hand)
        {
            int[] HandValues = new int[Hand.Length];
            for (int i = 0; i < Hand.Length; i++)
                HandValues[i] = SignsToValues[Hand[i]];

            var Counts = HandValues
                .GroupBy(e => e)
                .OrderByDescending(e => e.Count())
                .ThenByDescending(e => e.Key)
                .ToList();

            //if (Counts[0].Count() == 4) // Four of a kind
            //    return new int[6] { 10, Counts[0].Key, 0, 0, 0, 0 };
            //if (Counts[0].Count() == 3 && Counts[1].Count() == 2) // Full house
            //    return new int[6] { 9, Counts[0].Key, Counts[1].Key, 0, 0, 0 };
            //if (Counts[0].Count() == 3) // Three of a kind
            //    return new int[6] { 9, Counts[0].Key, Counts[1].Key, Counts[2].Key, 0, 0 };
            //if (Counts[0].Count() == 2 && Counts[1].Count() == 2) // Two pairs
            //    return new int[6] { 8, Counts[0].Key, Counts[1].Key, Counts[2].Key, 0, 0 };
            //if (Counts[0].Count() == 2) // Pair
            //    return new int[6] { 7, Counts[0].Key, Counts[1].Key, Counts[2].Key, Counts[3].Key, 0 };

            //return new int[6] { 6, Counts[0].Key, Counts[1].Key, Counts[2].Key, Counts[3].Key, Counts[4].Key };

            if (Counts[0].Count() == 2) // Pair
                return new int[3] { 1, Counts[0].Key, 0 };
            else
                return new int[3] { 0, Counts[0].Key, Counts[1].Key };
        }
    }
}