using Grpc.Surimi;

namespace SURIMI_controller.Models
{
    public class Experiment
    {
        /// <summary>
        /// The simulation IDs registered for this experiment.
        /// </summary>
        public IReadOnlyList<string> SimulationIds { get; set; } = [];

        /// <summary>
        /// Biomass summaries per simulation, grouped by date (time component stripped).
        /// Entries are removed once aggregated.
        /// </summary>
        public Dictionary<DateTime, Dictionary<string, BiomassSummary?>> BiomassSummary { get; set; } = new();
        /// <summary>
        /// Biomass summaries per simulation, grouped by date (time component stripped).
        /// Entries are removed once aggregated.
        /// </summary>
        public Dictionary<DateTime, Dictionary<string, CatchDispositionSummary?>> CatchDispositionSummary { get; set; } = new();

        /// <summary>Tracks which simulations have fired SimulateStep, grouped by date.</summary>
        public Dictionary<DateTime, Dictionary<string, bool?>> SimulateStepCalled { get; set; } = new();

        /// <summary>Tracks which simulations have fired SimulationFinalised, grouped by date.</summary>
        public Dictionary<DateTime, Dictionary<string, bool?>> SimulationFinalisedCalled { get; set; } = new();

        /// <summary>Tracks which simulations have fired SimulationCancelled, grouped by date.</summary>
        public Dictionary<DateTime, Dictionary<string, bool?>> SimulationCancelledCalled { get; set; } = new();

        /// <summary>
        /// Fishing activity summaries per simulation, grouped by date (time component stripped).
        /// Entries are removed once aggregated.
        /// </summary>
        public Dictionary<DateTime, Dictionary<string, FishingActivitySummary?>> FishingActivitySummary { get; set; } = new();

        /// <summary>
        /// Sales summaries per simulation, grouped by date (time component stripped).
        /// Entries are removed once aggregated.
        /// </summary>
        public Dictionary<DateTime, Dictionary<string, SalesSummary?>> SalesSummary { get; set; } = new();

        /// <summary>
        /// Species price summaries per simulation, grouped by date (time component stripped).
        /// Entries are removed once aggregated.
        /// </summary>
        public Dictionary<DateTime, Dictionary<string, SpeciesPriceSummary?>> SpeciesPriceSummary { get; set; } = new();
        public Task? Task { get; set; }
        public CancellationTokenSource? Cts { get; set; }
    }
}
