using Grpc.Core;
using Grpc.Surimi;
using System.Diagnostics;

namespace SurimiController.Services
{
    public class SurimiControllerService : ControllerService.ControllerServiceBase
    {
        private readonly ILogger<SurimiControllerService> _logger;
        private readonly ActivitySource _activitySource;

        private readonly ISimulationManager _simulationManager;

        public SurimiControllerService(ActivitySource activitySource, ILogger<SurimiControllerService> logger, ISimulationManager simulationManager)
        {
            _activitySource = activitySource;
            _logger = logger;
            _simulationManager = simulationManager;
        }

        public override async Task<InitSimulationResponse> InitSimulation(InitSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("InitSimulation");
            activity?.SetTag("ScenarioId", request.Simulation.ScenarioId);

            _logger.LogInformation("{SimulationId} Initializing scenario {ScenarioId}", request.Simulation.SimulationId, request.Simulation.ScenarioId);

            await _simulationManager.InitSimulationAsync(
                request.Simulation.SimulationId,
                request.Simulation.ScenarioId,
                request.Simulation.StartDateTime,
                request.Simulation.StepSize,
                request.Simulation.SimulationDuration);

            activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitSimulationResponse() { SimulationId = request.Simulation.SimulationId };
        }

        public override Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("RunSimulation");
            _logger.LogInformation("{SimulationId} Running simulation...", request.SimulationId);

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
            _logger.LogInformation("{SimulationId} Cancel Simulation", request.SimulationId);
            _simulationManager.CancelSimulation(request.SimulationId);

            return Task.FromResult(new CancelSimulationResponse() { SimulationId = request.SimulationId });
        }

        public override async Task<GetAllSimulationsResponse> GetAllSimulations(GetAllSimulationsRequest get, ServerCallContext context)
        {
            return await _simulationManager.GetAllSimulationsAsync(context.CancellationToken);
        }
    }
}
