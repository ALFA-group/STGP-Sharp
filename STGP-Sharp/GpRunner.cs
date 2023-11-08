#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.Interfaces;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;

#endregion

// ReSharper disable SuspiciousTypeConversion.Global

namespace STGP_Sharp
{
    public struct TimeoutInfo
    {
        public int timeLimitInSeconds;
        public DateTime runStartTime;
        public bool ignoreGenerationsUseTimeout;
        public CancellationTokenSource cancelTokenSource;

        public bool ShouldTimeout =>
            (this.ignoreGenerationsUseTimeout && GeneralCSharpUtilities.SecondsElapsedSince(this.runStartTime) >
                this.timeLimitInSeconds) || this.cancelTokenSource.IsCancellationRequested;
    }


    // The only state it maintains right now is verbose info
    // Maybe it's just better to change it to a static class?
    public partial class GpRunner
    {
        private readonly Type _fitnessType;
        private readonly Type _gpResultsStatsType;
        private readonly string? _checkPointSaveFile;
        public readonly IFitnessFunction fitnessFunction;
        public NamedArguments? namedArguments;
        public GpPopulationParameters populationParameters;
        public readonly PositionalArguments? positionalArguments;
        public readonly IGpExperimentProgress? progress;
        public readonly Random rand;
        public readonly int? randomSeed;
        public readonly Type solutionReturnType;
        public readonly TimeoutInfo timeoutInfo;

        public readonly VerboseInfo verbose;

        // Action to perform after evaluating a population.
        private readonly Action? _postFitnessEvaluationFunction;

        // Action to perform at the end of each generation.
        private readonly Action? _postGenerationFunction;

        public GpRunner(
            IFitnessFunction fitnessFunction,
            GpPopulationParameters populationParameters,
            Type solutionReturnType,
            TimeoutInfo timeoutInfo,
            int? randomSeed = null,
            bool verbose = false,
            IGpExperimentProgress? progress = null,
            NamedArguments? namedArguments = null,
            PositionalArguments? positionalArguments = null,
            string? checkPointSaveFile = null,
            Action? postFitnessEvaluationFunction = null,
            Action? postGenerationFunction = null)
        {
            this.fitnessFunction = fitnessFunction;
            this.solutionReturnType = solutionReturnType;
            this.timeoutInfo = timeoutInfo;
            this.populationParameters = populationParameters;
            this.randomSeed = randomSeed;
            this.verbose = verbose;
            this.rand = this.randomSeed != null ? new Random(this.randomSeed.Value) : new Random();
            this.progress = progress;
            this.namedArguments = namedArguments;
            this.positionalArguments = positionalArguments;
            this._checkPointSaveFile = checkPointSaveFile;
            this._postFitnessEvaluationFunction = postFitnessEvaluationFunction;
            this._postGenerationFunction = postGenerationFunction;

            this._fitnessType = this.fitnessFunction.FitnessType;

            GpResultsUtility.ValidateFitnessFunctionAndRelatedClasses(this.fitnessFunction);

            this._gpResultsStatsType = GpResultsUtility.GetGpResultsStatsType(this._fitnessType);

            // NOTE: Probability distribution must be defined before the helper
            this.SetMinTreeDictionariesForSatisfiableNodeTypes();
        }

        public async Task<GeneratedPopulations> RunAsync()
        {
            if (null != this.progress)
            {
                this.progress.GenerationsInRunCount = this.populationParameters.numberGenerations;
                this.progress.GenerationsInRunCompleted = 0;
                this.progress.Status = "Init population";
            }

            Type initializationMethodType = this.populationParameters.populationInitializationMethod.GetType();
            MethodInfo initializationMethodInfo =
                initializationMethodType.GetMethod("GetPopulation") ??
                throw new Exception($"GetPopulation is not defined in the class {initializationMethodType.Name}");

            List<Individual> population = await (Task<List<Individual>>)(initializationMethodInfo
                .MakeGenericMethod(this.solutionReturnType)
                .Invoke(this.populationParameters.populationInitializationMethod,
                    new object[] { this, this.timeoutInfo }) ?? throw new Exception("Could not initialize method"));

            if (this.timeoutInfo.ShouldTimeout || this.populationParameters.numberGenerations == 0)
            {
                return new GeneratedPopulations(
                    new[] { population }.ToNestedList(),
                    GpResultsUtility.GetDetailedSummary(
                        this._gpResultsStatsType,
                        population), this.timeoutInfo.runStartTime,
                    DateTime.Now,
                    population.SortedByFitness().FirstOrDefault(), this.verbose
                );
            }


            this.MaybeSaveProgressToCheckpointJson(population);

            if (null != this.progress)
            {
                this.progress.Status = "Evolving";
            }

            GeneratedPopulations allPopulationsFromRun = await this.SearchLoop(population);
            return allPopulationsFromRun;
        }

