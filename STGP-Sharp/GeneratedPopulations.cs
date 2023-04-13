#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Linq;
using STGP_Sharp.Fitness_and_Gp_Results.Gp_Results_Stats;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;

#endregion

namespace STGP_Sharp
{
    public class Generation
    {
        public readonly string generationNumberString;

        public readonly List<Individual> population;

        public Generation(List<Individual> sortedPopulation, int generationNumber)
        {
            this.population = sortedPopulation;
            this.generationNumberString = $"Generation {generationNumber}";
        }

        public Individual? Best => this.population.Any() ? this.population[0] : null;
        public Individual? Worst => this.population.Any() ? this.population[this.population.Count - 1] : null;
        public Individual? Median => this.population.Any() ? this.population[this.population.Count / 2] : null;
    }

    public class GeneratedPopulations
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Individual? bestEver;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly DateTime endTime;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly List<Generation> generations;

        public readonly List<List<Individual>> generationsAsNestedList;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly float secondsElapsed;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly DateTime startTime;

        public readonly VerboseInfo verboseInfo;

        // ReSharper disable once MemberCanBePrivate.Global
        public GpResultsStatsBase.IDetailedSummary fitnessSummary;

        public GeneratedPopulations(
            List<List<Individual>> populations,
            GpResultsStatsBase.IDetailedSummary fitnessSummary,
            DateTime startTime,
            DateTime endTime,
            Individual? bestEver,
            VerboseInfo verboseInfo)
        {
            this.generationsAsNestedList = populations;
            this.generations = new List<Generation>();
            List<List<Individual>> populationsAsList = this.generationsAsNestedList.ToList();
            for (var i = 0; i < populationsAsList.Count; i++)
            {
                this.generations.Add(new Generation(populationsAsList[i].SortedByFitness(), i));
            }

            this.fitnessSummary = fitnessSummary;
            this.startTime = startTime;
            this.endTime = endTime;
            this.secondsElapsed = GeneralCSharpUtilities.SecondsElapsed(startTime, endTime);
            this.bestEver = bestEver;
            this.verboseInfo = verboseInfo;
        }

        public Individual? GetBestEver()
        {
            return GetBestEver(this.generationsAsNestedList);
        }

        private static Individual? GetBestEver(IEnumerable<IEnumerable<Individual>> populations)
        {
            return populations.Last().SortedByFitness().FirstOrDefault();
        }

        public GeneratedPopulations DeepCopy()
        {
            List<List<Individual>> newPopulations = this.generations.Select(population =>
                    population.population.Select(individual =>
                        individual.DeepCopy()
                    ).ToList())
                .ToList();

            return new GeneratedPopulations(newPopulations, this.fitnessSummary, this.startTime, this.endTime,
                this.bestEver, this.verboseInfo);
        }
    }
}