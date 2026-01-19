using Grpc.Core;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IWorkflowService
    {
        AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default);  
        Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token);
        Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default);
        Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken);
    }
}