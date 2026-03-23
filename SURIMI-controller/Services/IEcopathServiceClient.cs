using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IEcopathServiceClient : IWorkflowService
    {
        Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken);
        Task<GetBiomassResponse> GetBiomassAsync(GetBiomassRequest getBiomassRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateEnvironmentVariablesResponse> UpdateEnvironmentVariablesAsync(UpdateEnvironmentVariablesRequest updateEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateRegulationsResponse> UpdateRegulationsAsync(UpdateRegulationsRequest updateRegulationsRequest, DateTime current, CancellationToken cancellationToken);
        Task<GetFishingActivityResponse> GetFishingActivityAsync(GetFishingActivityRequest getFishingActivityRequest, DateTime current, CancellationToken cancellationToken);
    }
}