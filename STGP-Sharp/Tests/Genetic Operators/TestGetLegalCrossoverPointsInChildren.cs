using System.Collections.Generic;
using NUnit.Framework;
using STGP_Sharp.STGP_Sharp.Tests;

namespace STGP_Sharp.Tests.Genetic_Operators
{
    public class TestGetLegalCrossoverPointsInChildren
    {
        private readonly Dictionary<int, List<int>> _expectedResults = new Dictionary<int, List<int>>
        {
            { 1, new List<int> { 1, 2} },
            { 2, new List<int> { 1, 2 } },
            { 3, new List<int> { 3 } }
        };

        private readonly Node _testNodeA = new TwoVectorsOneFloat(
            new Vector2Constant(0, 0),
            new Vector2Constant(1, 1),
            new DiscreteFloatConstant(0)
        );

        private readonly Node _testNodeB = new TwoVectorsOneFloat(
            new Vector2Constant(4, 4),
            new Vector2Constant(2, 2),
            new FloatConstant(0));

        [Test]
        public void TestHardCodedNodeInputs()
        {
            var results = GpRunner.GetLegalCrossoverPointsInChildren(this._testNodeA, this._testNodeB);
            Assert.That(results, Is.EquivalentTo(this._expectedResults));
        }
    }
}