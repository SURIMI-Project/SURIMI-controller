using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SurimiWorkflow;
using SurimiSpeciesPrice;
using SurimiInitRequest;

namespace Ecopath.Services;

public class WorkflowService : Workflow.WorkflowBase
{
    private readonly ILogger<WorkflowService> _logger;
    public WorkflowService(ILogger<WorkflowService> logger)
    {
        _logger = logger;
    }

    public override Task<Empty> Init(InitRequest request, ServerCallContext context)
    {
        GrpcValidation.ArgumentNotNullOrEmpty(request.ExperimentId);
        Console.WriteLine($"Ecopath Initializing experiment {request.ExperimentId}...");

        Task.Delay(1000).Wait();
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> UpdatePrices(SpeciesPrices list, ServerCallContext context)
    {
        Console.WriteLine($"Updating prices for {list.Prices.Count} species...");

        // Simulate some processing delay
        Task.Delay(1000).Wait();

        return Task.FromResult(new Empty());
    }
}
