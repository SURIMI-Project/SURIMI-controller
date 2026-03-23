using Grpc.Surimi;
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

        // Add EDITO DataLab service
        builder.Services.AddHttpClient<EditoDataLabService>();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddGrpcClient<ControllerService.ControllerServiceClient>(o =>
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

        // Retrieve the logger
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"For the Controller write to: {Environment.GetEnvironmentVariable("CONTROLLER_URL")}");

        LoadVaultSecretsInEnvironmentVariables();

        app.Run();

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
