using Grpc.Surimi;
using SURIMI.Common.Services;
using SURIMI_gui.Components;
using SURIMI_gui.Services;

namespace SURIMI_gui;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddSingleton<SurimiGUIControllerService>();
        builder.Services.AddTransient<VaultService>();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddGrpcClient<ControllerService.ControllerServiceClient>(options =>
        {
            options.Address = new Uri(Environment.GetEnvironmentVariable("CONTROLLER_URL")!);
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

        // Retrieve the logger
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"For the Controller write to: {Environment.GetEnvironmentVariable("CONTROLLER_URL")}");

        app.Services.GetRequiredService<VaultService>().LoadVaultSecretsInEnvironmentVariables();

        app.Run();
    }
}
