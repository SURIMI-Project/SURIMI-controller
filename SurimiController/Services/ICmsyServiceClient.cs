using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface ICmsyServiceClient 
    {
        void AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default(CancellationToken));
        Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token);
        Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default(CancellationToken));
        Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
    }
}