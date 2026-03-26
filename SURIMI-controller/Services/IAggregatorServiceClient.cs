using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IAggregatorServiceClient : IWorkflowService
    {
        Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken);
    }
}