        private static void ThrowErrorIfAnyIndividualHasNullFitness(IEnumerable<Individual> population)
        {
            List<Individual> nullFitness = population.Where(i => null == i.fitness).ToList();
            if (nullFitness.Any())
            {
                throw new Exception($"Fitness is null for individuals {string.Join(", ", nullFitness)}");
            }
        }

        private async Task<GeneratedPopulations> SearchLoop(
            List<Individual> population)
        {
            // The caller is required to already have evaluated the given population
            Debug.Assert(population.All(i => null != i.fitness));

            if (null != this.progress)
            {
                this.progress.GenerationsInRunCount = this.populationParameters.numberGenerations;
                this.progress.GenerationsInRunCompleted = 0;
            }

            var allPopulations = new List<List<Individual>>();


            population = population.SortedByFitness();

            allPopulations.Add(population);

            Individual bestEver = population.FirstOrDefault() ??
                                  throw new Exception("Population is empty");

            ThrowErrorIfAnyIndividualHasNullFitness(population);

            for (var generationIndex = 0;
                 this.timeoutInfo.ignoreGenerationsUseTimeout ||
                 generationIndex < this.populationParameters.numberGenerations;
                 generationIndex++)
            {
                List<Individual> newPopulation = await this.GenerateNewPopulation(population);

                newPopulation = newPopulation.SortedByFitness();
                bestEver = newPopulation.First();

                allPopulations.Add(newPopulation);

                ThrowErrorIfAnyIndividualHasNullFitness(newPopulation);

                if (null != this.progress)
                {
                    this.progress.GenerationsInRunCompleted = generationIndex + 1;
                }

                if (this.timeoutInfo.ShouldTimeout)
                {
                    break;
                }

                // Only add checkpoint save if this isn't the last generation
                this.MaybeSaveProgressToCheckpointJson(allPopulations.Flatten());

                this._postGenerationFunction?.Invoke();

                CustomPrinter.PrintLine($"Generation {generationIndex}");
            }

            return new GeneratedPopulations(
                allPopulations,
                GpResultsUtility.GetDetailedSummary(
                    this._gpResultsStatsType,
                    allPopulations.Flatten()), this.timeoutInfo.runStartTime,
                DateTime.Now,
                bestEver, this.verbose);
        }

        partial void SetMinTreeDictionariesForSatisfiableNodeTypes();

        public async Task EvaluatePopulation(List<Individual> population)
        {
            IEnumerable<FitnessBase> results;
            IEnumerable<int> enumerable = Enumerable.Range(0, population.Count);
            switch (this.fitnessFunction)
            {
                case ICoevolutionSync coevolutionFitnessFunction:
                    results =
                        coevolutionFitnessFunction.GetFitnessOfPopulationUsingCoevolution(this, population);
                    break;
                case ISync syncFitnessFunction:
                    results = enumerable.Select(i =>
                        syncFitnessFunction.GetFitnessOfIndividual(this, population[i]));
                    break;
                case ICoevolutionAsync coevolutionAsyncFitnessFunction:

                    results = await coevolutionAsyncFitnessFunction.GetFitnessOfPopulationUsingCoevolutionAsync(this,
                        population);
                    break;
                case IAsync asyncFitnessFunction:

                    IEnumerable<Task<FitnessBase>> asyncTasks = enumerable.Select(i =>
                        asyncFitnessFunction.GetFitnessOfIndividualAsync(this, population[i]));
                    results = await Task.WhenAll(asyncTasks);
                    break;
                default:
                    throw new Exception(
                        "The given fitness function does not implement either IAsync," +
                        "ICoevolutionSync, ICoevolutionSync, or ISync.");
            }

            List<FitnessBase> resultsAsList = results.ToList();

            for (var i = 0; i < population.Count; i++)
            {
                population[i].fitness = resultsAsList[i];
            }

            this._postFitnessEvaluationFunction?.Invoke();
        }

