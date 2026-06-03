using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI.Common.gRPC.Services;

namespace SURIMI_controller.Services
{
    public class VersionCheckerService
    {
        private readonly ILogger<VersionCheckerService> _logger;

        private readonly FisheryService.FisheryServiceClient _fisheryServiceClient;
        private readonly StockAssessmentService.StockAssessmentServiceClient _stockAssessmentServiceClient;
        private readonly ValueChainService.ValueChainServiceClient _valueChainServiceClient;
        private readonly MarketService.MarketServiceClient _marketServiceClient;
        private readonly FisheriesAuthorityService.FisheriesAuthorityServiceClient _fisheriesAuthorityServiceClient;
        private readonly EnvironmentService.EnvironmentServiceClient _environmentServiceClient;
        private readonly OutputCreatorService.OutputCreatorServiceClient _outputCreatorServiceClient;

        private readonly SimulationDispatcher _ecopathSimDispatcher;
        private readonly ProtocolVersionService _protocolVersionService;

        public VersionCheckerService(GrpcClientFactory clientFactory, ILogger<VersionCheckerService> logger, SimulationDispatcher ecopathSimDispatcher, ProtocolVersionService protocolVersionService)
        {
            _fisheryServiceClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("Poseidon");
            _stockAssessmentServiceClient = clientFactory.CreateClient<StockAssessmentService.StockAssessmentServiceClient>("Cmsy");
            _valueChainServiceClient = clientFactory.CreateClient<ValueChainService.ValueChainServiceClient>("ValueChain");
            _marketServiceClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("Market");
            _fisheriesAuthorityServiceClient = clientFactory.CreateClient<FisheriesAuthorityService.FisheriesAuthorityServiceClient>("FisheriesAuthority");
            _environmentServiceClient = clientFactory.CreateClient<EnvironmentService.EnvironmentServiceClient>("Environment");
            _outputCreatorServiceClient = clientFactory.CreateClient<OutputCreatorService.OutputCreatorServiceClient>("OutputCreator");

            _ecopathSimDispatcher = ecopathSimDispatcher;
            _protocolVersionService = protocolVersionService;
            _logger = logger;
        }

        public void WriteVersions()
        {
            var versions = new Dictionary<string, string>();

            versions["Poseidon"] = _fisheryServiceClient.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
            versions["Cmsy"] = _stockAssessmentServiceClient.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
            versions["OutputCreator"] = _outputCreatorServiceClient.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
            versions["ValueChain"] = _valueChainServiceClient.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
            versions["Market"] = _marketServiceClient.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
            versions["FisheriesAuthority"] = _fisheriesAuthorityServiceClient.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;
            versions["Environment"] = _environmentServiceClient.GetProtocolVersion(new GetProtocolVersionRequest()).ProtocolVersion;

            try
            {
                var ecopathGetProtocolVersionResponse = _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetProtocolVersionRequest, GetProtocolVersionResponse>(
                    new GetProtocolVersionRequest(),
                    "dummy",
                    (client, req) => client.GetProtocolVersionAsync(req));
                var response = ecopathGetProtocolVersionResponse.GetAwaiter().GetResult();
                versions["EcopathWorkflow"] = response.ProtocolVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get version from Ecopath");
                versions["EcopathWorkflow"] = "Error";
            }
            versions["Controller"] = _protocolVersionService.LoadVersion();
            var versionSummary = string.Join(Environment.NewLine, versions.Select(kvp => $"  {kvp.Key, -27}: {kvp.Value}"));
            _logger.LogInformation("Protocol versions:{NewLine}{VersionSummary}", Environment.NewLine, versionSummary);
        }
    }
}
