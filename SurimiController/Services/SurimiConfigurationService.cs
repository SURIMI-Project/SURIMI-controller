using SURIMI.Datamodel;
using SurimiController.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SURIMI.ConfigurationService
{
    public class SurimiConfigurationService : ISurimiConfigurationService
    {
        private readonly ILogger<SurimiConfigurationService> _logger;
        private readonly IS3Services _s3Services;

        public SurimiConfigurationService(ILogger<SurimiConfigurationService> logger, IS3Services s3Services)
        {
            _logger = logger;
            _s3Services = s3Services;
        }

        /// <summary>
        /// This method tries to read the Surimi configuration from a local file.
        /// If the file does not exist locally, it fetches it from an S3-compatible storage using MinIO client.
        /// This will be the case when running in a Docker container in Kubernetes.
        /// </summary>
        /// <returns>A SurimiConfiguration object representing the configuration.</returns>
        public async Task<SurimiConfiguration> ReadConfigurationAsync(string contractName)
        {
            string yaml = "";
            if (File.Exists(AppContext.BaseDirectory + $"/Includes/{contractName}.yaml"))
            {
                yaml = await File.ReadAllTextAsync(AppContext.BaseDirectory + $"/Includes/{contractName}.yaml");
                _logger.LogInformation("Loaded {ContractName}.yaml from local Includes directory.", contractName);
            }
            else
            {
                yaml = await _s3Services.GetYamlFromS3(contractName);
            }

            return DeserialiseConfiguration(yaml);
        }

        public SurimiConfiguration DeserialiseConfiguration(string yaml)
        {
            // Implementation for reading YAML configuration
            var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<SurimiConfiguration>(input);
        }
    }
}
