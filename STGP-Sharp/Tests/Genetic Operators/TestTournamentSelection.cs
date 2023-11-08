#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.STGP_Sharp.Tests;

namespace STGP_Sharp.Tests.Genetic_Operators
{
    public class TestTournamentSelection : GpRunnerUnitTest
    {
        protected static readonly ProbabilityDistribution TypesForThisUnitTest = new ProbabilityDistribution(
            new List<Type>()
            {
                typeof(BooleanConstant)
            }
        );

        private const int POPULATION_SIZE = 10;
        private const int TOURNAMENT_SIZE = 2;

        private readonly List<double> _expectedFitnessResults = new List<double>
        {
            -0.6, -0.1, -0.5, -0.4, -0.2, -0.4, -0.4, 0, -0.6, -0.3
        };

        private readonly List<Individual> _inputPopulation = new List<Individual>
        {
            new Individual(new BooleanConstant(true),
                new FitnessStandard(0.0)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.1)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.2)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.3)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.4)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.5)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.6)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.7)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.8)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.9))
        };

        public TestTournamentSelection() : base(
            new GpPopulationParameters(
                probabilityDistribution: TypesForThisUnitTest,
                populationSize: POPULATION_SIZE,
                tournamentSize: TOURNAMENT_SIZE
            ),
            solutionReturnType: typeof(BooleanConstant))
        {
        }

        [Test]
        public void HardCodedInputPopulation()
        {
            var results = this.GetGpRunner().TournamentSelection(this._inputPopulation)
                .Select(i => ((FitnessStandard?)i.fitness)?.fitnessScore ?? 
                             throw new Exception("Fitness for an individual is null")).ToList();
            Assert.That(results, Is.EqualTo(this._expectedFitnessResults));
        }
    }
}