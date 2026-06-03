using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IFisheriesAuthorityServiceClient : ISimulationService
    {
        Task<GetRegulationsResponse> GetRegulationsAsync(GetRegulationsRequest getRegulationsRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateFishingActivityResponse> UpdateFishingActivityAsync(UpdateFishingActivityRequest updateFishingActivityRequest, DateTime current, CancellationToken cancellationToken);
        Task<CreateRegulationsResponse> CreateRegulationsAsync(CreateRegulationsRequest createRegulationsRequest, CancellationToken cancellationToken);
    }
}