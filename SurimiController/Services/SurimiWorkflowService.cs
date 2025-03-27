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

    public override async Task<InitResponse> Init(InitRequest init, ServerCallContext context)
    {
        Console.WriteLine($"Initializing experiment {init.ExperimentId}...");

        var ecopathReply = _ecopathWorkflowClient.InitAsync(new InitRequest { ExperimentId = init.ExperimentId });

        var poseidonReply = _poseidonWorkflowClient.InitAsync(new InitRequest { ExperimentId = init.ExperimentId });

        await ecopathReply;
        await poseidonReply;
        return new InitResponse();
    }
}
