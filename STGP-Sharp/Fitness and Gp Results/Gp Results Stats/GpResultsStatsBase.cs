using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable MemberCanBePrivate.Global

#nullable enable

namespace STGP_Sharp.Fitness_and_Gp_Results.Gp_Results_Stats
{
    public abstract class GpResultsStatsBase
    {
        public readonly IEnumerable<FitnessBase> fitnessValues;
        public readonly IEnumerable<Individual>? individuals;

        public GpResultsStatsBase(IEnumerable<FitnessBase> fitnessValues)
        {
            this.fitnessValues = fitnessValues;
        }

        // Give the user the option to work with other statistics relating to other aspects of individuals
        public GpResultsStatsBase(IEnumerable<Individual> individuals) :
            this(individuals.Select(i =>
                i.fitness ?? throw new NullReferenceException("Individual with null fitness found")))
        {
            this.individuals = individuals;
        }

        // ReSharper disable once UnusedMember.Global
        public abstract IDetailedSummary GetDetailedSummary();

        public interface IDetailedSummary
        {
            public string ToString();
        }
    }
}