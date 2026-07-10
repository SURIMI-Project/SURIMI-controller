using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IOutputCreatorServiceClient : IExperimentService
    {
        public Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatisticsAsync(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatisticsAsync(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateFishingActivityStatisticsResponse> UpdateFishingActivityStatisticsAsync(UpdateFishingActivityStatisticsRequest updateFishingActivityStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateSalesStatisticsResponse> UpdateSalesStatisticsAsync(UpdateSalesStatisticsRequest updateSalesStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateSpeciesPriceStatisticsResponse> UpdateSpeciesPriceStatisticsAsync(UpdateSpeciesPriceStatisticsRequest updateSpeciesPriceStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        public Task<UpdateStockAssessmentResponse> UpdateStockAssessmentAsync(UpdateStockAssessmentRequest updateStockAssessmentRequest, string experimentId, CancellationToken cancellationToken);
    }
}