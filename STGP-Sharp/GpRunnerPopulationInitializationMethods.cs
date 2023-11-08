#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp
{
    public abstract class PopulationInitializationMethod
    {
        /// <summary>
        /// The max depth of genomes. This is for internal purposes of
        /// verifying that an genomes which starts in the population is valid.
        /// </summary>
        private readonly int _maxDepth;
        /// <summary>
        /// The probability distribution for generating genomes. This is for internal purposes of
        /// verifying that a genome which starts in the population is valid.
        /// </summary>
        private readonly ProbabilityDistribution? _probabilityDistribution;
        
        protected PopulationInitializationMethod(GpPopulationParameters populationParameters) :
            this(populationParameters.probabilityDistribution, populationParameters.maxDepth)
        {
        }

        protected PopulationInitializationMethod(ProbabilityDistribution? probabilityDistribution, int maxDepth)
        {
            this._probabilityDistribution = probabilityDistribution;
            this._maxDepth = maxDepth;
        }

        public virtual Task<List<Individual>> GetPopulation<T>(GpRunner gp, TimeoutInfo timeoutInfo)
        {
            return Task.FromResult(new List<Individual>());
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public static List<Type>? GetPopulationInitializationMethodTypes()
        {
            List<Type> types = GpReflectionCache.GetAllSubTypes(typeof(PopulationInitializationMethod)).ToList();
            return !types.Any() ? null : types;
        }


        protected bool GenomeIsNullOrValid(Node? genome)
        {
            return genome == null || this.GenomeIsValid(genome);
        }


        protected bool GenomeIsValid(Node genome)
        {
            return this._probabilityDistribution != null &&
                   GpRunner.IsValidTree(genome, this._probabilityDistribution, this._maxDepth);
        }
    }

    public class RampedPopulationInitialization : PopulationInitializationMethod
    {
        public Node? genomeToStartInPopulation;

        public RampedPopulationInitialization(GpPopulationParameters populationParameters) : base(populationParameters)
        {
        }

        public RampedPopulationInitialization(ProbabilityDistribution probabilityDistribution, int maxDepth)
            : base(probabilityDistribution, maxDepth)
        {
        }


        public override async Task<List<Individual>>
            GetPopulation<T>(GpRunner gp, TimeoutInfo timeoutInfo) // Get around T by using MakeClosedGenericType
        {
            // Don't know if we should do this because can't always do that if building block min tree height is lower than the ramped depth

            var population = new List<Individual>();
            var uniqueNodes = new List<Node>();
            int n = gp.populationParameters.populationSize * 10;
            for (
                var individualNumber = 0;
                individualNumber < n && population.Count < gp.populationParameters.populationSize &&
                !timeoutInfo.ShouldTimeout;
                individualNumber++)
            {
                // Ramp the depth
                Type t = typeof(T);
                bool isSubclassOfGpBuildingBlock = GpRunner.IsSubclassOfGpBuildingBlock(t);
                MinTreeData minTreeData = isSubclassOfGpBuildingBlock
                    ? gp.nodeTypeToMinTreeDictionary[t]
                    : gp.nodeReturnTypeToMinTreeDictionary[new ReturnTypeSpecification(t, null)];

                int possibleRampedDepth = individualNumber % gp.populationParameters.maxDepth + 1;
                int currentMaxDepth =
                    gp.populationParameters.ramp
                        ? Math.Max(minTreeData.heightOfMinTree + 1, possibleRampedDepth)
                        : gp.populationParameters.maxDepth;

                Node randomTree = gp.GenerateRandomTreeFromTypeOrReturnType<T>(currentMaxDepth, gp.rand.NextBool());
                if (uniqueNodes.Contains(randomTree, new NodeComparer()))
                {
                    CustomPrinter.PrintLine("Duplicate node found in initialization");
                    continue;
                }

                uniqueNodes.Add(randomTree);

                foreach (Node child in randomTree.children)
                foreach (Node node in child.IterateNodes())
                {
                    TypeProbability tp = gp.populationParameters.probabilityDistribution.distribution
                        .First(typeProbability => typeProbability.type == node.GetType());
                    Debug.Assert(tp.probability != 0);
                }


                Debug.Assert(randomTree.GetHeight() <= gp.populationParameters.maxDepth);

                var ind = new Individual(randomTree);
                population.Add(ind);
            }

            if (null != this.genomeToStartInPopulation)
            {
                var goalIndividual = new Individual(this.genomeToStartInPopulation);
                population.Add(goalIndividual);
            }

            await gp.EvaluatePopulation(population);

            return population;
        }
    }


    public class RandomPopulationInitialization : PopulationInitializationMethod
    {
        public RandomPopulationInitialization(GpPopulationParameters populationParameters) : base(populationParameters)
        {
        }

        public RandomPopulationInitialization(ProbabilityDistribution probabilityDistribution, int maxDepth)
            : base(probabilityDistribution, maxDepth)
        {
        }


        public override async Task<List<Individual>>
            GetPopulation<T>(GpRunner gp, TimeoutInfo timeoutInfo) // Get around T by using MakeClosedGenericType
        {
            var population = new List<Individual>();
            for (var i = 0; i < gp.populationParameters.populationSize && !timeoutInfo.ShouldTimeout; i++)
            {
                const bool forceFullyGrow = false;
                Node randomTree =
                    gp.GenerateRandomTreeFromTypeOrReturnType<T>(gp.populationParameters.maxDepth, forceFullyGrow);
                var ind = new Individual(randomTree);
                population.Add(ind);
            }

            await gp.EvaluatePopulation(population);

            return population;
        }
    }
}