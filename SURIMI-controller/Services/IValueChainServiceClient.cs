using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IValueChainServiceClient : IWorkflowService
    {
        Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken);
    }
}
