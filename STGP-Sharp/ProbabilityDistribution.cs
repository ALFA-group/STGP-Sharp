#nullable enable

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp
{
    public class TypeProbability
    {
        public double probability;

        public Type type;

        public TypeProbability(Type type, double prob)
        {
            this.type = type;
            this.probability = prob;
        }

        public TypeProbability()
        {
            this.type = ProbabilityDistribution.ValidTypes.First();
            this.probability = 1;
        }


        public string TypeToString
        {
            get => this.type.Name;
            set { this.type = ProbabilityDistribution.ValidTypes.First(t => t.Name == value); }
        }
    }

    public class ProbabilityDistribution
    {
        public static readonly IEnumerable<Type> ValidTypes =
            GpRunner.GetSubclassesOfGpBuildingBlock(true);

        public readonly List<TypeProbability> distribution;

        public ProbabilityDistribution(IEnumerable<TypeProbability> distribution)
        {
            this.distribution = distribution.ToList();

            this.AddDefaultUnspecifiedTypeProbabilities();
        }

        public ProbabilityDistribution(List<Type> types)
        {
            this.distribution = new List<TypeProbability>(types.Count);

            foreach (Type type in types)
            {
                this.distribution.Add(new TypeProbability(type, 1));
            }

            this.AddDefaultUnspecifiedTypeProbabilities();
        }

        public IEnumerable<Type> GetTypesWithProbabilityGreaterThanZero()
        {
            return this.distribution.Where(tp => tp.probability > 0).Select(tp => tp.type);
        }

        public double? GetProbabilityOfType(Type t)
        {
            return this.distribution.FirstOrDefault(tp => tp.type == t)?.probability;
        }

        public void AddDefaultUnspecifiedTypeProbabilities()
        {
            List<Type> typesOnly = this.distribution.Select(tp => tp.type).ToList();
            foreach (Type type in ValidTypes)
            {
                if (!typesOnly.Contains(type))
                {
                    this.distribution.Add(new TypeProbability(type, 0));
                }
            }
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static ProbabilityDistribution? Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<ProbabilityDistribution>(json);
        }

        public static ProbabilityDistribution GetProbabilityDistributionFromFile(string file)
        {
            file = GeneralCSharpUtilities.GetRelativePath(file);
            return Deserialize(File.ReadAllText(file)) ??
                   throw new Exception($"File {file} does not contain a probability distribution.");
        }

        public void WriteToFile(string file)
        {
            file = GeneralCSharpUtilities.GetRelativePath(file);
            File.WriteAllText(file, this.Serialize());
        }

        public Dictionary<Type, double> ToDictionary()
        {
            return this.distribution.ToDictionary(
                tp => tp.type,
                tp => tp.probability
            );
        }

        public static ProbabilityDistribution FromDictionary(Dictionary<Type, double> d)
        {
            var typeProbabilities = new List<TypeProbability>(d.Count);
            typeProbabilities.AddRange(
                d.Select(kvp =>
                    new TypeProbability(kvp.Key, kvp.Value)));

            return new ProbabilityDistribution(typeProbabilities);
        }
    }
}