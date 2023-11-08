using System;
using System.Collections.Generic;
using NUnit.Framework;
using STGP_Sharp.STGP_Sharp.Tests;

namespace STGP_Sharp.Tests.Genetic_Operators
{
    public class TestMutate : GpRunnerUnitTest
    {
        protected static readonly ProbabilityDistribution TypesForThisUnitTest = new ProbabilityDistribution(
            new List<Type>()
            {
                typeof(Vector2Constant),
                typeof(TwoVectorsOneFloat),
                typeof(FloatConstant)
            }
        );
        
        private const int RandomSeed = 0;
        private const int MaxDepth = 10;


        private readonly Node _inputNode = new TwoVectorsOneFloat(
            new Vector2Constant(7, 7),
            new Vector2Constant(2, 2),
            new FloatConstant(1)
        );
        
        
        private readonly Node _expectedResult = new TwoVectorsOneFloat(
            new Vector2Constant(7, 7),
            new Vector2Constant(2, 2),
            new FloatConstant( 0.6771811f)
        );

        public TestMutate() : base(
            new GpPopulationParameters(
                probabilityDistribution: TypesForThisUnitTest,
                maxDepth: MaxDepth
            ),
            solutionReturnType: typeof(TwoVectorsOneFloat),
            randomSeed: RandomSeed
        )
        {
        }

        [Test]
        public void HardCodedInputNode()
        {
            var runner = this.GetGpRunner();
            runner.Mutate(this._inputNode);
            Assert.That(this._inputNode, Is.EqualTo(this._expectedResult).Using(new NodeComparer()));
        }
    }
}