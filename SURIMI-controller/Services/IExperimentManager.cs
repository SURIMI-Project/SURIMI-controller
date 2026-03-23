using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IExperimentManager
    {
        Task CancelExperimentAsync(string experimentId, CancellationToken cancellationToken);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken);
        Task InitialiseExperiment(InitialiseExperimentRequest request, Simulation simulation, CancellationToken cancellationToken);
        Task RunExperimentAsync(string experimentId, CancellationToken cancellationToken);
    }
}