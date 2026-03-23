using Grpc.Surimi;

namespace SURIMI_controller.Services
{

    public interface ISimulationManager
    {
        Task RunSimulationAsync(string simulationId, CancellationToken externalToken);
        Task CancelSimulationAsync(string simulationId);
        Task InitSimulationAsync(string simulationId, string experimentId, string scenarioId, DateTime? endDateTime, Simulation simulation, RegulationDefinitionsSummary regulationsSummary, CancellationToken cancellationToken);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken);
    }

}
