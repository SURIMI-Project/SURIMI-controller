using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using SurimiInitRequest;
using SurimiWorkflow;


namespace SurimiController.Services;

public class WorkflowService : Workflow.WorkflowBase
{
    private readonly ILogger<WorkflowService> _logger;

    private readonly Workflow.WorkflowClient _ecopathWorkflowClient;
    private readonly Workflow.WorkflowClient _poseidonWorkflowClient;

    public WorkflowService(ILogger<WorkflowService> logger, GrpcClientFactory clientFactory)
    {
        _logger = logger;
        _ecopathWorkflowClient = clientFactory.CreateClient<Workflow.WorkflowClient>("EcopathWorkflow");
        _poseidonWorkflowClient = clientFactory.CreateClient<Workflow.WorkflowClient>("PoseidonWorkflow");
    }

    public override async Task<Empty> Init(InitRequest init, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"Initializing experiment {init.ExperimentId}...");

            var ecopathReply = _ecopathWorkflowClient.InitAsync(new InitRequest { ExperimentId = init.ExperimentId });

            var poseidonReply = _poseidonWorkflowClient.InitAsync(new InitRequest { ExperimentId = init.ExperimentId });

            await ecopathReply;
            await poseidonReply;
            return new Empty();
        }
        catch (Exception ex)
        {

            throw;
        }
    }
}
