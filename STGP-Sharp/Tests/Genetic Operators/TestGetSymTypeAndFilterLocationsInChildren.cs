using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using STGP_Sharp.STGP_Sharp.Tests;

namespace STGP_Sharp.Tests.Genetic_Operators
{
    public class TestGetSymTypeAndFilterLocationsInChildren
    {
        private readonly List<int> _expectedResults = new List<int> { 1, 2 };
        private readonly List<FilterAttribute> _filtersOfReturnTypeToSearchFor = new List<FilterAttribute>();

        private readonly Node _inputNode =
            new TwoVectorsOneFloat(
                new Vector2Constant(0, 0),
                new Vector2Constant(1, 1),
                new FloatConstant(0));

        private readonly Type _returnTypeToSearchFor = typeof(Vector2);

        [Test]
        public void TestHardCodedInputNode()
        {
            var results = this._inputNode.GetSymTypeAndFilterLocationsInDescendants(
                this._returnTypeToSearchFor,
                this._filtersOfReturnTypeToSearchFor).ToList();

            Assert.That(results, Is.EqualTo(this._expectedResults));
        }
    }
}