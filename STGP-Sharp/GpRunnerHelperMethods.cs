#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;
using static STGP_Sharp.Utilities.GeneralCSharp.GeneralCSharpUtilities;

#endregion

namespace STGP_Sharp
{
    public partial class GpRunner
    {
        private void MaybeSaveProgressToCheckpointJson(List<Individual> population)
        {
            if (null == this._checkPointSaveFile)
            {
                return;
            }

            string directoryContainingCheckPointSaveFile =
                CreateAndGetDirectoryForFileIfNotExist(this._checkPointSaveFile);

            string uniqueCheckPointSaveFileName =
                Path.GetFileNameWithoutExtension(this._checkPointSaveFile) +
                $@"_{DateTime.Now.ToString("s",
                        CultureInfo.GetCultureInfo("en-US"))
                    .Replace(":", "-")}" +
                Path.GetExtension(this._checkPointSaveFile);

            string checkPointSaveFileNameFullPath =
                Path.Join(directoryContainingCheckPointSaveFile, uniqueCheckPointSaveFileName);

            CustomPrinter.PrintLine($"Saving progress to {checkPointSaveFileNameFullPath}\n");

            var generatedPopulations = new GeneratedPopulations(
                new[] { population }.ToNestedList(),
                GpResultsUtility.GetDetailedSummary(
                    this._gpResultsStatsType,
                    population), this.timeoutInfo.runStartTime,
                DateTime.Now,
                population.SortedByFitness().FirstOrDefault(), this.verbose);

            string json = JsonConvert.SerializeObject(generatedPopulations, Formatting.Indented);

