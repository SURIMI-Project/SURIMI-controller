using Grpc.Surimi;
using SURIMI_controller.Models;

namespace SURIMI_controller.Services
{
    public interface ICmsyServiceClient : IExperimentService
    {
        Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatistics(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatistics(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken);
    }
}