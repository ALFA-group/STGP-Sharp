#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace STGP_Sharp.Fitness_and_Gp_Results
{
    public interface IFitnessFunction
    {
        public Type FitnessType { get; }
    }

    public interface ISync
    {
        public FitnessBase GetFitnessOfIndividual(GpRunner gp, Individual i);
    }

    public interface IAsync
    {
        public Task<FitnessBase> GetFitnessOfIndividualAsync(GpRunner gp, Individual i);
    }

    public interface ICoevolutionSync
    {
        public List<FitnessBase> GetFitnessOfPopulationUsingCoevolution(GpRunner gp,
            List<Individual> population);
    }

    public interface ICoevolutionAsync
    {
        public Task<List<FitnessBase>> GetFitnessOfPopulationUsingCoevolutionAsync(GpRunner gp,
            List<Individual> population);
    }
}