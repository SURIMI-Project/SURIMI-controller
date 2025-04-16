using EwECore;
using Grpc.Core;
using Grpc.Surimi;

namespace Ecopath.Services;

public class EcopathWorkflowService : WorkflowService.WorkflowServiceBase
{
    private readonly ILogger<EcopathWorkflowService> _logger;
    private readonly cCore _core;

    public EcopathWorkflowService(ILogger<EcopathWorkflowService> logger)
    {
        _logger = logger;
        _core = new cCore();
    }

    public override Task<InitResponse> Init(InitRequest request, ServerCallContext context)
    {
        GrpcValidation.ArgumentNotNullOrEmpty(request.ScenarioId);
        Console.WriteLine($"Ecopath Initializing scenario {request.ScenarioId}...");
        if (request.ScenarioId.ToLower().Equals("testecopath"))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ScenarioId cannot be 'testecopath'"));
        }
        if (request.ScenarioId.ToLower().Equals("testecopath2"))
        {
            string tst = request.ScenarioId.Substring(4, 12);       // Will throw exception
        }
        return Task.FromResult(new InitResponse());
    }

    public override Task<UpdatePricesResponse> UpdatePrices(UpdatePricesRequest list, ServerCallContext context)
    {
        Console.WriteLine($"Updating prices for {list.Prices.Count} species...");

        // Simulate some processing delay
        // test Test
        //Task.Delay(1000).Wait();

        return Task.FromResult(new UpdatePricesResponse());
    }

    public override Task<SimulateStepResponse> SimulateStep(SimulateStepRequest req, ServerCallContext context)
    {
        Console.WriteLine($"Simulate step for simulation {req.SimulationId}");

        // Simulate some processing delay
        //Task.Delay(1000).Wait();

        return Task.FromResult(new SimulateStepResponse());
    }
}