using System;

namespace lowbotCFR
{
    public class Node
    {
        public int NumActions;
        public string Actions;
        public string InfoSet;
        public double[] RegretSum;
        public double[] Strategy;
        public double[] StrategySum;
        public double Realization;
        public int Count;

        public void Init(int num_actions, string ac, string info)
        {
            Count = 1;
            Realization = 0.0;
            NumActions = num_actions;
            Actions = ac;
            InfoSet = info;
            RegretSum = new double[NumActions];
            Strategy = new double[NumActions];
            StrategySum = new double[NumActions];
        }

        public double[] GetStrategy(double RealizationWeight)
        {
            double NormalizingSum = 0.0;

            for (int i = 0; i < NumActions; i++)
            {
                Strategy[i] = (RegretSum[i] > 0) ? RegretSum[i] : 0;
                NormalizingSum += Strategy[i];
            }

            for (int i = 0; i < NumActions; i++)
            {
                if (NormalizingSum > 0)
                    Strategy[i] /= NormalizingSum;
                else
                    Strategy[i] = 1.0 / NumActions;

                StrategySum[i] += RealizationWeight * Strategy[i];
            }

            return Strategy;
        }

        public double[] GetAverageStrategy()
        {
            double[] AvgStrategy = new double[NumActions];
            double NormalizingSum = 0.0;

            for (int i = 0; i < NumActions; i++)
                NormalizingSum += StrategySum[i];

            for (int i = 0; i < NumActions; i++)
            {
                if (NormalizingSum > 0)
                    AvgStrategy[i] = StrategySum[i] / NormalizingSum;
                else
                    AvgStrategy[i] = 1.0 / NumActions;
            }

            return AvgStrategy;
        }

        public override string ToString()
        {
            return String.Format("{0};{1};{2}", InfoSet, Count, String.Join(";", GetAverageStrategy()));
        }
    }
}