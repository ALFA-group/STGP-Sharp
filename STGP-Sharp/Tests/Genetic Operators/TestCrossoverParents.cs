using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STGP_Sharp.STGP_Sharp.Tests;
using Assert = NUnit.Framework.Assert;


namespace STGP_Sharp.Tests.Genetic_Operators
{
    public class TestCrossoverParents : GpRunnerUnitTest
    {
        protected static readonly ProbabilityDistribution TypesForThisUnitTest = new ProbabilityDistribution(
            new List<Type>()
            {
                typeof(Vector2Constant),
                typeof(TwoVectorsOneFloat),
                typeof(FloatConstant)
            }
        );
        
        private const int PopulationSize = 3;
        private const int MaxDepth = 10;
        private const double CrossoverProbability = 1.0;
        private const int RandomSeed = 7;

        private readonly List<Node> _expectedResults = new List<Node>
        {
            new TwoVectorsOneFloat(
                new Vector2Constant(4, 4),
                new Vector2Constant(50, 50),
                new FloatConstant(4)),

            new TwoVectorsOneFloat(
                new Vector2Constant(3, 3),
                new Vector2Constant(100, 100),
                new DiscreteFloatConstant(3)),

            new TwoVectorsOneFloat(
                new Vector2Constant(4, 4),
                new Vector2Constant(150, 150),
                new FloatConstant(2))
        };

        private readonly List<Individual> _inputPopulation = new List<Individual>
        {
            new Individual(
                new TwoVectorsOneFloat(
                    new Vector2Constant(2, 2),
                    new Vector2Constant(150, 150),
                    new FloatConstant(2)
                )
            ),
            new Individual(
                new TwoVectorsOneFloat(
                    new Vector2Constant(3, 3),
                    new Vector2Constant(50, 50),
                    new DiscreteFloatConstant(3)
                )
            ),
            new Individual(
                new TwoVectorsOneFloat(
                    new Vector2Constant(4, 4),
                    new Vector2Constant(100, 100),
                    new FloatConstant(4)
                )
            )
        };

        public TestCrossoverParents() : base(
            new GpPopulationParameters(
                probabilityDistribution: TypesForThisUnitTest,
                populationSize: PopulationSize,
                maxDepth: MaxDepth,
                crossoverProbability: CrossoverProbability
            ),
            randomSeed: RandomSeed,
            solutionReturnType: typeof(TwoVectorsOneFloat))
        {
        }

        [Test]
        public void HardCodedInputPopulations()
        {
            var results = this.GetGpRunner(true)
                .CrossoverListOfParents(this._inputPopulation)
                .Select(i => i.genome);
            Assert.That(results, Is.EquivalentTo(this._expectedResults).Using(new NodeComparer()));
        }
    }
}