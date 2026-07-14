using Eii.BlobStore;
using Eii.BlobStore.S3;
using Grpc.Net.Client.Configuration;
using Grpc.Surimi;
using SURIMI.Common.gRPC;
using SURIMI.Common.gRPC.Services;
using SURIMI.Common.Services;
using SURIMI_controller.ConfigurationService;
using SURIMI_controller.Services;

namespace SURIMI_controller;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddSingleton<IBlobStore>(sp =>
        {
            // if AWS_ACCESS_KEY_ID is set, use S3 compatible storage
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")))
            {
                return new S3BlobStore(
                    Environment.GetEnvironmentVariable("AWS_S3_ENDPOINT"),
                    Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
                    Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
                    Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"),
                    inputBasePrefix: @"controller", outputBasePrefix: @"controller", localInputRoot: "Includes", localOutputRoot: "Output");
            }

            // Default local Filesystem
            return new LocalBlobStore(inputRoot: "Includes", outputRoot: "Output");
        });


        // Add services to the container. For communication with the GUI.
        builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<ExceptionMetadataInterceptor>();
            options.Interceptors.Add<VersionMetadataInterceptor>();
        });

        // Use the helper for all your gRPC clients
        AddConfiguredGrpcClient<FisheryService.FisheryServiceClient>("Poseidon", "POSEIDON_URL");
        AddConfiguredGrpcClient<MarketService.MarketServiceClient>("Market", "MARKET_URL");
        AddConfiguredGrpcClient<StockAssessmentService.StockAssessmentServiceClient>("Cmsy", "CMSY_URL");
        AddConfiguredGrpcClient<ValueChainService.ValueChainServiceClient>("ValueChain", "VALUECHAIN_URL");
        AddConfiguredGrpcClient<EnvironmentService.EnvironmentServiceClient>("Environment", "ENVIRONMENT_URL");
        AddConfiguredGrpcClient<FisheriesAuthorityService.FisheriesAuthorityServiceClient>("FisheriesAuthority", "FISHERIES_AUTHORITY_URL");
        AddConfiguredGrpcClient<OutputCreatorService.OutputCreatorServiceClient>("OutputCreator", "OUTPUT_CREATOR_URL");

        builder.Services.AddSingleton<GrpcErrorDetailLoggingInterceptor>();
        builder.Services.Configure<SimulationDispatcherOptions>(o =>
        {
            o.EcopathUrl = Environment.GetEnvironmentVariable("ECOPATH_URL") ?? string.Empty;
            o.PodNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? string.Empty;
        });
        builder.Services.AddSingleton<SimulationDispatcher>();
        builder.Services.AddSingleton<ISimulationManager, SimulationManager>();
        builder.Services.AddSingleton<IExperimentManager, ExperimentManager>();
        builder.Services.AddSingleton<IAggregatorService, AggregatorService>();

        builder.Services.AddTransient<IConfigurationService, ConfigurationService.ConfigurationService>();
        builder.Services.AddTransient<ICmsyServiceClient, CmsyServiceClient>();
        builder.Services.AddTransient<IValueChainServiceClient, ValueChainServiceClient>();
        builder.Services.AddSingleton<IPoseidonServiceClient, PoseidonServiceClient>(); // singleton because it holds state about the current experiment (e.g. which fishery models are currently loaded)
        builder.Services.AddTransient<IMarketServiceClient, MarketServiceClient>();
        builder.Services.AddTransient<IEcopathServiceClient, EcopathServiceClient>();
        builder.Services.AddTransient<IEnvironmentServiceClient, EnvironmentServiceClient>();
        builder.Services.AddTransient<IOutputCreatorServiceClient, OutputCreatorServiceClient>();
        builder.Services.AddTransient<IFisheriesAuthorityServiceClient, FisheriesAuthorityServiceClient>();
        builder.Services.AddSingleton<VersionCheckerService>();
        builder.Services.AddTransient<ProtocolVersionService>();
        builder.Services.AddTransient<VaultService>();

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
        app.MapGrpcService<Services.ControllerService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        // Retrieve the logger
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        app.Services.GetRequiredService<VaultService>().LoadVaultSecretsInEnvironmentVariables();

        logger.LogInformation("============================= Logging All Environment Variables =============================");
        foreach (System.Collections.DictionaryEntry envVar in Environment.GetEnvironmentVariables())
        {
            logger.LogInformation("{Key}: {Value}", envVar.Key, envVar.Value);
        }
        logger.LogInformation("============================= End of Environment Variables =============================");

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
                ? builder.Services.AddGrpcClient<TClient>(name, o => { o.Address = new Uri(baseAddress); })
                : builder.Services.AddGrpcClient<TClient>(o => { o.Address = new Uri(baseAddress); });


            registration
                .AddInterceptor<GrpcErrorDetailLoggingInterceptor>()
                // Remove the Aspire standard resilience pipeline (AttemptTimeout, Polly retry, circuit breaker).
                // gRPC clients use their own retry via ServiceConfig and are long-lived by design.
                .RemoveAllResilienceHandlers();

            registration.ConfigureChannel(options =>
            {
                options.Credentials = Grpc.Core.ChannelCredentials.Insecure;
                options.MaxReceiveMessageSize = 100 * 1024 * 1024; // 100 MB
                options.MaxSendMessageSize = 100 * 1024 * 1024;    // 100 MB
                // No ServiceConfig retry — all gRPC calls here are stateful and non-idempotent.
                // Retrying InitialiseExperiment, ExperimentStep, etc. would corrupt downstream state.
                // If a call fails, it should surface as an error, not be silently retried.
            });

            // Configure the HTTP handler separately so gRPC built-in retry remains active.
            // PooledConnectionLifetime forces periodic DNS re-resolution, preventing stale
            // entries after pod restarts in Kubernetes.
            registration.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                KeepAlivePingDelay = TimeSpan.FromMinutes(6),       // > Python gRPC server's 5-min minimum
                KeepAlivePingTimeout = TimeSpan.FromSeconds(20),    // time to wait for pong before failing
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests // ping during active requests too
            });
            //            .EnableCallContextPropagation();
        }
    }
}