using Grpc.Core;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class SurimiControllerService : ControllerService.ControllerServiceBase
    {
        private readonly ILogger<SurimiControllerService> _logger;

        private readonly ISimulationManager _simulationManager;

        public SurimiControllerService(ILogger<SurimiControllerService> logger, ISimulationManager simulationManager)
        {
            _logger = logger;
            _simulationManager = simulationManager;
        }

        public override async Task<InitialiseSimulationResponse> InitialiseSimulation(InitialiseSimulationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Simulation {SimulationId} is initializing scenario {ScenarioId}", request.Simulation.SimulationId, request.Simulation.ScenarioId);
            System.Diagnostics.Activity.Current?.SetTag("simulation_id", request.Simulation.SimulationId);

            await _simulationManager.InitSimulationAsync(
                request.Simulation.SimulationId,
                request.Simulation.ScenarioId,
                request.Simulation.StartDateTime,
                request.Simulation.StepSize,
                request.Simulation.SimulationDuration);

            //activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitialiseSimulationResponse() { SimulationId = request.Simulation.SimulationId };
        }

        public override Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Running simulation {SimulationId} ...", request.SimulationId);
            System.Diagnostics.Activity.Current?.SetTag("simulation_id", request.SimulationId);

            try
            {
                _simulationManager.RunSimulationAsync(request.SimulationId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in RunSimulation. ID={SimulationId}", request.SimulationId);
            }

            return Task.FromResult(new RunSimulationResponse() { SimulationId = request.SimulationId });
        }

        public override Task<CancelSimulationResponse> CancelSimulation(CancelSimulationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Cancel Simulation {SimulationId}", request.SimulationId);
            System.Diagnostics.Activity.Current?.SetTag("simulation_id", request.SimulationId);
            _simulationManager.CancelSimulation(request.SimulationId);

            return Task.FromResult(new CancelSimulationResponse() { SimulationId = request.SimulationId });
        }

        public override async Task<GetAllSimulationsResponse> GetAllSimulations(GetAllSimulationsRequest get, ServerCallContext context)
        {
            return await _simulationManager.GetAllSimulationsAsync(context.CancellationToken);
        }
    }
}
