using EwECore;
using Grpc.Core;
using Grpc.Surimi;

namespace Ecopath.Services;

public class EcopathWorkflowService : WorkflowService.WorkflowServiceBase
{
    private readonly ILogger<EcopathWorkflowService> _logger;

    public EcopathWorkflowService(ILogger<EcopathWorkflowService> logger)
    {
        _logger = logger;
    }

    public override Task<InitResponse> Init(InitRequest request, ServerCallContext context)
    {
        GrpcValidation.ArgumentNotNullOrEmpty(request.ExperimentId);
        Console.WriteLine($"Ecopath Initializing experiment {request.ExperimentId}...");

        var core = new cCore();     // It would be better to inject this dependency. But I couldn't find a way to do it. Maybe with an interface?
        string ModelName = "Anchovy Bay Spatial.eiixml";
        if (!core.LoadModel(ModelName))
        {
            Console.WriteLine("Couldn't load EwE model {0}", ModelName);
        }

        Task.Delay(1000).Wait();
        return Task.FromResult(new InitResponse());
    }

    public override Task<UpdatePricesResponse> UpdatePrices(UpdatePricesRequest list, ServerCallContext context)
    {
        Console.WriteLine($"Updating prices for {list.Prices.Count} species...");

        // Simulate some processing delay
        //Task.Delay(1000).Wait();

        return Task.FromResult(new UpdatePricesResponse());
    }

    public override Task<SimulateStepResponse> SimulateStep(SimulateStepRequest req, ServerCallContext context)
    {
        Console.WriteLine($"Simulate step for year {req.Year} and month {req.Month}...");

        // Simulate some processing delay
        //Task.Delay(1000).Wait();

        return Task.FromResult(new SimulateStepResponse());
    }
}
