using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Reflection;

namespace CosmosDB
{
    public class Config
    {
        private static AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
        private static KeyVaultClient keyVaultClient = new((authority, resource, scope) => azureServiceTokenProvider.GetAccessTokenAsync(resource));

        public async Task<string> GetValue(string key)
        {
            if (Environment.GetEnvironmentVariable("FUNCTIONS_CORETOOLS_ENVIRONMENT") != null)
                return LocalSettings(key);
            if (Environment.GetEnvironmentVariable(key) == null)
            {
                var KeyVaultName = "AzureValut";
                var secret = await keyVaultClient.GetSecretAsync($"https://{KeyVaultName}.vault.azure.net/", key);                
                return secret.Value;
            }
            return Environment.GetEnvironmentVariable(key) ?? String.Empty;
        }

        public static string LocalSettings(string key)
        {
            ConfigurationBuilder builder = new();
            builder.AddUserSecrets(Assembly.GetExecutingAssembly());
            builder.AddEnvironmentVariables();
            var configuration = builder.Build();
            var output = configuration.GetValue<string>(key);
            return output ?? string.Empty;
        }
    }
}
