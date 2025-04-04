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
        builder.Services.AddGrpc();

        builder.Services.AddGrpcClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("ECOPATH_URL")!);
        });
        builder.Services.AddGrpcClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("POSEIDON_URL")!);
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<Services.SurimiWorkflowService>();
        app.MapGrpcService<Services.SurimiControllerService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        Console.WriteLine($"For Ecopath write to: {Environment.GetEnvironmentVariable("ECOPATH_URL")}");
        Console.WriteLine($"For Poseidon write to: {Environment.GetEnvironmentVariable("POSEIDON_URL")}");

        app.Run();
    }
}