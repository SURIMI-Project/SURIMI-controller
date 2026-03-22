using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class VersionCheckerService
    {
        private readonly ILogger<VersionCheckerService> _logger;

        private readonly Dictionary<string, WorkflowService.WorkflowServiceClient> _workflowClients;

        public VersionCheckerService(GrpcClientFactory clientFactory, ILogger<VersionCheckerService> logger)
        {

            _workflowClients = new Dictionary<string, WorkflowService.WorkflowServiceClient>
            {
                { "PoseidonWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow") },
                { "CmsyWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow") },
//                { "EcopathWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow") },
                { "AggregatorWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("AggregatorWorkflow") },
                { "ValueChainWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("ValueChainWorkflow") },
                { "MarketWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow") },
                { "EnvironmentWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EnvironmentWorkflow") },
                { "FisheriesAuthorityWorkflow", clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("FisheriesAuthorityWorkflow") }
            };
            _logger = logger;
        }

        public void WriteVersions()
        {
            foreach (var client in _workflowClients)
            {
                try
                {
                    var version = client.Value.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
                    _logger.LogInformation("Service {ServiceName} is running version {Version}", client.Key, version);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get version from service {ServiceName}", client.Key);
                }
            }
        }
    }
}
