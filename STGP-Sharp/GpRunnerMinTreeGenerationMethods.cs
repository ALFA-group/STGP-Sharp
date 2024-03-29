﻿#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace STGP_Sharp
{
    public readonly struct MinTreeData
    {
        public readonly HashSet<Type> permissibleNodeTypes;
        public readonly int heightOfMinTree;

        public MinTreeData(int heightOfMinTree, HashSet<Type> permissibleNodeTypes)
        {
            this.heightOfMinTree = heightOfMinTree;
            this.permissibleNodeTypes = permissibleNodeTypes;
        }

        public MinTreeData(int heightOfMinTree) :
            this(heightOfMinTree, new HashSet<Type>())
        {
        }

        public MinTreeData(int heightOfMinTree, Type type) :
            this(heightOfMinTree, new HashSet<Type> { type })
        {
        }
    }

    public partial class GpRunner
    {
        public Dictionary<ReturnTypeSpecification, MinTreeData> nodeReturnTypeToMinTreeDictionary =
            new Dictionary<ReturnTypeSpecification, MinTreeData>();

        public Dictionary<Type, MinTreeData> nodeTypeToMinTreeDictionary = new Dictionary<Type, MinTreeData>();

        partial void SetMinTreeDictionariesForSatisfiableNodeTypes()
        {
            this.nodeReturnTypeToMinTreeDictionary =
                new Dictionary<ReturnTypeSpecification,
                    MinTreeData>();
            this.nodeTypeToMinTreeDictionary = new Dictionary<Type, MinTreeData>();
            List<Type> nodeTypesWithProbabilityGreaterThanZero = GetSubclassesOfGpBuildingBlock()
                .Where(t => this.populationParameters.probabilityDistribution.GetProbabilityOfType(t) > 0).ToList();
            HashSet<Type> returnTypesWithProbabilityGreaterThanZero =
                nodeTypesWithProbabilityGreaterThanZero
                    .Select(t => GetReturnTypeSpecification(t).returnType).ToHashSet();


            if (!nodeTypesWithProbabilityGreaterThanZero.Contains(this.solutionReturnType) &&
                !returnTypesWithProbabilityGreaterThanZero.Contains(this.solutionReturnType))
            {
                throw new Exception($"Probability ZERO Return type! Looking for {this.solutionReturnType}");
            }

            List<Type> terminals = GetTerminals().Where(t =>
                nodeTypesWithProbabilityGreaterThanZero.Contains(t)).ToList();

            var satisfiableNodeTypes = new HashSet<Type>();

            foreach (Type t in terminals)
            {
                Node tree = this.GenerateRandomTreeOfType(t, 0, false);
                ReturnTypeSpecification returnTypeSpec = GetReturnTypeSpecification(t);
                this.nodeTypeToMinTreeDictionary[t] = new MinTreeData(tree.GetHeight(), t);
                this.nodeReturnTypeToMinTreeDictionary[returnTypeSpec] = this.nodeTypeToMinTreeDictionary[t];
                nodeTypesWithProbabilityGreaterThanZero.Remove(t);
                satisfiableNodeTypes.Add(t);
            }

            var targetMinTreeHeight = 1; // Not zero because we already found terminals

            while (nodeTypesWithProbabilityGreaterThanZero.Count > 0)
            {
                // This loop is basically saying "Find me all min trees of targetMinTreeHeight".
                foreach (Type t in nodeTypesWithProbabilityGreaterThanZero)
                {
                    ReturnTypeSpecification tSpec = GetReturnTypeSpecification(t);
                    Node minTree;
                    try
                    {
                        minTree = this.GenerateRandomTreeOfType(t, targetMinTreeHeight, false);
                    }
                    catch (MinTreeNotSatisfiable)
                    {
                        continue;
                    }

                    if (!this.nodeReturnTypeToMinTreeDictionary.TryGetValue(tSpec, out MinTreeData oldMinTree) ||
                        minTree.GetHeight() < oldMinTree.heightOfMinTree)
                    {
                        this.nodeReturnTypeToMinTreeDictionary[tSpec] = new MinTreeData(minTree.GetHeight());
                        this.nodeReturnTypeToMinTreeDictionary[tSpec].permissibleNodeTypes.Add(t);
                    }
                    else if (minTree.GetHeight() == this.nodeReturnTypeToMinTreeDictionary[tSpec].heightOfMinTree)
                    {
                        this.nodeReturnTypeToMinTreeDictionary[tSpec].permissibleNodeTypes.Add(t);
                    }

                    this.nodeTypeToMinTreeDictionary[t] = new MinTreeData(minTree.GetHeight(), t);
                    satisfiableNodeTypes.Add(t);
                }

                var found = false;
                foreach (Type key in satisfiableNodeTypes)
                {
                    found = nodeTypesWithProbabilityGreaterThanZero.Remove(key) || found;
                }

                if (!found)
                {
                    throw new MinTreesNotSatisfiable(nodeTypesWithProbabilityGreaterThanZero);
                }

                targetMinTreeHeight++;
            }
        }


        public class MinTreeNotSatisfiable : MinTreesNotSatisfiable
        {
            public MinTreeNotSatisfiable(Type returnType) : base(new[] { returnType })
            {
            }
        }


        public class MinTreesNotSatisfiable : Exception
        {
            public MinTreesNotSatisfiable(IEnumerable<Type> returnTypes) :
                base(GetMessage(returnTypes))
            {
            }

            private static string GetMessage(IEnumerable<Type> returnTypes)
            {
                List<Type> returnTypesList = returnTypes.ToList();
                string pluralityChar = returnTypesList.Count > 1 ? "s" : "";
                string types = returnTypesList.Count > 1
                    ? string.Join(", ", returnTypesList)
                    : returnTypesList.First().Name;
                string isOrAre = returnTypesList.Count > 1 ? "are" : "is";

                // The lack of spaces around certain variables is intentional as some of them include spaces
                return
                    $"The requested tree{pluralityChar} " +
                    $"with return type{pluralityChar} {types} " +
                    $"{isOrAre} not satisfiable";
            }
        }
    }
}