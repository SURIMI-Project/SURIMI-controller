using Grpc.Core;
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

        public async Task WriteVersionsAsync(CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<(string Name, string Version)>>
            {
                GetVersionWithRetryAsync("Poseidon",          async () => (await _fisheryServiceClient.GetProtocolVersionAsync(new GetProtocolVersionRequest(), cancellationToken: cancellationToken)).ProtocolVersion, cancellationToken),
                GetVersionWithRetryAsync("Cmsy",              async () => (await _stockAssessmentServiceClient.GetProtocolVersionAsync(new GetProtocolVersionRequest(), cancellationToken: cancellationToken)).ProtocolVersion, cancellationToken),
                GetVersionWithRetryAsync("OutputCreator",     async () => (await _outputCreatorServiceClient.GetProtocolVersionAsync(new GetProtocolVersionRequest(), cancellationToken: cancellationToken)).ProtocolVersion, cancellationToken),
                GetVersionWithRetryAsync("ValueChain",        async () => (await _valueChainServiceClient.GetProtocolVersionAsync(new GetProtocolVersionRequest(), cancellationToken: cancellationToken)).ProtocolVersion, cancellationToken),
                GetVersionWithRetryAsync("Market",            async () => (await _marketServiceClient.GetProtocolVersionAsync(new GetProtocolVersionRequest(), cancellationToken: cancellationToken)).ProtocolVersion, cancellationToken),
                GetVersionWithRetryAsync("FisheriesAuthority",async () => (await _fisheriesAuthorityServiceClient.GetProtocolVersionAsync(new GetProtocolVersionRequest(), cancellationToken: cancellationToken)).ProtocolVersion, cancellationToken),
                GetVersionWithRetryAsync("Environment",       async () => (await _environmentServiceClient.GetProtocolVersionAsync(new GetProtocolVersionRequest(), cancellationToken: cancellationToken)).ProtocolVersion, cancellationToken),
                GetVersionWithRetryAsync("EcopathWorkflow",   async () =>
                {
                    var response = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetProtocolVersionRequest, GetProtocolVersionResponse>(
                        new GetProtocolVersionRequest(),
                        "dummy",
                        (client, req) => client.GetProtocolVersionAsync(req, cancellationToken: cancellationToken));
                    return response.ProtocolVersion;
                }, cancellationToken),
            };

            var results = await Task.WhenAll(tasks);

            var versions = results.ToDictionary(r => r.Name, r => r.Version);
            versions["Controller"] = _protocolVersionService.LoadVersion();

            var versionSummary = string.Join(Environment.NewLine, versions.Select(kvp => $"  {kvp.Key,-27}: {kvp.Value}"));
            _logger.LogInformation("Protocol versions:{NewLine}{VersionSummary}", Environment.NewLine, versionSummary);
        }

        private async Task<(string Name, string Version)> GetVersionWithRetryAsync(string serviceName, Func<Task<string>> getVersion, CancellationToken ct)
        {
            var initialDelay = TimeSpan.FromSeconds(2);
            var maxDelay = TimeSpan.FromSeconds(30);
            var attempt = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var version = await getVersion();
                    return (serviceName, version);
                }
                catch (Exception ex) when (ex is RpcException or HttpRequestException)
                {
                    var delay = TimeSpan.FromSeconds(Math.Min(maxDelay.TotalSeconds, initialDelay.TotalSeconds * Math.Pow(2, attempt)));
                    _logger.LogWarning("{ServiceName} not yet available ({Message}). Retrying in {Delay}s...", serviceName, ex.Message, (int)delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                    attempt++;
                }
            }
        }
    }
}
