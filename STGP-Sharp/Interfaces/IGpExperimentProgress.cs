#nullable enable
namespace STGP_Sharp.Interfaces
{
    public interface IGpExperimentProgress
    {
        public int GenerationsInRunCompleted { get; set; }
        public int GenerationsInRunCount { get; set; }
        public string? Status { get; set; }
    }
}