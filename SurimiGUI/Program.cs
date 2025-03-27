using SurimiController;
using SurimiGUI.Components;
using SurimiGUI.Services;
using SurimiWorkflow;

namespace SurimiGUI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddSingleton<ControllerService>();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddGrpcClient<Workflow.WorkflowClient>(o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("CONTROLLER_URL")!);
        });

        builder.Services.AddGrpcClient<Controller.ControllerClient>(o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("CONTROLLER_URL")!);
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        Console.WriteLine($"For the Controller write to: {Environment.GetEnvironmentVariable("CONTROLLER_URL")}");

        app.Run();
    }
}
