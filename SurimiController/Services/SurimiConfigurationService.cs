using Grpc.Core;
using Minio;
using Minio.DataModel.Args;
using SURIMI.Datamodel;
using SurimiController.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SURIMI.ConfigurationService
{
    public class SurimiConfigurationService : ISurimiConfigurationService
    {
        private readonly ILogger<SurimiConfigurationService> _logger;

        public SurimiConfigurationService(ILogger<SurimiConfigurationService> logger)
        {
            _logger = logger;
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
            if ( File.Exists(AppContext.BaseDirectory + $"/Includes/{contractName}.yaml"))
            {
                yaml = await File.ReadAllTextAsync(AppContext.BaseDirectory + $"/Includes/{contractName}.yaml");
                _logger.LogInformation("Loaded {ContractName}.yaml from local Includes directory.", contractName);
            }
            else
            {
                string? AWS_ACCESS_KEY_ID = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                string? AWS_SECRET_ACCESS_KEY = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                string? AWS_SESSION_TOKEN = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
                string? AWS_S3_ENDPOINT = Environment.GetEnvironmentVariable("AWS_S3_ENDPOINT");
                string? AWS_DEFAULT_REGION = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                string? AWS_BUCKET_NAME = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");

                string objectName = $"surimi-controller/config/{contractName}.yaml";

                // Create temp directory if it doesn't exist
                string tempDirectory = Path.Combine(AppContext.BaseDirectory, "temp");
                Directory.CreateDirectory(tempDirectory);

                // Extract original filename and create full path
                string originalFileName = Path.GetFileName(objectName);
                string localFile = Path.Combine(tempDirectory, originalFileName);

                try
                {
                    var client = new MinioClient()
                        .WithEndpoint(AWS_S3_ENDPOINT, 443) // or 443 for HTTPS, 9000 for http "minio.your-domain.local"
                        .WithCredentials(AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
                        .WithSSL(true) // set to true if your endpoint uses HTTPS
                        .WithRegion(AWS_DEFAULT_REGION)
                        .WithSessionToken(AWS_SESSION_TOKEN)
                        .Build();

                    await client.GetObjectAsync(new GetObjectArgs()
                                .WithBucket(AWS_BUCKET_NAME)

                                .WithObject(objectName)
                                .WithFile(localFile));
                    yaml = File.ReadAllText(localFile);
                    _logger.LogInformation("Fetched {ContractName}.yaml from S3 bucket {BucketName}.", contractName, AWS_BUCKET_NAME);
                }
                catch (Exception ex)
                {
                    throw new RpcException(new Status(StatusCode.Internal, $"Couldn't find a local {contractName}.yaml file in the Includes directory. And couldn't retrieve it from the S3 bucket. Error: {ex.Message}"));
                }
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
