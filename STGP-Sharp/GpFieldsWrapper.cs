#nullable enable

#region

using System;

#endregion

namespace STGP_Sharp
{
    /// <summary>
    ///     Wrapper to hold relevant GP fields for genome and fitness evaluation.
    /// </summary>
    public class GpFieldsWrapper
    {
        public readonly NamedArguments? namedArguments;
        public readonly GpPopulationParameters populationParameters;
        public readonly PositionalArguments? positionalArguments;
        public readonly Random rand;
        public readonly TimeoutInfo timeoutInfo;
        public readonly bool verbose;

        public GpFieldsWrapper(GpRunner gp) : this(gp, gp.namedArguments, gp.positionalArguments)
        {
        }


        public GpFieldsWrapper(GpRunner gp,
            NamedArguments? namedArguments = null,
            PositionalArguments? positionalArguments = null)
        {
            this.rand = gp.rand;
            this.timeoutInfo = gp.timeoutInfo;
            this.populationParameters = gp.populationParameters;
            this.verbose = gp.verbose;
            this.namedArguments = namedArguments;
            this.positionalArguments = positionalArguments;
        }
    }
}