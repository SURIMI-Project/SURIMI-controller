using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IExperimentManager
    {
        Task CancelExperimentAsync(string experimentId, CancellationToken cancellationToken);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken);
        Task InitialiseExperiment(InitialiseExperimentRequest request, Simulation simulation, CancellationToken cancellationToken);
        Task RunExperimentAsync(string experimentId, CancellationToken cancellationToken);
    }
}