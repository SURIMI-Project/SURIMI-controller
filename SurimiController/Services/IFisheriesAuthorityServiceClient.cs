using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IFisheriesAuthorityServiceClient : IWorkflowService
    {
        Task<GetRegulationsResponse> GetRegulationsAsync(GetRegulationsRequest getRegulationsRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateFishingActivityResponse> UpdateFishingActivityAsync(UpdateFishingActivityRequest updateFishingActivityRequest, DateTime current, CancellationToken cancellationToken);
        Task<CreateRegulationsResponse> CreateRegulationsAsync(CreateRegulationsRequest createRegulationsRequest, CancellationToken cancellationToken);
    }
}