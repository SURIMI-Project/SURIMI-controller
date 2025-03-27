using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SurimiInitRequest;
using SurimiWorkflow;

namespace Poseidon.Services;

public class WorkflowService : Workflow.WorkflowBase
{
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(ILogger<WorkflowService> logger)
    {
        _logger = logger;
    }

    public override Task<Empty> Init(InitRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ExperimentId))
        {
            _logger.LogError("ExperimentId is required.");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ExperimentId is required."));
        }
        Console.WriteLine($"Poseidon Initializing experiment {request.ExperimentId}...");

        Task.Delay(1000).Wait();

        return Task.FromResult(new Empty());
    }
}
