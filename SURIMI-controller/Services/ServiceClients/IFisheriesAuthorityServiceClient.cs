using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IFisheriesAuthorityServiceClient : ISimulationService
    {
        Task<GetRegulationsResponse> GetRegulationsAsync(GetRegulationsRequest getRegulationsRequest, DateTime current, CancellationToken cancellationToken);
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken);
        Task<CreateRegulationsResponse> CreateRegulationsAsync(CreateRegulationsRequest createRegulationsRequest, CancellationToken cancellationToken);
        Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassTotalRequest, DateTime current, CancellationToken cancellationToken);
    }
}