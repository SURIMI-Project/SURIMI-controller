using Grpc.Surimi;
using SURIMI_controller.Models;

namespace SURIMI_controller.Services
{
    public interface ICmsyServiceClient : IExperimentService
    {
        Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatisticsAsync(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatisticsAsync(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        Task<GetStockAssessmentResponse> GetStockAssessmentAsync(GetStockAssessmentRequest getStockAssessmentRequest, CancellationToken cancellationToken = default);
    }
}