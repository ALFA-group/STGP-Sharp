using System;
using STGP_Sharp;
using STGP_Sharp.Fitness_and_Gp_Results;

namespace STGP_Sharp.STGP_Sharp.Tests
{
    public class TestFitnessFunction : IFitnessFunction, ISync
    {
        public Type FitnessType { get; } = typeof(FitnessStandard);
        
        FitnessBase ISync.GetFitnessOfIndividual(GpRunner gp, Individual i)
        {
            return new FitnessStandard(0);
        }
    }
}