
using Google.Protobuf.WellKnownTypes;
using Grpc.Surimi;

namespace SurimiController.Services
{

    public interface ISimulationManager
    {
        Task RunSimulationAsync(string simulationId, CancellationToken externalToken);
        Task CancelSimulationAsync(string simulationId);
        Task InitSimulationAsync(string simulationId, string scenarioId, DateTime? endDateTime, Simulation simulation);
        Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken);
    }

}
