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
        /// <returns>A SurimiContract object representing the configuration.</returns>
        public async Task<SurimiContract> ReadConfigurationAsync(string scenarioName)
        {
            string yaml = "";
            if (!await _blobStore.ExistsAsync($"{scenarioName}/contract.yaml", PathType.Input))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Couldn't find {scenarioName}/contract.yaml"));
            }

            yaml = await _blobStore.ReadAllTextAsync($"{scenarioName}/contract.yaml", PathType.Input);
            _logger.LogInformation("Loaded {ScenarioName}/contract.yaml", scenarioName);
            return DeserialiseContract(yaml);
        }

        public SurimiContract DeserialiseContract(string yaml)
        {
            // Implementation for reading YAML configuration
            var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<SurimiContract>(input);
        }
    }
}
