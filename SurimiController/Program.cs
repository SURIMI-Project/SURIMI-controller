using Grpc.Surimi;
using System.Diagnostics;

namespace SurimiController;

public class Program
{
    private static readonly ActivitySource MyActivitySource = new("OpenTelemetry.SURIMI");
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Register Custom ActivitySource for the application
        builder.Services.AddSingleton(MyActivitySource);

        // Custom ActivitySource for the application
        using var parent = MyActivitySource.StartActivity("SurimiController");

        // Add services to the container.
        builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<ExceptionMetadataInterceptor>();
        });

        builder.Services.AddGrpcClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("ECOPATH_URL")!);
        }).EnableCallContextPropagation();  // propagates deadlines and cancellation tokens

        builder.Services.AddGrpcClient<EcologyService.EcologyServiceClient>("EcopathEcology", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("ECOPATH_URL")!);
        }).EnableCallContextPropagation();  // propagates deadlines and cancellation tokens

        builder.Services.AddGrpcClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("POSEIDON_URL")!);
        }).EnableCallContextPropagation();  // propagates deadlines and cancellation tokens

        builder.Services.AddGrpcClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("MARKET_URL")!);
        }).EnableCallContextPropagation();  // propagates deadlines and cancellation tokens

        builder.Services.AddGrpcClient<MarketService.MarketServiceClient>(o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("MARKET_URL")!);
        }).EnableCallContextPropagation();  // propagates deadlines and cancellation tokens

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

        app.Run();
    }
}