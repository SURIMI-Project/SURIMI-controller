
using Google.Protobuf.WellKnownTypes;
using Grpc.Surimi;

namespace SurimiController.Services
{

    public interface ISimulationManager
    {
        Task RunSimulationAsync(string simulationId, CancellationToken externalToken);
        void CancelSimulation(string simulationId);
        Task InitSimulationAsync(string simulationId, string scenarioId, Timestamp startDateTime, string stepSize, string duration);
        Task<GetAllSimulationsResponse> GetAllSimulationsAsync(CancellationToken cancellationToken);
    }

}
