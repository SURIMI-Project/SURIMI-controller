using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI.Common.gRPC.Services;

namespace SURIMI_controller.Services
{
    public class VersionCheckerService
    {
        private readonly ILogger<VersionCheckerService> _logger;

        private readonly Dictionary<string, WorkflowService.WorkflowServiceClient> _workflowClients;
        private readonly SimulationDispatcher _ecopathSimDispatcher;
        private readonly ProtocolVersionService _protocolVersionService;

        public VersionCheckerService(GrpcClientFactory clientFactory, ILogger<VersionCheckerService> logger, SimulationDispatcher ecopathSimDispatcher, ProtocolVersionService protocolVersionService)
        {
            _workflowClients = new Dictionary<string, WorkflowService.WorkflowServiceClient>
            {
                { "PoseidonWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow") },
                { "CmsyWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow") },
                { "AggregatorWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("AggregatorWorkflow") },
                { "ValueChainWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("ValueChainWorkflow") },
                { "MarketWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow") },
                { "EnvironmentWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EnvironmentWorkflow") },
                { "FisheriesAuthorityWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("FisheriesAuthorityWorkflow") }
            };
            _ecopathSimDispatcher = ecopathSimDispatcher;
            _protocolVersionService = protocolVersionService;
            _logger = logger;
        }

        public async Task WriteVersions()
        {
            var versions = new Dictionary<string, string>();

            foreach (var client in _workflowClients)
            {
                try
                {
                    var version = client.Value.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
                    versions[client.Key] = version;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get version from service {ServiceName}", client.Key);
                    versions[client.Key] = "Error";
                }
            }

            try
            {
                var ecopathGetProtocolVersionResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, GetProtocolVersionRequest, GetProtocolVersionResponse>(
                    new GetProtocolVersionRequest(),
                    "dummy",
                    (client, req) => client.GetProtocolVersionAsync(req));
                var response = await ecopathGetProtocolVersionResponse;
                versions["EcopathWorkflow"] = response.ProtocolVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get version from Ecopath");
                versions["EcopathWorkflow"] = "Error";
            }
            versions["Controller"] = _protocolVersionService.LoadVersion();
            var versionSummary = string.Join(Environment.NewLine, versions.Select(kvp => $"  {kvp.Key}: {kvp.Value}"));
            _logger.LogInformation("Protocol versions:{NewLine}{VersionSummary}", Environment.NewLine, versionSummary);
        }

    }
}
