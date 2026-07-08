using Grpc.Surimi;
using SURIMI_controller.Models;

namespace SURIMI_controller.Services
{
    public interface ISimulationManager
    {
        event EventHandler<BiomassEventArgs> BiomassUpdated;
        event EventHandler<CatchDispositionEventArgs> CatchDispositionUpdated;
        event EventHandler<SalesEventArgs> SalesUpdated;
        event EventHandler<FishingActivityEventArgs> FishingActivityUpdated;
        event EventHandler<ExperimentEventArgs> SimulateStep;
        event EventHandler<ExperimentEventArgs>? SimulationFinalised;
        event EventHandler<ExperimentEventArgs>? SimulationCancelled;
        event EventHandler<SpeciesPriceEventArgs>? SpeciesPriceUpdated;

        Task CancelSimulationAsync(string simulationId);
        Task InitSimulationAsync(string simulationId, string experimentId, string scenarioName, DateTime? endDateTime, Grpc.Surimi.Simulation simulation, CancellationToken cancellationToken);
        Task RunSimulationAsync(string simulationId, string scenarioName, RegulationDefinitionsSummary regulationsSummary, CancellationToken cancellationToken);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken);
    }
}
