using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IWorkflowService
    {
        void AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default);  
        Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token);
        Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default);
        Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken);
    }
}