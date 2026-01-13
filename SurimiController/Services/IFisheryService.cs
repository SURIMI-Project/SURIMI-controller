using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IFisheryService 
    {
        Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, CancellationToken cancellationToken);
        Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, CancellationToken cancellationToken);

    }
}