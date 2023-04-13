#nullable enable

#region

using System;
using STGP_Sharp.Fitness_and_Gp_Results.Gp_Results_Stats;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp.Fitness_and_Gp_Results
{
    [Serializable]
    public class FitnessStandard : FitnessBase
    {
        public readonly double fitnessScore;

        public FitnessStandard(double fitnessScore)
        {
            this.fitnessScore = fitnessScore;
        }

        // ReSharper disable once UnusedMember.Global
        public new static Type GpResultsStatsType { get; } = typeof(GpResultsStatsStandard);

        public static void ThrowExceptionIfInvalidFitness(FitnessBase? f, out FitnessStandard fs)
        {
            ThrowExceptionIfInvalidFitnessForComparison(f, out fs);
        }

        public override int CompareTo(FitnessBase? other)
        {
            if (null == other)
            {
                return this.fitnessScore.CompareTo(null);
            }

            ThrowExceptionIfInvalidFitness(other, out FitnessStandard otherStandard);
            return this.fitnessScore.CompareTo(otherStandard.fitnessScore);
        }


        public override int GetHashCode()
        {
            return GeneralCSharpUtilities.CombineHashCodes(new[]
                { this.fitnessScore.GetHashCode() });
        }

        public override FitnessBase DeepCopy()
        {
            return new FitnessStandard(this.fitnessScore);
        }

        public FitnessBase Add(FitnessBase f)
        {
            ThrowExceptionIfInvalidFitness(f, out FitnessStandard fitnessStandard);
            return new FitnessStandard(this.fitnessScore + fitnessStandard.fitnessScore);
        }

        public FitnessBase Divide(int divisor)
        {
            return divisor == 0
                ? this.DeepCopy()
                : new FitnessStandard(this.fitnessScore / divisor);
        }

        public override bool LessThan(FitnessBase f)
        {
            ThrowExceptionIfInvalidFitness(f, out FitnessStandard fitnessStandard);
            return this.fitnessScore < fitnessStandard.fitnessScore;
        }

        public override bool GreaterThan(FitnessBase f)
        {
            ThrowExceptionIfInvalidFitness(f, out FitnessStandard fitnessStandard);
            return this.fitnessScore > fitnessStandard.fitnessScore;
        }

        public override bool Equals(FitnessBase? otherFitness)
        {
            if (null == otherFitness)
            {
                return false;
            }

            ThrowExceptionIfInvalidFitness(otherFitness, out FitnessStandard fitnessStandard);
            return Math.Abs(this.fitnessScore - fitnessStandard.fitnessScore) < 0.00001;
        }

        public override string ToString()
        {
            return this.fitnessScore.ToString();
        }
    }
}