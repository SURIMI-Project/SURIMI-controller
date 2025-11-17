using Grpc.Surimi;
using SurimiController.Services;
using System.Diagnostics;

namespace SurimiController;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<ExceptionMetadataInterceptor>();
        });

        // Use the helper for all your gRPC clients
        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow", "POSEIDON_URL");
        AddConfiguredGrpcClient<MarketService.MarketServiceClient>("PoseidonMarket", "POSEIDON_URL");
        AddConfiguredGrpcClient<FisheryService.FisheryServiceClient>("PoseidonFishery", "POSEIDON_URL");
        AddConfiguredGrpcClient<EcologyService.EcologyServiceClient>("PoseidonEcology", "POSEIDON_URL");

        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow", "MARKET_URL");
        AddConfiguredGrpcClient<MarketService.MarketServiceClient>("MarketMarket", "MARKET_URL");

        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow", "CMSY_URL");
        AddConfiguredGrpcClient<EcologyService.EcologyServiceClient>("CmsyEcology", "CMSY_URL");
        AddConfiguredGrpcClient<FisheryService.FisheryServiceClient>("CmsyFishery", "CMSY_URL");

        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("ValueChainWorkflow", "VALUECHAIN_URL");
        AddConfiguredGrpcClient<MarketService.MarketServiceClient>("ValueChainMarket", "VALUECHAIN_URL");

        builder.Services.AddSingleton<SimulationDispatcher>();
        builder.Services.AddSingleton<ISimulationManager, SimulationManager>();

        builder.Logging.ClearProviders();
        builder.Services.AddLogging(opt =>
        {
            opt.AddSimpleConsole(c =>
            {
                c.TimestampFormat = "[HH:mm:ss] ";
            });
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<Services.SurimiControllerService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        // Retrieve the logger
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation($"For Ecopath write to: {Environment.GetEnvironmentVariable("ECOPATH_URL")}");
        logger.LogInformation($"For Poseidon write to: {Environment.GetEnvironmentVariable("POSEIDON_URL")}");
        logger.LogInformation($"For Market write to: {Environment.GetEnvironmentVariable("MARKET_URL")}");
        logger.LogInformation($"For CMSY write to: {Environment.GetEnvironmentVariable("CMSY_URL")}");
        logger.LogInformation($"For Value Chain write to: {Environment.GetEnvironmentVariable("VALUECHAIN_URL")}");

        app.Run();


        // Local helper function for registering gRPC clients
        void AddConfiguredGrpcClient<TClient>(string? name, string envVar)
            where TClient : class
        {
            var baseAddress = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                throw new InvalidOperationException($"Environment variable '{envVar}' is not set.");
            }

            var registration = name != null
                ? builder.Services.AddGrpcClient<TClient>(name, o =>
                {
                    o.Address = new Uri(baseAddress);
                })
                : builder.Services.AddGrpcClient<TClient>(o =>
                {
                    o.Address = new Uri(baseAddress);
                });

            registration.ConfigureChannel(options =>
            {
                options.Credentials = Grpc.Core.ChannelCredentials.Insecure;
                options.HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true
                };
            });
//            .EnableCallContextPropagation();
        }
    }
}