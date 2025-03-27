using Grpc.Core;
using Grpc.Surimi;

namespace Poseidon.Services;

public class PoseidonWorkflowService : WorkflowService.WorkflowServiceBase
{
    private readonly ILogger<PoseidonWorkflowService> _logger;

    public PoseidonWorkflowService(ILogger<PoseidonWorkflowService> logger)
    {
        _logger = logger;
    }

    public override Task<InitResponse> Init(InitRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ExperimentId))
        {
            _logger.LogError("ExperimentId is required.");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ExperimentId is required."));
        }
        Console.WriteLine($"Poseidon Initializing experiment {request.ExperimentId}...");

        Task.Delay(1000).Wait();

        return Task.FromResult(new InitResponse());
    }
}
