using Grpc.Surimi;
using SURIMI.ConfigurationService;
using SurimiController.Services;

namespace SurimiController;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container. For communication with the GUI.
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
        builder.Services.AddTransient<ISurimiConfigurationService, SurimiConfigurationService>();

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

        logger.LogInformation("For Ecopath write to: {ECOPATH_URL}", Environment.GetEnvironmentVariable("ECOPATH_URL"));
        logger.LogInformation("For Poseidon write to: {POSEIDON_URL}", Environment.GetEnvironmentVariable("POSEIDON_URL"));
        logger.LogInformation("For Market write to: {MARKET_URL}", Environment.GetEnvironmentVariable("MARKET_URL"));
        logger.LogInformation("For CMSY write to: {CMSY_URL}", Environment.GetEnvironmentVariable("CMSY_URL"));
        logger.LogInformation("For Value Chain write to: {VALUECHAIN_URL}", Environment.GetEnvironmentVariable("VALUECHAIN_URL"));

        logger.LogInformation("AWS_ACCESS_KEY_ID: {AWS_ACCESS_KEY_ID}", Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"));
        logger.LogInformation("AWS_SECRET_ACCESS_KEY: {AWS_SECRET_ACCESS_KEY}", Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"));
        logger.LogInformation("AWS_SESSION_TOKEN: {AWS_SESSION_TOKEN}", Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN"));
        logger.LogInformation("AWS_S3_ENDPOINT: {AWS_S3_ENDPOINT}", Environment.GetEnvironmentVariable("AWS_S3_ENDPOINT"));
        logger.LogInformation("AWS_DEFAULT_REGION: {AWS_DEFAULT_REGION}", Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION"));
        logger.LogInformation("AWS_BUCKET_NAME: {AWS_BUCKET_NAME}", Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"));
        logger.LogInformation("OTEL_EXPORTER_OTLP_ENDPOINT: {OTEL_EXPORTER_OTLP_ENDPOINT}", Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));


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
                options.MaxReceiveMessageSize = 100 * 1024 * 1024; // 100 MB
                options.MaxSendMessageSize = 100 * 1024 * 1024;    // 100 MB
                options.HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true
                };
            });
//            .EnableCallContextPropagation();
        }
    }
}