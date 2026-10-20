using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IExperimentManager
    {
        Task CancelExperimentAsync(string experimentId, CancellationToken cancellationToken);
        Task<GetSimulationStatusesResponse> GetSimulationStatusesAsync(IEnumerable<string> experimentIds, CancellationToken cancellationToken);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatusesAsync(CancellationToken cancellationToken);
        Task SubmitExperiment(SubmitExperimentRequest request, Simulation simulation, CancellationToken cancellationToken);
    }
}