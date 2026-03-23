using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IMarketServiceClient : IWorkflowService
    {
        Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken);
        Task<GetSpeciesPricesResponse> GetSpeciesPricesAsync(GetSpeciesPricesRequest getSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken);
    }
}