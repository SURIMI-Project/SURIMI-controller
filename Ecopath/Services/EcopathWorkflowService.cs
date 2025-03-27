using Grpc.Core;
using Surimi;

namespace Ecopath.Services;

public class EcopathWorkflowService : WorkflowService.WorkflowServiceBase{
    private readonly ILogger<EcopathWorkflowService> _logger;
    public EcopathWorkflowService(ILogger<EcopathWorkflowService> logger)
    {
        _logger = logger;
    }

    public override Task<InitResponse> Init(InitRequest request, ServerCallContext context)
    {
        GrpcValidation.ArgumentNotNullOrEmpty(request.ExperimentId);
        Console.WriteLine($"Ecopath Initializing experiment {request.ExperimentId}...");

        Task.Delay(1000).Wait();
        return Task.FromResult(new InitResponse());
    }

    public override Task<UpdatePricesResponse> UpdatePrices(UpdatePricesRequest list, ServerCallContext context)
    {
        Console.WriteLine($"Updating prices for {list.Prices.Count} species...");

        // Simulate some processing delay
        Task.Delay(1000).Wait();

        return Task.FromResult(new UpdatePricesResponse());
    }
}
