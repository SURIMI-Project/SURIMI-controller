using Grpc.Surimi;

namespace SURIMI_controller.Models
{
    public class CatchDispositionEventArgs : ExperimentEventArgs
    {
        public CatchDispositionSummary CatchDispositionSummary { get; set; }

        public CatchDispositionEventArgs(CatchDispositionSummary catchDispositionSummary, ExperimentEventArgs experimentEventArgs)
            : base(experimentEventArgs)
        {
            CatchDispositionSummary = catchDispositionSummary;
        }
    }
}
