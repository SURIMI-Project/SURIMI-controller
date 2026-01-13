using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IEcologyService
    {
        Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, CancellationToken cancellationToken);
    }
}