using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IValueChainServiceClient : IWorkflowService
    {
        Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken);
    }
}
