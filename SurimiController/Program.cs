using Eii.BlobStore;
using Eii.BlobStore.Minio;
using Grpc.Surimi;
using Minio;
using Minio.DataModel.Args;
using SURIMI.ConfigurationService;
using SurimiController.Services;

namespace SurimiController;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddSingleton<IBlobStore>(sp =>
        {
            // if AWS_ACCESS_KEY_ID is set, use MinIO
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")))
            {
                var minio = new MinioClient()
                    .WithEndpoint(Environment.GetEnvironmentVariable("AWS_S3_ENDPOINT"), 443)
                    .WithCredentials(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"), Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"))
                    .WithSSL(true) // set to true if your endpoint uses HTTPS
                    .Build();

                // Ensure bucket exists (idempotent)
                var bucket = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")!;
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var exists = minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cts.Token).GetAwaiter().GetResult();
                if (!exists)
                {
                    minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cts.Token).GetAwaiter().GetResult();
                }

                return new MinioBlobStore(minio, bucket, inputBasePrefix: @"surimi-controller/config", outputBasePrefix: @"surimi-controller", localInputRoot: "Includes", localOutputRoot: "Output");
            }

            // Default local Filesystem
            return new LocalBlobStore(inputRoot: "Includes", outputRoot: "Output");
        });


        // Add services to the container. For communication with the GUI.
        builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<ExceptionMetadataInterceptor>();
        });

        // Use the helper for all your gRPC clients
        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow", "POSEIDON_URL");
        AddConfiguredGrpcClient<SalesProviderService.SalesProviderServiceClient>("PoseidonSalesProvider", "POSEIDON_URL");
        AddConfiguredGrpcClient<SpeciesPriceConsumerService.SpeciesPriceConsumerServiceClient>("PoseidonSpeciesPriceConsumer", "POSEIDON_URL");
        AddConfiguredGrpcClient<CatchProviderService.CatchProviderServiceClient>("PoseidonCatchProvider", "POSEIDON_URL");
        AddConfiguredGrpcClient<EcologyConsumerService.EcologyConsumerServiceClient>("PoseidonEcologyConsumer", "POSEIDON_URL");

        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow", "MARKET_URL");
        AddConfiguredGrpcClient<MarketProviderService.MarketProviderServiceClient>("MarketMarketProvider", "MARKET_URL");

        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow", "CMSY_URL");
        AddConfiguredGrpcClient<EcologyConsumerService.EcologyConsumerServiceClient>("CmsyEcologyConsumer", "CMSY_URL");
        AddConfiguredGrpcClient<CatchConsumerService.CatchConsumerServiceClient>("CmsyCatchConsumer", "CMSY_URL");

        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("AggregatorWorkflow", "AGGREGATOR_URL");
        AddConfiguredGrpcClient<EcologyConsumerService.EcologyConsumerServiceClient>("AggregatorEcologyConsumer", "AGGREGATOR_URL");
        AddConfiguredGrpcClient<CatchConsumerService.CatchConsumerServiceClient>("AggregatorCatchConsumer", "AGGREGATOR_URL");
        AddConfiguredGrpcClient<MarketProviderService.MarketProviderServiceClient>("AggregatorMarketProvider", "AGGREGATOR_URL");
        AddConfiguredGrpcClient<AggregatorService.AggregatorServiceClient>("Aggregator", "AGGREGATOR_URL");

        AddConfiguredGrpcClient<WorkflowService.WorkflowServiceClient>("ValueChainWorkflow", "VALUECHAIN_URL");
        AddConfiguredGrpcClient<MarketProviderService.MarketProviderServiceClient>("ValueChainMarketProvider", "VALUECHAIN_URL");

        builder.Services.AddSingleton<SimulationDispatcher>();
        builder.Services.AddSingleton<ISimulationManager, SimulationManager>();
        builder.Services.AddSingleton<IExperimentManager, ExperimentManager>();
        builder.Services.AddTransient<ISurimiConfigurationService, SurimiConfigurationService>();
        builder.Services.AddTransient<ICmsyServiceClient, CmsyServiceClient>();
        builder.Services.AddTransient<IAggregatorServiceClient, AggregatorServiceClient>();
        builder.Services.AddTransient<IValueChainServiceClient, ValueChainServiceClient>();
        builder.Services.AddTransient<IPoseidonServiceClient, PoseidonServiceClient>();
        builder.Services.AddTransient<IMarketServiceClient, MarketServiceClient>();
        builder.Services.AddTransient<IEcopathServiceClient, EcopathServiceClient>();


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
        app.MapGrpcService<SurimiControllerService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        // Retrieve the logger
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        LoadVaultSecretsInEnvironmentVariables();
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

        void LoadVaultSecretsInEnvironmentVariables()
        {
            var vaultAddr = Environment.GetEnvironmentVariable("VAULT_ADDR");
            var vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
            var vaultTopDir = Environment.GetEnvironmentVariable("VAULT_TOP_DIR");
            var vaultRelativePath = Environment.GetEnvironmentVariable("VAULT_RELATIVE_PATH");
            var vaultMount = Environment.GetEnvironmentVariable("VAULT_MOUNT");
            if (string.IsNullOrEmpty(vaultAddr) || string.IsNullOrEmpty(vaultToken) || string.IsNullOrEmpty(vaultTopDir) || string.IsNullOrEmpty(vaultRelativePath) || string.IsNullOrEmpty(vaultMount))
            {
                Console.WriteLine("Vault Addr, Token, Top Dir, Relative Path, or Mount not set in environment variables. Skipping Vault loading.");
                return;
            }
            var vaultClient = new VaultSharp.VaultClient(new VaultSharp.VaultClientSettings(vaultAddr, new VaultSharp.V1.AuthMethods.Token.TokenAuthMethodInfo(vaultToken)));
            // Assuming secrets are stored under "secret/data/surimi"
            var secretPath = $"{vaultTopDir}/{vaultRelativePath}";
            var secret = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath, mountPoint: vaultMount).Result;
            foreach (var kv in secret.Data.Data)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value.ToString());
                Console.WriteLine($"Loaded secret '{kv.Key}' from Vault into environment variables.");
            }
        }
    }
}