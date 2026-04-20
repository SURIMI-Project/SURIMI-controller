using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface ISimulationManager
    {
        Task CancelSimulationAsync(string simulationId);
        Task RunSimulationAsync(string simulationId, string experimentId, string scenarioId, DateTime? endDateTime, Simulation simulation, RegulationDefinitionsSummary regulationsSummary, CancellationToken cancellationToken);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken);
    }
}
