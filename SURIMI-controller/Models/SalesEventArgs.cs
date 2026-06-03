using Grpc.Surimi;

namespace SURIMI_controller.Models
{
    public class SalesEventArgs : ExperimentEventArgs
    {
        public SalesSummary SalesSummary { get; set; }

        public SalesEventArgs(SalesSummary salesSummary, ExperimentEventArgs experimentEventArgs)
            : base(experimentEventArgs)
        {
            SalesSummary = salesSummary;
        }
    }
}
