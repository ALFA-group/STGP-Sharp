using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.Utilities.GP;

namespace STGP_Sharp.Tests.Genetic_Operators
{
    public class TestSortedByFitness
    {
        private readonly List<Individual> _inputPopulation = new List<Individual>
        {
            new Individual(new BooleanConstant(true),
                new FitnessStandard(0.0)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.1)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.8)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.3)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.5)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.4)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.9)),
            new Individual(new BooleanConstant(false),
                new FitnessStandard(-0.2)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.6)),
            new Individual(new BooleanConstant(true),
                new FitnessStandard(-0.7))
        };

        [Test]
        public void HardCodedInputPopulation()
        {
            var results = this._inputPopulation.SortedByFitness();
            Assert.That(results.Select(i => i.fitness!), Is.Ordered.Descending);
            // Whether they contain the same elements, ignoring order
            Assert.That(results, Is.EquivalentTo(this._inputPopulation));
        }
    }
}