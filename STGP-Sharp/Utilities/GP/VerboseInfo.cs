// ReSharper disable NotAccessedField.Global

namespace STGP_Sharp.Utilities.GP
{
    public class VerboseInfo
    {
        private bool _verbose;

        public int numberOfTimesCrossoverSkipped;

        public int numberOfTimesCrossoverSwappedEquivalentNode;

        public int numberOfTimesCrossoverWasTooDeep;

        public int numberOfTimesMutationCreatedEquivalentNode;

        public int numberOfTimesMutationSkipped;

        public int numberOfTimesNoLegalCrossoverPoints;

        public static implicit operator bool(VerboseInfo v)
        {
            return v._verbose;
        }

        public static implicit operator VerboseInfo(bool v)
        {
            return new VerboseInfo { _verbose = v };
        }

        public void ResetCountInfo()
        {
            this.numberOfTimesCrossoverWasTooDeep = 0;
            this.numberOfTimesCrossoverSkipped = 0;
            this.numberOfTimesMutationCreatedEquivalentNode = 0;
            this.numberOfTimesMutationSkipped = 0;
            this.numberOfTimesCrossoverSwappedEquivalentNode = 0;
            this.numberOfTimesNoLegalCrossoverPoints = 0;
        }
    }
}