using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IEcopathServiceClient : IWorkflowService
    {
        Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken);
        Task<GetBiomassResponse> GetBiomassAsync(GetBiomassRequest getBiomassRequest, DateTime current, CancellationToken cancellationToken);
        Task<string> GetHostValueAsync();
    }
}