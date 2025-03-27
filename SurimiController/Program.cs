using SurimiController.Services;
using SurimiWorkflow;

namespace SurimiController;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddGrpc();

        builder.Services.AddGrpcClient<Workflow.WorkflowClient>("EcopathWorkflow", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("ECOPATH_URL")!);
        });
        builder.Services.AddGrpcClient<Workflow.WorkflowClient>("PoseidonWorkflow", o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("POSEIDON_URL")!);
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<WorkflowService>();
        app.MapGrpcService<ControllerService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        Console.WriteLine($"For Ecopath write to: {Environment.GetEnvironmentVariable("ECOPATH_URL")}");
        Console.WriteLine($"For Poseidon write to: {Environment.GetEnvironmentVariable("POSEIDON_URL")}");

        app.Run();
    }
}