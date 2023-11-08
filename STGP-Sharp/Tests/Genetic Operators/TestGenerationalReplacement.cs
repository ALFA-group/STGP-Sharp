using System;
using System.Collections.Generic;
using NUnit.Framework;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.STGP_Sharp.Tests;

namespace STGP_Sharp.Tests.Genetic_Operators
{
    public class TestGenerationalReplacement : GpRunnerUnitTest
    {
        protected static readonly ProbabilityDistribution TypesForThisUnitTest = new ProbabilityDistribution(
            new List<Type>()
            {
                typeof(BooleanConstant)
            }
        );
        
        private const int PopulationSize = 5;
        private const int EliteSize = 3;

        private readonly List<Individual> _expectedNewPopulation = new List<Individual>
        {
            new Individual(new BooleanConstant(false),
                new FitnessStandard(100)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(0)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-1)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-2)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-3))
        };

        private readonly List<Individual> _oldPopulation = new List<Individual>
        {
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-3)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-4)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-5)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-6)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(100))
        };

        private List<Individual> _newPopulation = new List<Individual>
        {
            new Individual(new BooleanConstant(true),
                new FitnessStandard(0)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-1)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-2)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-10)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-11))
        };

        public TestGenerationalReplacement() : base(new GpPopulationParameters(
            probabilityDistribution: TypesForThisUnitTest,
            populationSize: PopulationSize,
            eliteSize: EliteSize),
            solutionReturnType: typeof(bool))
        {
        }

        [Test]
        public void HardCodedInputPopulations()
        {
            this.GetGpRunner(true).GenerationalReplacement(ref this._newPopulation, this._oldPopulation);

            Assert.That(this._newPopulation,
                Is.EquivalentTo(this._expectedNewPopulation).Using(new IndividualComparer()));
        }
    }
}