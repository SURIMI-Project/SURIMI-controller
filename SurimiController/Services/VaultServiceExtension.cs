using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace SurimiController.Services
{
    public static class VaultServiceExtension
    {
        public static WebApplication AddVaultSecretsAsEnvironmentVariables(this WebApplication app)
        {
            try
            {
                string? VAULT_TOKEN = Environment.GetEnvironmentVariable("VAULT_TOKEN");
                string? VAULT_ADDR = Environment.GetEnvironmentVariable("VAULT_ADDR");
                string? VAULT_MOUNT = Environment.GetEnvironmentVariable("VAULT_MOUNT");
                string? VAULT_TOP_DIR = Environment.GetEnvironmentVariable("VAULT_TOP_DIR");
                string? VAULT_RELATIVE_PATH = Environment.GetEnvironmentVariable("VAULT_RELATIVE_PATH");

                if(string.IsNullOrEmpty(VAULT_TOKEN) ||
                    string.IsNullOrEmpty(VAULT_ADDR) ||
                    string.IsNullOrEmpty(VAULT_MOUNT) ||
                    string.IsNullOrEmpty(VAULT_TOP_DIR) ||
                    string.IsNullOrEmpty(VAULT_RELATIVE_PATH))
                {
                    return app; // if these environment variables are not set, skip Vault integration
                }

                IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken: VAULT_TOKEN);

                VaultClientSettings vaultClientSettings = new VaultClientSettings(VAULT_ADDR, authMethod);
                IVaultClient vaultClient = new VaultClient(vaultClientSettings);

                // Read a secret
                var secret = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path: $"{VAULT_TOP_DIR}/{VAULT_RELATIVE_PATH}",
                    mountPoint: $"{VAULT_MOUNT}"
                ).Result;

                foreach (var kvp in secret.Data.Data)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error. Couldn't read from Vault: {ex.Message}");
            }
            return app;
        }
    }
}
