using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;


namespace SurimiController.Services;

public class SurimiWorkflowService : WorkflowService.WorkflowServiceBase
{
    private readonly ILogger<SurimiWorkflowService> _logger;

    private readonly WorkflowService.WorkflowServiceClient _ecopathWorkflowClient;
    private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;

    public SurimiWorkflowService(ILogger<SurimiWorkflowService> logger, GrpcClientFactory clientFactory)
    {
        _logger = logger;
        _ecopathWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow");
        _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
    }   


}
