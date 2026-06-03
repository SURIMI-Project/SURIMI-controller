using Grpc.Surimi;

namespace SURIMI_controller.Models
{
    public class BiomassEventArgs : ExperimentEventArgs
    {
        public BiomassSummary BiomassSummary { get; set; }

        public BiomassEventArgs(BiomassSummary biomassSummary, ExperimentEventArgs experimentEventArgs)
            : base(experimentEventArgs)
        {
            BiomassSummary = biomassSummary;
        }
    }
}
