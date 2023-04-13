#nullable enable

#region

using System;
using STGP_Sharp.GpBuildingBlockTypes;

#endregion

namespace STGP_Sharp.Fitness_and_Gp_Results.Sample_Fitness_Functions
{
    public class EquivalentToOr : IFitnessFunction, ISync
    {
        public Type FitnessType { get; } = typeof(FitnessStandard);

        public FitnessBase GetFitnessOfIndividual(GpRunner gp, Individual i)
        {
            if (i.genome is not GpBuildingBlock<bool> evaluateBoolean)
            {
                throw new Exception("Genome does not evaluate to boolean");
            }

            float fitnessScoreSoFar = 0;
            foreach (bool b1 in new[] { true, false })
            {
                foreach (bool b2 in new[] { true, false })
                {
                    var positionalArguments = new PositionalArguments(b1, b2);
                    var gpFieldsWrapper = new GpFieldsWrapper(gp, positionalArguments: positionalArguments);
                    if ((b1 || b2) == evaluateBoolean.Evaluate(gpFieldsWrapper))
                    {
                        fitnessScoreSoFar++;
                    }
                }
            }

            fitnessScoreSoFar /= 4;
            return new FitnessStandard(fitnessScoreSoFar);
        }
    }
}