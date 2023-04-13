# STGP-Sharp
`STGP-Sharp` is a strongly-typed genetic programming framework for C#. It is directly compatible with Unity.

`STGP-Sharp` is a standalone version of the genetic programming framework used in [ABL-Unity3D](https://github.com/ALFA-group/ABL-Unity3D). Currently, it provides more capabilities such as co-evolution and slightly different implementation details.


## Requirements
- DotNet 6.0

## Sample Experiments
We provide sample experiments in the file `SampleExperiments.cs`. The experiment provided is one in which we use GP to generate a genome using the logical operators AND and NOT to create an equivalent operator to the logical operator OR.

## GP Search Algorithm
We provide a standard GP search algorithm with the capabilities for co-evolution, as well as asynchronous individual evaluation. Whether the search algorithm uses co-evolution or asynchronous individual evaluation is determined by the user-provided fitness function. See [here](#defining-a-fitness-function) for more information on how to define a fitness function.

The GP uses [reflection](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/reflection) to find user-defined GP operators, which are then used in the search algorithm to generate individuals.

In addition, the user must provide a probability distribution for the different GP operators. This probability distribution is used by the GP search algorithm to randomly generate individuals. 

Lastly, STGP-Sharp provides the option to use multiple different population initialization methods. By default, random and ramped initialization are included. The user can create their own.

**NOTE:** We use the terms "genome", "node", and "tree" interchangebly.

### Defining a GP Operator
<span style="background-color: light-gray">`GpBuildingBlock<T>`</span> is the super-class for GP primitives where <span style="background-color: light-gray">`T`</span> is the evaluation type. `GpBuildingBlock<T>` extends the class `Node`, which is a generic class for a genome with no strong typing. <span style="background-color: light-gray">`GpBuildingBlock<T>.children`</span> is the list of child nodes. Child nodes extend <span style="background-color: light-gray">`GpBuildingBlock<T>`</span>. A GP primitive must define a constructor which passes all child nodes to the primitive base constructor. A GP primitive must also override the function <span style="background-color: light-gray">`Evaluate`</span>, which returns the result of the GP primitive upon evaluation. The return type of <span style="background-color: light-gray">`Evaluate`</span> must be the same as the evaluation type <span style="background-color: light-gray">`T`</span> for the GP primitive.

The following figure shows sample code for defining the `Not` GP primitive shown in Figure <a href="#gpResults" data-reference-type="ref" data-reference="gpResults">2</a>, which models the boolean operator NOT. The GP primitive <span style="background-color: light-gray">`Not`</span> has an evaluation type of <span style="background-color: light-gray">`bool`</span>, as shown by line 1. Line 3 is a helper properties. Lines 5-7 define the constructor for the <span style="background-color: light-gray">`Not`</span> class. Lastly, lines 9-12 define the <span style="background-color: light-gray">`Evaluate`</span> method. Line 11 evaluates the child node, and then returns the negated value.

```csharp
1.  public class Not : GpBuildingBlock<bool>  
2.  {  
3.      public GpBuildingBlock<bool> Operand => (GpBuildingBlock<bool>)this.children[0]; 
4. 
5.      public Not(GpBuildingBlock<bool> operand) : base(operand)  
6.      {  
7.      }  
8. 
9.      public override bool Evaluate(GpFieldsWrapper gpFieldsWrapper)  
10.     {
11.          return !this.Operand.Evaluate(gpFieldsWrapper); 
12.     }
13.  }
```
Child nodes of a GP primitive can be immutable; genetic operators can not modify them. For example, replace line 6 with a hard-coded instance of a subclass of <span style="background-color: light-gray">`GpBuildingBlock<bool>`</span>. E.g., an instance of the GP primitive `BooleanConstant`.

### Fitness
There are different types of fitness values. For example, the following [project](https://github.com/18goldr/BomberLand-Evostar-2023) implements lexicographic fitness. 

Each fitness type must extend the class `FitnessBase`. 

Given that there are different types of fitness values, it may or may not make sense for certain types of fitness values to be compared to other fitness types. Thus, the `FitnessBase` class provides a helper function `ThrowExceptionIfInvalidFitnessForComparison` which throws an exception if two incompatible types of fitness are compared.

Lastly, each fitness type also has an associated type to generate statistics for that fitness type. For example, lexicographic fitness can define summary statistics for each dimension of the fitness value. To do so, you must define the static property `GpResultsStatsType` (see [here](#gpResultsStats) for my details).

Below is a partial implementation of the `FitnessStandard` class. We only show part of the implementation so as to illustrate the most important pieces. Line 1 defines the class by extending `FitnessBase`. Lines 3-8 define the fitness value score. Line 10 defines the associated `GpResultsStats` class for the `FitnessStandard` class. Lines 12-15 define a helper function to determine if the a fitness type is of type `FitnessStandard`. Otherwise, it throws an exception. Line 17-26 define the `CompareTo` function which is called when comparing two fitness values. Lines 19-22 check if the fitness to compared against is null. Line 24 calls the helper function `ThrowExceptionIfInvalidFitness` to verify the fitness being compared is of the correct type (e.g. `FitnessStandard`). Lastly, line 25 compares the fitness score of the current `FitnessStandard` instance to the fitness value being compared to.

```csharp
1.  public class FitnessStandard : FitnessBase  
2.  {  
3.       public readonly double fitnessScore;  
4.   
5.       public FitnessStandard(double fitnessScore)  
6.       {  
7.           this.fitnessScore = fitnessScore;  
8.       }  
9.   
10.      public new static Type GpResultsStatsType { get; } = typeof(GpResultsStatsStandard);  
11. 
12.      public static void ThrowExceptionIfInvalidFitness(FitnessBase? f, out FitnessStandard fs)  
13.      {  
14.          ThrowExceptionIfInvalidFitnessForComparison(f, out fs);  
15.      }  
16.   
17.      public override int CompareTo(FitnessBase? other)  
18.      {  
19.          if (null == other)  
20.          {  
21.              return this.fitnessScore.CompareTo(null);  
22.          }  
23. 
24.          ThrowExceptionIfInvalidFitness(other, out FitnessStandard otherStandard);  
25.          return this.fitnessScore.CompareTo(otherStandard.fitnessScore);  
26.      }
27. 
28.      ...
29. }
```


### Defining a Fitness Function
To define a fitness function, define a class that implements the interface<span style="background-color: light-gray">`IFitnessFunction`</span>. In addition, the sub-class must implement several different interfaces. If the fitness function works with co-evolution, it must either implement <span style="background-color: light-gray">`ICoevolutionAsync`</span> or <span style="background-color: light-gray">`ICoevolutionSync`</span> depending on whether the fitness function is asynchronous. Similarly, if it does not work with coevolution, it must implement <span style="background-color: light-gray">`IAsync`</span> or <span style="background-color: light-gray">`ISync`</span> depending on whether the fitness function is asynchronous. 

If the sub-class works with co-evolution,  it must implement the evaluation function  <span style="background-color: light-gray">`GetFitnessOfPopulationUsingCoevolution`</span>/<span style="background-color: light-gray">`GetFitnessOfPopulationUsingCoevolutionAsync`</span>.

If the sub-class does not work with co-evolution, it must implement the evaluation function  <span style="background-color: light-gray">`GetFitnessOfIndividual`</span>/<span style="background-color: light-gray">`GetFitnessOfIndividualAsync`</span>.

Additionally, each fitness function must define an associated  <span style="background-color: light-gray">`Fitness`</span> type (see [here](#fitness) for more details on <span style="background-color: light-gray">`Fitness`</span> types). To do so, define the property  <span style="background-color: light-gray">`FitnessType`</span>. 

Below is an implementation of the fitness function to model the logical OR operator using the logical operators AND and NOT. Lines 1-3 define that the fitness function is to be run synchronously. Line 3 defines that the fitness function works with fitness type `FitnessStandard`. Lines 6-8 check if the given individual has the correct return type, aka `boolean`. Essentially, the function checks the given individual against the truth table of the operator OR. Line 16 defines a `PositionalArguments` which is used to pass the booleans `b1` and `b2` to the evaluation function of the genome. Line 17 defines a wrapper which is used by the evaluation function of the genome. Lines 11 and 18-25 are used to evaluate the accuracy of the genome in modelling the logical OR operator. Line 26 then returns the result as an instance of the `FitnessStandard` class.

```csharp
1. public class EquivalentToOr : IFitnessFunction, ISync  
2. {  
3.      public Type FitnessType { get; } = typeof(FitnessStandard);  
4.      public FitnessBase GetFitnessOfIndividual(GpRunner gp, Individual i)  
5.      {  
6.          if (i.genome is not GpBuildingBlock<bool> evaluateBoolean)  
7.          {  
8.              throw new Exception("Genome does not evaluate to boolean");  
9.          }  
10.  
11.         float fitnessScoreSoFar = 0;  
12.         foreach (bool b1 in new[] { true, false })  
13.         {  
14.             foreach (bool b2 in new[] { true, false })  
15.             {  
16.                 var positionalArguments = new PositionalArguments(b1, b2);
17.                 var gpFieldsWrapper = new GpFieldsWrapper(gp, positionalArguments: positionalArguments);  
18.                 if ((b1 || b2) == evaluateBoolean.Evaluate(gpFieldsWrapper))  
19.                 {  
20.                     fitnessScoreSoFar++;  
21.                 }  
22.             }  
23.         }  
24.  
25.         fitnessScoreSoFar /= 4;  
26.         return new FitnessStandard(fitnessScoreSoFar);  
27.     }  
28. }
```

### GpResultsStats
Each fitness type has an associated type to generate summary statistics for it. To define a `GpResultsStats` classs, you must create a class which extends the `GpResultsStatsBase`. The class must implement the method `GetDetailedSummary` which returns a statistical summary represented by an interface `IDetailedSummary`. 

Below is an implementation of the `GpResultsStatsStandard` class which is associated with the `FitnessStandard` class. Lines 1-5 define a constructor where you can pass in an enumerable of `FitnessStandard` instances. Lines 7-11 define a more general constructor which takes in an enumerable of `FitnessBase` instances. Line 8 attempts to cast the fitness values to the class `FitnessStandard`, and if it can't it throws an exception. If it is successful, it is passed to the base constructor defined in `GpResultsStatsBase`. It is necessary to define a more general constructor that dynamically checks if the input fitness values are of the correct type. This is a consequence of C#8 not supporting contra/covariant types.  I would use a more recent version of C#, but I want this library to be compatible with Unity. In addition, this is a consequence of the implementation specifics of the `GpResultsStatsBase` class. This will ideally be changed in a future version of `STGP-Sharp`. 

Lines 13-18 defines the method `GetDetailedSummary` which returns an instance of the struct `DetailedSummaryStandard`, defined in lines 19-34. `DetailedSummaryStandard` creates a fitness summary using the `SimpleStats` class which generates basic summary statistics.

```csharp
1.  public class GpResultsStatsStandard : GpResultsStatsBase  
2.  {  
3.      public GpResultsStatsStandard(IEnumerable<FitnessStandard>? fitnessValues) : base(fitnessValues)  
4.      {  
5.      }  
6.   
7.      public GpResultsStatsStandard(IEnumerable<FitnessBase> fitnessValues) : base(  
8.           fitnessValues?.Cast<FitnessStandard>()  
9.           ?? throw new Exception($"The fitness values passed in are not of type {typeof(GpResultsStatsStandard)}"))  
10.     {  
11.     }  
12.   
13.     public override IDetailedSummary GetDetailedSummary()  
14.     {  
15.         return new DetailedSummaryStandard(this.fitnessValues?.Cast<FitnessStandard>() ??  
17.                                            throw new NullReferenceException(nameof(this.fitnessValues)));  
18.     }  
  
19.     public struct DetailedSummaryStandard : IDetailedSummary  
20.     {  
21.          public SimpleStats.Summary fitnessScoreSummary;  
22.    
23.          public DetailedSummaryStandard(IEnumerable<FitnessStandard> fitnessValues)  
24.          {  
25.              this.fitnessScoreSummary = new SimpleStats(  
26.                      fitnessValues.Select(f => f.fitnessScore))  
27.                  .GetSummary();  
28.          }  
29.  
30.          public override string ToString()  
31.          {  
32.              return $"Total Fitness Summary: {this.fitnessScoreSummary.ToString()}";  
33.          }  
34.     }  
35.  }
```

### Defining a Population Initialization Method

To define a custom population initialization method, you must create a class which extends `PopulationInitializationMethod` and implements the method `GetPopulation<T>` where `T` either the type of the genome to be generated, or is the solution return type of the genomes to be generated. The method takes a `GpRunner` instance which implements the GP search algorithm, as well a `TimeoutInfo` instance which defines the method which keeps track of whether a breakout has been requested, or whether the given time limit has been reached. Because `GetPopulation<T>` provides asynchronous capabilities, it returns an awaitable list of individuals, ie. `Task<List<Individual>>`.  See [here](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-6.0) for information on asynchronous operations through the use of `Task`. 

Below is the implementation of randomized population initialization provided by `STGP-Sharp`. Line 5 defines the initially empty population. Line 6 create a loop which ends either when the size of `population` reaches the user-provided parameter `populationSize`, or the user-provided time limit has been reached. Then line 8-9, using the method `GpRunner.GenerateRandomTreeFromTypeOrReturnType<T>`, a random genome is generated of either type `T` or of return type `T`. This method takes in the maximum depth parameter provided by the user. `GpRunner.GenerateRandomTreeFromTypeOrReturnType<T>` takes the parameter `forceFullyGrow` which defines whether the random genome generated must be as large as possible. Line 10 then takes the random genome generated and creates an individual from it. Line 11 then evaluates the individual and assigns it a fitness score using the method `GpRunner.EvaluateFitnessOfIndividual`. Line 12 then adds this new individual to the population. Lastly, line 14 returns the generated population.

```csharp
1.  public class RandomPopulationInitialization : PopulationInitializationMethod
2.  {  
3.      public override async Task<List<Individual>> GetPopulation<T>(GpRunner gp, TimeoutInfo timeoutInfo)
4.      {   
5.          var population = new List<Individual>();  
6.          for (int i = 0; i < gp.populationParameters.populationSize && !timeoutInfo.ShouldTimeout; i++)  
7.          {  
8.              const bool forceFullyGrow = false;
9.              Node randomTree = gp.GenerateRandomTreeFromTypeOrReturnType<T>(gp.populationParameters.maxDepth, forceFullyGrow);  
10.             var ind = new Individual(randomTree);  
11.             await gp.EvaluateFitnessOfIndividual(ind);  
12.             population.Add(ind);  
13.         }            
14.         return population;  
15.     }  
16. }
```




