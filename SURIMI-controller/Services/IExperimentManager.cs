using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IExperimentManager
    {
        Task CancelExperimentAsync(string experimentId, CancellationToken cancellationToken);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken);
        Task SubmitExperiment(SubmitExperimentRequest request, Simulation simulation, CancellationToken cancellationToken);
    }
}