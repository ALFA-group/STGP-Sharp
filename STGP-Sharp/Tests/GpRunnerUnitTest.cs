using System;
using System.Threading;
#nullable enable

namespace STGP_Sharp.STGP_Sharp.Tests
{
    public abstract class GpRunnerUnitTest
    {
        private readonly GpPopulationParameters _gpPopulationParameters;
        private readonly int _randomSeed;
        private readonly Type _solutionReturnType;

        protected GpRunnerUnitTest(
            GpPopulationParameters gpPopulationParameters,
            Type solutionReturnType,
            int randomSeed = 0)
        {
            this._gpPopulationParameters = gpPopulationParameters;
            this._solutionReturnType = solutionReturnType;
            this._randomSeed = randomSeed;
        }

        protected GpRunner GetGpRunner(bool verbose = false)
        {
            var fitnessFunction = new TestFitnessFunction();
            var timeoutInfo = new TimeoutInfo()
                { cancelTokenSource = new CancellationTokenSource(), ignoreGenerationsUseTimeout = false };
            return new GpRunner(
                fitnessFunction, this._gpPopulationParameters, this._solutionReturnType,
                timeoutInfo: timeoutInfo,
                randomSeed: this._randomSeed, verbose: verbose
            );
        }
    }
}