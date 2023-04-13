#region

using System;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class RandomTreeConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Parameter,
        Inherited = false)]
    public class FilterAttribute : Attribute
    {
        public bool IsSatisfiedBy(Type candidate)
        {
            return candidate.HasAttribute(this.GetType());
        }
    }
}