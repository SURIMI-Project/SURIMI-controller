using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IMarketService 
    {
        Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, CancellationToken cancellationToken);
        Task<GetSpeciesPricesResponse> GetSpeciesPricesAsync(GetSpeciesPricesRequest getSpeciesPricesRequest, CancellationToken cancellationToken);
        Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, CancellationToken cancellationToken);
        Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, CancellationToken cancellationToken);
    }
}