using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IOutputCreatorServiceClient : IExperimentService
    {
        public Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatistics(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatistics(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateFishingActivityStatisticsResponse> UpdateFishingActivityStatistics(UpdateFishingActivityStatisticsRequest updateFishingActivityStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateSalesStatisticsResponse> UpdateSalesStatistics(UpdateSalesStatisticsRequest updateSalesStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateSpeciesPriceStatisticsResponse> UpdateSpeciesPriceStatistics(UpdateSpeciesPriceStatisticsRequest updateSpeciesPriceStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
    }
}