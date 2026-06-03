using Grpc.Surimi;

namespace SURIMI_controller.Models
{
    public class SpeciesPriceEventArgs : ExperimentEventArgs
    {
        public SpeciesPriceSummary SpeciesPriceSummary { get; set; }

        public SpeciesPriceEventArgs(SpeciesPriceSummary speciesPriceSummary, ExperimentEventArgs experimentEventArgs)
            : base(experimentEventArgs)
        {
            SpeciesPriceSummary = speciesPriceSummary;
        }
    }
}