        private (Node, Node)? OnePointCrossoverChildren(Node t1, Node t2)
        {
            Node a = t1.DeepCopy();
            Node b = t2.DeepCopy();

            Dictionary<int, List<int>> xPoints = GetLegalCrossoverPointsInChildren(a, b);
            if (!xPoints.Any())
            {
                if (this.verbose)
                {
                    CustomPrinter.PrintLine("No legal crossover points. Skipped crossover");
                }

                this.verbose.numberOfTimesNoLegalCrossoverPoints++;
                return null;
            }

            int xaRand = xPoints.Keys.GetRandomEntry(this.rand);
            int xbRand = xPoints[xaRand].GetRandomEntry(this.rand);

            NodeWrapper xaSubTreeNodeWrapper = a.GetNodeWrapperAtIndex(xaRand);
            NodeWrapper xbSubTreeNodeWrapper = b.GetNodeWrapperAtIndex(xbRand);

            if (xaSubTreeNodeWrapper.child.Equals(xbSubTreeNodeWrapper.child))
            {
                if (this.verbose)
                {
                    CustomPrinter.PrintLine("Nodes swapped are equivalent");
                }

                this.verbose.numberOfTimesCrossoverSwappedEquivalentNode++;
                return null;
            }

            if (xaSubTreeNodeWrapper.child.GetHeight() + b.GetDepthOfNodeAtIndex(xbRand) >
                this.populationParameters.maxDepth ||
                xbSubTreeNodeWrapper.child.GetHeight() + a.GetDepthOfNodeAtIndex(xaRand) >
                this.populationParameters.maxDepth)
            {
                if (this.verbose)
                {
                    CustomPrinter.PrintLine("Crossover too deep");
                    CustomPrinter.PrintLine($"     Height: {a.GetDepthOfNodeAtIndex(xaRand)} --");
                    xaSubTreeNodeWrapper.child.PrintAsList("     ");
                    CustomPrinter.PrintLine($"     Height: {b.GetDepthOfNodeAtIndex(xbRand)} -- ");
                    xbSubTreeNodeWrapper.child.PrintAsList("     ");
                }

                this.verbose.numberOfTimesCrossoverWasTooDeep++;
                return null;
            }

            Node tmp = xbSubTreeNodeWrapper.child;
            xbSubTreeNodeWrapper.ReplaceWith(xaSubTreeNodeWrapper.child);
            xaSubTreeNodeWrapper.ReplaceWith(tmp);
            Debug.Assert(xaSubTreeNodeWrapper.child.returnType == xbSubTreeNodeWrapper.child.returnType);

            return (a, b);
        }

        public void Mutate(Node root)
        {
            List<NodeWrapper> nodesToChooseFrom = root.IterateNodeWrapperWithoutRoot().ToList();

            NodeWrapper oldNode = nodesToChooseFrom.GetRandomEntry(this.rand);
            int randomTreeMaxDepth = root.GetHeight() - root.GetDepthOfNode(oldNode.child);

            if (oldNode.parent == null)
            {
                throw new NullReferenceException("Old node parent cannot be null.");
            }

            Node randomNode = oldNode.child.Mutate(this, randomTreeMaxDepth);

            if (oldNode.child.Equals(randomNode))
            {
                if (this.verbose)
                {
                    CustomPrinter.PrintLine("Mutated node is equivalent to old node.");
                }

                return;
            }

            oldNode.ReplaceWith(randomNode);
        }

        public List<Individual> TournamentSelection(List<Individual> population)
        {
            Debug.Assert(population.All(i => null != i.fitness));

            var winners = new List<Individual>();

            for (var tournamentNumber = 0;
                 tournamentNumber < this.populationParameters.populationSize;
                 tournamentNumber++)
            {
                List<Individual> tmpPopulation = population.ToList();
                var competitors = new List<Individual>();

                // Populate competitors
                for (var competitorNumber = 0;
                     competitorNumber < this.populationParameters.tournamentSize;
                     competitorNumber++)
                {
                    Individual competitor = tmpPopulation.GetRandomEntry(this.rand);
                    competitors.Add(competitor);
                    tmpPopulation.Remove(competitor);
                }

                competitors = competitors.SortedByFitness();
                Individual winner = competitors.FirstOrDefault() ??
                                    throw new Exception("List of competitors cannot be empty.");
                winners.Add(winner);
            }

            return winners;
        }

