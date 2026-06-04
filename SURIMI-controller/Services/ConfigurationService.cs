using Eii.BlobStore;
using Grpc.Core;
using SURIMI.Datamodel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SURIMI_controller.ConfigurationService
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IBlobStore _blobStore;

        public ConfigurationService(ILogger<ConfigurationService> logger, IBlobStore blobStore)
        {
            _logger = logger;
            _blobStore = blobStore;
        }

        /// <summary>
        /// This method tries to read the Surimi configuration from a local file.
        /// If the file does not exist locally, it fetches it from an S3-compatible storage.
        /// This will be the case when running in a Docker container in Kubernetes.
        /// </summary>
        /// <returns>A SurimiConfiguration object representing the configuration.</returns>
        public async Task<SurimiConfiguration> ReadConfigurationAsync(string contractName)
        {
            string yaml = "";
            if (!await _blobStore.ExistsAsync($"{contractName}.yaml", PathType.Input))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Couldn't find {contractName}.yaml"));
            }

            yaml = await _blobStore.ReadAllTextAsync($"{contractName}.yaml", PathType.Input);
            _logger.LogInformation("Loaded {ContractName}.yaml", contractName);
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
