#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace STGP_Sharp.Utilities.GP
{
    public static class GpUtility
    {
        private static readonly Dictionary<Type, string> NiceNamesDictionary = new Dictionary<Type, string>
        {
            { typeof(float), "Float" },
            { typeof(int), "Integer" }
        };
        

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public static List<Individual> SortedByFitness(this IEnumerable<Individual> population)
        {
            return population.ToList().SortedByFitness();
        }

        public static List<Individual> SortedByFitness(this List<Individual> population)
        {
            return population.OrderByDescending(i => i.fitness).ToList();
        }
        
        /// <summary>
        /// Returns a "nice" name for a given type.
        /// </summary>
        /// <remarks>From http://stackoverflow.com/questions/401681/how-can-i-get-the-correct-text-definition-of-a-generic-type-using-reflection</remarks>
        /// <param name="type">The type to get a nice name of.</param>
        /// <returns></returns>
        public static string GetNiceName(Type type)
        {
            if (NiceNamesDictionary.TryGetValue(type, out var niceName))
            {
                return niceName;
            }
            
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var builder = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            builder.AppendFormat(name.Substring(0, index));
            builder.Append('<');
            var first = true;
            foreach (var arg in type.GetGenericArguments())
            {
                if (!first)
                {
                    builder.Append(',');
                }
                builder.Append(GetNiceName(arg));
                first = false;
            }
            builder.Append('>');
            return builder.ToString();
        }
        
    }
}