#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Linq;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp
{
    public class ReturnTypeSpecification : IEquatable<ReturnTypeSpecification>
    {
        public readonly IEnumerable<FilterAttribute> filters;
        public readonly Type returnType;

        public ReturnTypeSpecification(Type returnType, IEnumerable<FilterAttribute>? filters)
        {
            this.returnType = returnType;
            this.filters = filters ?? Array.Empty<FilterAttribute>();
        }

        public bool Equals(ReturnTypeSpecification? spec)
        {
            if (null == spec)
            {
                return false;
            }

            bool sameReturnType = this.returnType == spec.returnType;
            bool sameFilters = this.filters.SequenceEqual(spec.filters);
            return sameReturnType && sameFilters;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as ReturnTypeSpecification ??
                               throw new InvalidOperationException());
        }

        public override int GetHashCode()
        {
            var allHashes = new List<int> { this.returnType.GetHashCode() };
            allHashes.AddRange(this.filters.Select(f => f.GetHashCode()));
            return GeneralCSharpUtilities.CombineHashCodes(allHashes);
        }
    }
}