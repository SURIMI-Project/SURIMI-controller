using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using System.Collections.Concurrent;

namespace SURIMI_controller.Services
{

    public class ExperimentManager : IExperimentManager
    {
        private readonly ConcurrentDictionary<string, List<string>> _experiments = new();

        ISimulationManager _simulationManager;
        private readonly ILogger<ExperimentManager> _logger;
        private readonly AggregatorService.AggregatorServiceClient _aggregatorClient;

        public ExperimentManager(GrpcClientFactory clientFactory, ISimulationManager simulationManager, ILogger<ExperimentManager> logger)
        {
            _aggregatorClient = clientFactory.CreateClient<AggregatorService.AggregatorServiceClient>("Aggregator");
            _simulationManager = simulationManager;
            _logger = logger;
        }


        public async Task InitialiseExperiment(InitialiseExperimentRequest request, Grpc.Surimi.Simulation simulation, CancellationToken cancellationToken)
        {
            if (_experiments.ContainsKey(request.ExperimentId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Experiment with Id {request.ExperimentId} is already initialised"));
            }

            _experiments[request.ExperimentId] = new List<string>();
            for (var i = 0; i < request.NumberOfRuns; i++)
            {
                var simulationId = Guid.NewGuid().ToString();
                await _simulationManager.InitSimulationAsync(
                    simulationId,
                    request.ExperimentId,
                    request.ScenarioId,
                    request.EndDateTime?.ToDateTime(),
                    simulation,
                    request.RegulationsDefinitionsSummary,
                    cancellationToken
                    );
                _experiments[request.ExperimentId].Add(simulationId);
            }

            await _aggregatorClient.RegisterExperimentAsync(new RegisterExperimentRequest()
            {
                ExperimentId = request.ExperimentId,
                SimulationIds = { _experiments[request.ExperimentId] }
            });
        }

        public async Task RunExperimentAsync(string experimentId, CancellationToken cancellationToken)
        {
            if (!_experiments.ContainsKey(experimentId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Experiment with Id {experimentId} cannot be found and cannot run"));
            }

            foreach (var simulationId in _experiments[experimentId])
            {
                try
                {
                    await _simulationManager.RunSimulationAsync(simulationId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in running Simulation. ID={SimulationId}", simulationId);
                }
            }
        }

        public async Task CancelExperimentAsync(string experimentId, CancellationToken cancellationToken)
        {
            if (!_experiments.ContainsKey(experimentId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Experiment with Id {experimentId} cannot be found and cannot cancel"));
            }

            foreach (var simulationId in _experiments[experimentId])
            {
                try
                {
                    await _simulationManager.CancelSimulationAsync(simulationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in canceling Simulation. ID={SimulationId}", simulationId);
                }
            }
        }

        public async Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken)
        {
            return await _simulationManager.GetAllSimulationStatussesAsync(cancellationToken);
        }
    }
}
