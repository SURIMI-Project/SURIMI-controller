using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IMarketServiceClient : IWorkflowService
    {
        Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken);
        Task<GetSpeciesPricesResponse> GetSpeciesPricesAsync(GetSpeciesPricesRequest getSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken);
    }
}