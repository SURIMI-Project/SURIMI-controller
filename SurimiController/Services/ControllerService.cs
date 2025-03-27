using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using SurimiSpeciesPrice;
using SurimiWorkflow;

namespace SurimiController.Services
{
    public class ControllerService(GrpcClientFactory clientFactory) : Controller.ControllerBase
    {
        private readonly Workflow.WorkflowClient _ecopathWorkflowClient = clientFactory.CreateClient<Workflow.WorkflowClient>("EcopathWorkflow");
        private readonly Workflow.WorkflowClient _poseidonWorkflowClient = clientFactory.CreateClient<Workflow.WorkflowClient>("PoseidonWorkflow");

        public override async Task<Empty> RunSimulation(Empty request, ServerCallContext context)
        {
            Console.WriteLine($"Running simulation...");

            //var marketClient = GetClient<Market.MarketClient>("MARKET_URL");

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"Processing step {i}...");

                // retrieve marketprice
                //var marketReply = await marketClient.GetPricesAsync(new Empty());
                var marketReply = new SpeciesPrices
                {
                    Prices = { new SpeciesPrice { SpeciesId= "TUN", Price = 10.0f, Currency = "EUR", MeasurementUnit = "tonne", PortId = "ESBARC", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) },
                            new SpeciesPrice { SpeciesId = "WHA", Price = 231.43, Currency = "EUR", MeasurementUnit = "tonne", PortId = "ESMAR", Timestamp =Timestamp.FromDateTime(DateTime.UtcNow)  }
                    }
                };

                var ecopathReply = await _ecopathWorkflowClient.UpdatePricesAsync(marketReply);
                var poseidonReply = await _poseidonWorkflowClient.UpdatePricesAsync(marketReply);

                // call RunSimulation on Poseidon

                //                ecopathReply = await poseidonClient.RunSimulationAsync(new SimulationRequest { ExperimentId = request.ExperimentId });
            }
            return new Empty();
        }
    }
}