        public void GenerationalReplacement(ref List<Individual> newPop, List<Individual> oldPop)
        {
            // Sort the population
            oldPop = oldPop.SortedByFitness();
            newPop = newPop.SortedByFitness();

            // Store the original old population so we can check that individuals have been
            // propagated to the new population correctly.
            List<Individual> originalOldPop = oldPop.ToList();

            // Append "elite size" best solutions from the old population to the new population.
            newPop.AddRange(oldPop.Take(this.populationParameters.eliteSize));

            // Remove those solutions from the old population so that they are not selected
            // again when checking whether the new population is the correct size.
            oldPop.RemoveRange(0, this.populationParameters.eliteSize);

            // Check if the new population has the correct size.
            // If not, add to the new population however many are missing from the old population 
            if (newPop.Count < this.populationParameters.populationSize)
            {
                int numberMissing = this.populationParameters.populationSize - newPop.Count;
                newPop.AddRange(oldPop.Take(numberMissing));
            }

            newPop = newPop.SortedByFitness();

            // We may have added more individuals than allowed, so only take "population size" individuals
            if (newPop.Count > this.populationParameters.populationSize)
            {
                newPop = newPop.Take(this.populationParameters.populationSize).ToList();
            }

            Debug.Assert(newPop.Count == this.populationParameters.populationSize);

            FitnessBase bestNewPop = newPop.First().fitness ?? throw new InvalidOperationException();
            FitnessBase bestOldPop = originalOldPop.SortedByFitness().First().fitness ??
                                     throw new InvalidOperationException();
            if (bestNewPop.LessThan(bestOldPop))
            {
                throw new Exception("New population best is worse than old population best");
            }
        }


        public async Task<List<Individual>> GenerateNewPopulation(List<Individual> oldPopulation)
        {
            #region Selection

            List<Individual> parents = this.TournamentSelection(oldPopulation);

            #endregion

            #region Variation — Generate new individuals

            // Crossover
            List<Individual> newPopulation = this.CrossoverListOfParents(parents);

            // Mutation
            newPopulation.ForEach(i =>
            {
                if (this.rand.NextDouble() > this.populationParameters.mutationProbability)
                {
                    if (this.verbose)
                    {
                        CustomPrinter.PrintLine("Skipped mutation");
                    }

                    this.verbose.numberOfTimesMutationSkipped++;
                }
                else
                {
                    this.Mutate(i.genome);
                }
            });

            if (this.verbose && newPopulation.Count < this.populationParameters.populationSize)
            {
                CustomPrinter.PrintLine(
                    $"{this.populationParameters.populationSize - newPopulation.Count} individuals removed in deny list");
            }

            #endregion

            #region Evaluation

            await this.EvaluatePopulation(newPopulation);

            #endregion

            #region Generational replacement

            // Replace worst performing new individuals with the best performing old individuals
            this.GenerationalReplacement(ref newPopulation, oldPopulation);
            Debug.Assert(newPopulation.Count == this.populationParameters.populationSize);

            #endregion

            return newPopulation;
        }

        public List<Individual> CrossoverListOfParents(List<Individual> parents)
        {
            var newPopulation = new List<Individual>();

            // Ensure within size constraint
            while (newPopulation.Count < this.populationParameters.populationSize)
            {
                Individual parent1 = parents.GetRandomEntry(this.rand).DeepCopy();
                List<Individual> tmpParents = parents.ToList();
                tmpParents.Remove(parent1);
                Individual parent2 = tmpParents.GetRandomEntry(this.rand).DeepCopy();

                if (this.rand.NextDouble() > this.populationParameters.crossoverProbability)
                {
                    if (this.verbose)
                    {
                        CustomPrinter.PrintLine("Skipped crossover");
                    }

                    this.verbose.numberOfTimesCrossoverSkipped++;
                    newPopulation.Add(parent1);
                    if (newPopulation.Count < this.populationParameters.populationSize)
                    {
                        newPopulation.Add(parent2);
                    }

                    continue;
                }

                (Node, Node) crossoverChildren = this.OnePointCrossoverChildren(parent1.genome, parent2.genome) ??
                                                 (parent1.genome.DeepCopy(), parent2.genome.DeepCopy());
                (Node child1, Node child2) = crossoverChildren;

                var individual1 = new Individual(child1);
                newPopulation.Add(individual1);

                if (newPopulation.Count >= this.populationParameters.populationSize)
                {
                    continue;
                }

                var individual2 = new Individual(child2);
                newPopulation.Add(individual2);
            }

            return newPopulation;
        }
    }
}