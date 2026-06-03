using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IValueChainServiceClient : IExperimentService
    {
        Task<UpdateSalesStatisticsResponse> UpdateSalesStatisticsAsync(UpdateSalesStatisticsRequest updateSalesStatisticsRequest, DateTime current, CancellationToken cancellationToken);

    }
}