            File.WriteAllTextAsync(checkPointSaveFileNameFullPath, json);
        }

        public Node GenerateRandomTreeFromTypeOrReturnType<T>(int maxDepth, bool forceFullyGrow)
        {
            Type t = typeof(T);
            bool isSubclassOfGpBuildingBlock = IsSubclassOfGpBuildingBlock(t);
            Node randomTree = isSubclassOfGpBuildingBlock
                ? this.GenerateRandomTreeOfType(t, maxDepth, forceFullyGrow)
                : this.GenerateRootNodeOfReturnType<T>(maxDepth);
            return randomTree;
        }

        public TypedRootNode<T> GenerateRootNodeOfReturnType<T>(int maxDepth)
        {
            bool mustFullyGrow = this.rand.NextBool();
            IEnumerable<FilterAttribute> filters = GetFilterAttributes(typeof(T));
            var returnTypeSpecification = new ReturnTypeSpecification(typeof(T), filters);
            var child = (GpBuildingBlock<T>)this.GenerateRandomTreeOfReturnType(returnTypeSpecification, maxDepth - 1,
                mustFullyGrow);

            if (child.GetHeight() + 1 > maxDepth)
            {
                throw new Exception("Somehow the max depth has been violated");
            }

            return new TypedRootNode<T>(child);
        }

        public static bool IsListOfSubTypeOfExecutableNode(Type t)
        {
            return typeof(List<>).IsAssignableFrom(t) &&
                   IsSubclassOfGpBuildingBlock(GetReturnTypeSpecification(t).returnType);
        }

        private Node GetChildFromParam(ParameterInfo param, int maxDepth, bool forceFullyGrow)
        {
            Type returnType = GpReflectionCache.GetReturnTypeFromGpBuildingBlockSubClass(param.ParameterType);
            IEnumerable<FilterAttribute> filters = GetFilterAttributes(param);
            var returnTypeSpecification = new ReturnTypeSpecification(returnType, filters);
            Node child = this.GenerateRandomTreeOfReturnType(returnTypeSpecification, maxDepth - 1, forceFullyGrow);

            if (child.GetHeight() > maxDepth - 1)
            {
                throw new Exception("Somehow the max depth has been violated");
            }

            return child;
        }

        private static bool IsTypedRootNodeLegal(Node typedRootNode, ProbabilityDistribution probabilityDistribution)
        {
            Type maybeTypedRootNodeType = typedRootNode.GetType();
            if (maybeTypedRootNodeType.GetGenericTypeDefinition() != typeof(TypedRootNode<>))
            {
                throw new Exception("Root node must be a TypedRootNode");
            }

            ReturnTypeSpecification returnTypeSpecification = GetReturnTypeSpecification(maybeTypedRootNodeType);
            List<Type> allTypes = GetTerminalsOfReturnType(returnTypeSpecification).ToList();
            allTypes.AddRange(GetNonTerminalsOfReturnType(returnTypeSpecification));
            return allTypes.Any(t => probabilityDistribution.GetProbabilityOfType(t) > 0);
        }

        public static bool IsValidTree(Node root, ProbabilityDistribution probabilityDistribution, int maxDepth)
        {
            var satisfiesTypeConstraints = true;
            if (root.GetType().GetGenericTypeDefinition() == typeof(TypedRootNode<>))
            {
                satisfiesTypeConstraints = IsTypedRootNodeLegal(root, probabilityDistribution);
            }

            satisfiesTypeConstraints =
                satisfiesTypeConstraints && NodeSatisfiesTypeConstraints(root, probabilityDistribution);

            return root.GetHeight() <= maxDepth && satisfiesTypeConstraints;
        }

        private static bool HaveCompatibleReturnTypes(Type executableNodeType1, Type executableNodeType2)
        {
            Debug.Assert(IsSubclassOfGpBuildingBlock(executableNodeType1) &&
                         IsSubclassOfGpBuildingBlock(executableNodeType2));
            return executableNodeType1.IsAssignableFrom(executableNodeType2) ||
                   executableNodeType2.IsAssignableFrom(executableNodeType1);
        }

        private static bool NodeSatisfiesTypeConstraints(Node node, ProbabilityDistribution probabilityDistribution)
        {
            Type nodeType = node.GetType();

            if (IsTerminal(nodeType) && probabilityDistribution.GetProbabilityOfType(nodeType) > 0)
            {
                return true;
            }

            if (probabilityDistribution.GetProbabilityOfType(nodeType) <= 0)
            {
                return false;
            }

            ConstructorInfo[] constructors = nodeType.GetConstructors();
            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (node.children.Count != parameters.Length)
                {
                    continue;
                }

                List<Type> parameterTypes = parameters.Select(pInfo => pInfo.ParameterType).ToList();
                List<Type> childrenTypes = node.children.Select(child => child.GetType()).ToList();

                // This is assuming constructor arguments and the children in a Node are in the same order respectively 
                // which is currently the case because we do so elsewhere in the code (ie. GenerateRandomTreeOfType)
                IEnumerable<(Type p, Type c)> zippedTypes = parameterTypes.Zip(childrenTypes, (p, c) => (p, c));
                if (zippedTypes.All(t => HaveCompatibleReturnTypes(t.Item1, t.Item2)))
                {
                    return node.children.All(child => NodeSatisfiesTypeConstraints(child, probabilityDistribution));
                }
            }

            return false;
        }

        public static int GetNumberOfChildrenOfGpBuildingBlockType(Type t)
        {
            return GetParametersOfGpBuildingBlockType(t, out _).Length;
        }

        public static ParameterInfo[] GetParametersOfGpBuildingBlockType(Type t, out ConstructorInfo constructor)
        {
            Debug.Assert(IsSubclassOfGpBuildingBlock(t));

            constructor =
                GetRandomTreeConstructor(t) ??
                throw new Exception(
                    $"The type {t.Name} does not have a constructor with the RandomTreeConstructor attribute");

            ParameterInfo[] parameters = constructor.GetParameters();
            return parameters;
        }

        public Node GenerateRandomTreeOfType(Type t, int currentMaxDepth, bool forceFullyGrow)
        {
            ParameterInfo[] parameters = GetParametersOfGpBuildingBlockType(t, out ConstructorInfo constructor);

            var constructorArguments = new List<object?>();


            foreach (ParameterInfo param in parameters)
            {
                if (param.ParameterType == typeof(GpFieldsWrapper))
                {
                    constructorArguments.Add(new GpFieldsWrapper(this));
                }
                else if (param.ParameterType == typeof(int) && param.Name == "maxDepth")
                {
                    constructorArguments.Add(currentMaxDepth - 1);
                }
                else if (param.ParameterType == typeof(bool) && param.Name == "forceFullyGrow")
                {
                    constructorArguments.Add(forceFullyGrow);
                }
                else
                {
                    constructorArguments.Add(this.GetChildFromParam(param, currentMaxDepth, forceFullyGrow));
                }
            }

            var node = (Node)constructor.Invoke(constructorArguments.ToArray());

            if (node.GetHeight() > currentMaxDepth)
            {
                throw new Exception("Somehow the max depth has been violated.");
            }

            return node;
        }

        public Node GenerateRandomTreeOfReturnType(ReturnTypeSpecification returnTypeSpecification, int currentMaxDepth,
            bool forceFullyGrow)
        {
            Debug.Assert(GetAllReturnTypes().Contains(returnTypeSpecification.returnType));

            Type randomSubType;
            List<FilterAttribute> filterAttributes = returnTypeSpecification.filters.ToList();

            List<Type> nonTerminals = GetNonTerminalsOfReturnType(returnTypeSpecification);
            double? nonTerminalsProbabilitySum = nonTerminals.Sum(t =>
                this.populationParameters.probabilityDistribution.GetProbabilityOfType(t));
            bool hasLegalNonTerminals = nonTerminals.Count > 0 && nonTerminalsProbabilitySum > 0;

            IEnumerable<Type> terminals =
                GetTerminalsOfReturnType(new ReturnTypeSpecification(returnTypeSpecification.returnType,
                    filterAttributes));
            List<Type> terminalsList = terminals.ToList();
            double? terminalsProbabilitySum = terminalsList.Sum(t =>
                this.populationParameters.probabilityDistribution.GetProbabilityOfType(t));
            bool hasLegalTerminals = terminalsList.Count > 0 && terminalsProbabilitySum > 0;

            bool randomChanceToStopGrowing = hasLegalTerminals && !forceFullyGrow && this.rand.NextBool();

            if (!this.nodeReturnTypeToMinTreeDictionary.TryGetValue(returnTypeSpecification, out MinTreeData minTree))
            {
                throw new MinTreeNotSatisfiable(returnTypeSpecification.returnType);
            }

            if (minTree.heightOfMinTree > currentMaxDepth)
            {
                throw new MinTreeNotSatisfiable(returnTypeSpecification.returnType);
            }

            if (minTree.heightOfMinTree == currentMaxDepth)

                // Generate Random Tree will generate the min tree if given the height of the min
                // tree as the current max depth which right now is equal to the current max depth anyways.
            {
                randomSubType = minTree.permissibleNodeTypes.GetRandomEntry(this.rand);
            }
            else if (currentMaxDepth < 1 || !hasLegalNonTerminals || randomChanceToStopGrowing)
            {
                randomSubType = this.GetRandomTerminalOfReturnType(returnTypeSpecification);
            }
            else
            {
                randomSubType = this.GetRandomNonTerminalOfReturnType(returnTypeSpecification, currentMaxDepth);
            }

            Node tree = this.GenerateRandomTreeOfType(randomSubType, currentMaxDepth, forceFullyGrow);
            if (tree.GetHeight() > currentMaxDepth)
            {
                throw new Exception("Somehow the max depth has been violated.");
            }

            return tree;
        }

        public static IEnumerable<FilterAttribute> GetFilterAttributes(Type t)
        {
            return t.GetCustomAttributes<FilterAttribute>();
        }

        private static IEnumerable<FilterAttribute> GetFilterAttributes(ParameterInfo param)
        {
            return param.GetCustomAttributes<FilterAttribute>();
        }

        private static bool SatisfiesAllFilterAttributes(Type t, IEnumerable<FilterAttribute> filters)
        {
            return filters.All(f => f.IsSatisfiedBy(t));
        }

        private static IEnumerable<Type> GetAllSubTypesWithReturnType(Type openGenericType,
            ReturnTypeSpecification returnTypeSpecification)
        {
            Type closedGenericType = openGenericType.MakeGenericType(returnTypeSpecification.returnType);
            return ReflectionUtilities.GetAllTypesFromAllAssemblies()
                .Where(t =>
                    closedGenericType.IsAssignableFrom(t) &&
                    t != closedGenericType &&
                    SatisfiesAllFilterAttributes(t, returnTypeSpecification.filters)
                );
        }


        public static bool IsSubclassOfGpBuildingBlock(Type type)
        {
            return GpReflectionCache.IsSubclass(typeof(GpBuildingBlock<>), type);
        }

        public static bool IsTerminal(Type t)
        {
            ConstructorInfo? constructor = GetRandomTreeConstructor(t);
            if (constructor == null)
            {
                return false;
            }

            ParameterInfo[] parameters = constructor.GetParameters();
            bool zeroParams = parameters.Length == 0;
            bool gpRunnerParam =
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(GpFieldsWrapper);

            return zeroParams || gpRunnerParam;
        }

        private static IEnumerable<Type> GetTerminalsOfReturnType(ReturnTypeSpecification returnTypeSpecification)
        {
            IEnumerable<Type> allTypes =
                GetAllSubTypesWithReturnType(typeof(GpBuildingBlock<>), returnTypeSpecification);
            return allTypes.Where(IsTerminal);
        }

        private Type GetRandomTerminalOfReturnType(ReturnTypeSpecification returnTypeSpecification)
        {
            List<Type> terminals = GetTerminalsOfReturnType(returnTypeSpecification).ToList();

            return this.GetRandomTypeFromDistribution(terminals);
        }

        private static IEnumerable<Type> GetTerminals()
        {
            IEnumerable<Type> allTypes = GetSubclassesOfGpBuildingBlock();
            return allTypes.Where(IsTerminal);
        }

        private static List<Type> GetNonTerminalsOfReturnType(ReturnTypeSpecification returnTypeSpecification)
        {
            IEnumerable<Type> allTypes =
                GetAllSubTypesWithReturnType(typeof(GpBuildingBlock<>), returnTypeSpecification);
            return allTypes.Except(GetTerminalsOfReturnType(returnTypeSpecification)).ToList();
        }

        private Type GetRandomNonTerminalOfReturnType(ReturnTypeSpecification returnTypeSpecification,
            int currentMaxDepth)
        {
            IEnumerable<Type> nonTerminals = GetNonTerminalsOfReturnType(returnTypeSpecification)
                .Where(t => this.nodeTypeToMinTreeDictionary.TryGetValue(t, out MinTreeData minTree) &&
                            minTree.heightOfMinTree <= currentMaxDepth);
            List<Type> allowedTypes = nonTerminals.ToList();
            if (!allowedTypes.Any())
            {
                throw new MinTreeNotSatisfiable(returnTypeSpecification.returnType);
            }

            return this.GetRandomTypeFromDistribution(allowedTypes);
        }

        private static ConstructorInfo? GetRandomTreeConstructor(Type t)
        {
            Debug.Assert(IsSubclassOfGpBuildingBlock(t));

            ConstructorInfo[] constructors = t.GetConstructors();
            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            var constructorsWithParameters = 0;
            ConstructorInfo? constructorToReturn = null;
            foreach (ConstructorInfo? candidateConstructor in constructors)
            {
                if (null != candidateConstructor.GetCustomAttribute<RandomTreeConstructorAttribute>())
                {
                    return candidateConstructor;
                }

                if (candidateConstructor.GetParameters().Length > 0)
                {
                    constructorToReturn = candidateConstructor;
                    constructorsWithParameters++;
                }
            }

            if (constructorsWithParameters > 1)
            {
                throw new Exception(
                    "There is no constructor decorated with the attribute [RandomTreeConstructor] " +
                    $"and there are multiple constructors with more than 0 parameters defined for the type {t.Name}. " +
                    "This is a limitation of the code we have written. " +
                    $"You must change the definition of the type {t.Name} to continue.");
            }

            return constructorToReturn;
        }

        private Type GetRandomTypeFromDistribution(ProbabilityDistribution typeProbabilities)
        {
            return GetRandomElementFromDistribution(typeProbabilities.ToDictionary(), this.rand);
        }

        public Type GetRandomTypeFromDistribution()
        {
            return this.GetRandomTypeFromDistribution(this.populationParameters.probabilityDistribution);
        }

        private Type GetRandomTypeFromDistribution(IEnumerable<Type> allowedTypes)
        {
            IEnumerable<TypeProbability> filteredTypes =
                this.populationParameters.probabilityDistribution.distribution.Where(tp =>
                    tp != null && allowedTypes.Contains(tp.type));
            return this.GetRandomTypeFromDistribution(new ProbabilityDistribution(filteredTypes.ToList()));
        }

        public static IEnumerable<Type> GetSubclassesOfGpBuildingBlock(bool sortAlphabetically = false)
        {
            IEnumerable<Type> types = GpReflectionCache.GetAllSubTypes(typeof(GpBuildingBlock<>))
                .Except(new List<Type>
                {
                    typeof(TypedRootNode<>)
                })
                .Where(t => t is {IsAbstract: false, IsGenericType: false, IsGenericTypeDefinition: false});

            return sortAlphabetically
                ? types.OrderBy(GpUtility.GetNiceName)
                : types;
        }

        public static Node LoadTreeFromFile(string file)
        {
            file = GetRelativePath(file);
            Node tree = JsonConvert.DeserializeObject<Node>(File.ReadAllText(file)) ??
                        throw new Exception($"File {file} does not contain a GP Tree.");

            return tree;
        }

        public static IEnumerable<Type> GetFitnessFunctionTypes()
        {
            return ReflectionUtilities.GetAllTypesFromAllAssemblies().Where(t =>
                typeof(IFitnessFunction).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                null == Attribute.GetCustomAttribute(t,
                    typeof(CompilerGeneratedAttribute))); // Exclude compiler generated classes
        }

        public static IEnumerable<Type> GetAllReturnTypes()
        {
            IEnumerable<Type> allTypes = GetSubclassesOfGpBuildingBlock();
            Type[] allTypesArray = allTypes as Type[] ?? allTypes.ToArray();
            List<Type> returnTypes = allTypesArray
                .Where(t =>
                    t is { IsGenericType: false, ContainsGenericParameters: false, BaseType: { IsGenericType: true } } && 
                    t.BaseType.GetGenericTypeDefinition().IsAssignableFrom(typeof(GpBuildingBlock<>)))
                // The previous line checks that it's a subclass of ExecutableNode, so it obviously has a non-null base type.
                .Select(t => GpReflectionCache.GetReturnTypeFromGpBuildingBlockSubClass(t.BaseType!)).ToList();
            returnTypes.AddRange(allTypesArray);
            return returnTypes;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static PropertyInfo[] GetAllGpBuildingBlockProperties(Type t)
        {
            if (!IsSubclassOfGpBuildingBlock(t))
            {
                throw new Exception($"Type {t.Name} is not a subclass of executable tree");
            }

            var props = new List<PropertyInfo>();


            // This is the case because t is always a subclass of GpBuildingBlock which
            // always extends from Node.
            Type? currentType = t;
            do
            {
                List<PropertyInfo>? newProps = currentType?
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .ToList();
                if (newProps != null)
                {
                    props.AddRange(newProps);
                }
            } while ((currentType = currentType!.BaseType) != typeof(Node));

            return props.ToArray();
        }

        public static string? GetChildPropertyNameAtChildrenIndex(int i, Node? child)
        {
            if (child == null)
            {
                throw new Exception("Child cannot be null");
            }

            PropertyInfo[] childProperties = GetAllGpBuildingBlockProperties(child.GetType());

            return childProperties
                .Where(prop =>
                    ReferenceEquals(prop.GetValue(child, null), child.children[i]))
                .Select(prop => prop.Name)
                .FirstOrDefault();
        }

        private static ReturnTypeSpecification GetReturnTypeSpecification(Type t)
        {
            Type returnType = GpReflectionCache.GetReturnTypeFromGpBuildingBlockSubClass(t);
            IEnumerable<FilterAttribute> filters = GetFilterAttributes(t);
            return new ReturnTypeSpecification(returnType, filters);
        }

        public static Dictionary<int, List<int>> GetLegalCrossoverPointsInChildren(Node a, Node b)
        {
            var xPoints = new Dictionary<int, List<int>>();
            var typesFound = new Dictionary<ReturnTypeSpecification, List<int>>();
            var i = 1;
            foreach (Node node in a.IterateNodes().Skip(1))
            {
                List<FilterAttribute> filters = GetFilterAttributes(node.GetType()).ToList();
                var nodeSpec = new ReturnTypeSpecification(node.returnType, filters);
                List<int> locations = typesFound.ContainsKey(nodeSpec)
                    ? typesFound[nodeSpec]
                    : b.GetSymTypeAndFilterLocationsInDescendants(node.returnType, filters).ToList();

                if (locations.Count > 0)
                {
                    typesFound[nodeSpec] = locations;
                    xPoints[i] = typesFound[nodeSpec];
                }

                i++;
            }

            return xPoints;
        }
    }
}