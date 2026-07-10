using Grpc.Core;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IExperimentService
    {
        AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initialisationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken token);  
        Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token);
        Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken token);
        Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token);
    }
}