using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IWorkflowService
    {
        void AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default(CancellationToken));
        Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token);
        Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default(CancellationToken));
        Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, CancellationToken cancellationToken);
    }
}