#nullable enable

#region

using System;
using System.Collections.Generic;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp
{
    public class IndividualComparer : IEqualityComparer<Individual>
    {
        public bool Equals(Individual? i1, Individual? i2)
        {
            return i1?.Equals(i2) ?? null == i2;
        }

        public int GetHashCode(Individual? i)
        {
            if (null == i)
            {
                return 0;
            }

            return GeneralCSharpUtilities.CombineHashCodes(
                new[]
                {
                    new NodeComparer()
                        .GetHashCode(i
                            .genome), // this cannot be i.genome.GetHashCode() because that will not take into account whether node child order matters.
                    i.fitness?.GetHashCode() ?? 0
                }
            );
        }
    }

    public class Individual
    {
        public readonly Node genome;

        public FitnessBase? fitness;


        public Individual(Node? genome, FitnessBase? fitness = null)
        {
            this.genome = genome ?? throw new ArgumentNullException(nameof(genome));
            this.fitness = fitness;
        }

        public Individual DeepCopy()
        {
            return new Individual(this.genome.DeepCopy(), this.fitness?.DeepCopy());
        }

        public bool Equals(Individual? otherIndividual)
        {
            if (null == otherIndividual)
            {
                return false;
            }

            return this.genome.Equals(otherIndividual.genome) &&
                   (this.fitness?.Equals(otherIndividual.fitness) ?? null == otherIndividual.fitness);
        }
    }
}