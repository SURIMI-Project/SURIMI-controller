using Grpc.Surimi;

namespace SURIMI_controller.Models
{
    public class FishingActivityEventArgs : ExperimentEventArgs
    {
        public FishingActivitySummary FishingActivitySummary { get; set; }

        public FishingActivityEventArgs(FishingActivitySummary fishingActivitySummary, ExperimentEventArgs experimentEventArgs)
            : base(experimentEventArgs)
        {
            FishingActivitySummary = fishingActivitySummary;
        }
    }
}
