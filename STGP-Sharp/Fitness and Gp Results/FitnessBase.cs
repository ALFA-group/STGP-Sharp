#nullable enable

#region

using System;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp.Fitness_and_Gp_Results
{
    public abstract class FitnessBase : IComparable<FitnessBase>, ICanDeepCopy<FitnessBase>
    {
        // The goal is to be able to write Fitness and a Gp Results Stats classes without
        // having to explicitly define a mapping from Fitness Type to Fitness Stats Type.
        // This makes it easier to write for the end-user, so that they don't have to worry
        // about as many things. Ideally, I would make this an abstract static property,
        // but I want this project to be compatible with Unity, and as of now, Unity
        // does not support any C# version which allows for this.
        // ReSharper disable once UnusedMember.Global
        public static Type? GpResultsStatsType => throw new NotImplementedException();

        public abstract FitnessBase DeepCopy();

        public abstract int CompareTo(FitnessBase? other);

        public abstract override string ToString();

        public abstract bool LessThan(FitnessBase f);
        public abstract bool GreaterThan(FitnessBase f);
        public abstract bool Equals(FitnessBase? other);

        public abstract override int GetHashCode();

        // Leave it up to the user to decide if they want to allow comparison between different fitness types.
        protected static void ThrowExceptionIfInvalidFitnessForComparison<TFitnessType>(FitnessBase? f,
            out TFitnessType casted) where TFitnessType : FitnessBase
        {
            if (f is not TFitnessType type)
            {
                throw new Exception($"Can not compare with fitness of type {f?.GetType()} with {typeof(TFitnessType)}");
            }

            casted = type;
        }
    }
}