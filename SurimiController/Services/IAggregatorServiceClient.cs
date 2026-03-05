using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IAggregatorServiceClient : IWorkflowService
    {
        Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, CancellationToken cancellationToken);
        Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken);
    }
}