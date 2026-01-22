using Grpc.Core;
using Minio;
using Minio.DataModel.Args;
using System.Security.AccessControl;
using System.Xml.Linq;

namespace SurimiController.Services
{
    public class S3Services : IS3Services
    {
        private readonly ILogger<S3Services> _logger;
        private readonly IOAuth2Services _oAuth2Services;

        public S3Services(ILogger<S3Services> logger, IOAuth2Services oAuth2Services)
        {
            _logger = logger;
            _oAuth2Services = oAuth2Services;
        }

        public async Task<string> GetYamlFromS3(string contractName)
        {
            string? AWS_S3_ENDPOINT = Environment.GetEnvironmentVariable("AWS_S3_ENDPOINT");
            string? AWS_DEFAULT_REGION = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            string? AWS_BUCKET_NAME = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");

            //string? AWS_ACCESS_KEY_ID = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            //string? AWS_SECRET_ACCESS_KEY = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            string? AWS_ACCESS_KEY_ID = "VIcpt2rkxtapSFbKhP3X";
            string? AWS_SECRET_ACCESS_KEY = "AZRoc8XYCqpmMwLx2NO75clmWbr6btQDdZXGL8jS";

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
                    .Build();

                await client.GetObjectAsync(new GetObjectArgs()
                            .WithBucket(AWS_BUCKET_NAME)
                            .WithObject(objectName)
                            .WithFile(localFile));
                _logger.LogInformation("Fetched {ContractName}.yaml from S3 bucket {BucketName}.", contractName, AWS_BUCKET_NAME);

                return await File.ReadAllTextAsync(localFile);
            }
            catch (Minio.Exceptions.AccessDeniedException ex)
            {
                _logger.LogInformation("Access denied when trying to fetch {ContractName}.yaml from S3. Error: {Error}", contractName, ex.Message);
                return "";
            }
        }

        public async Task<string> GetYamlFromS3Bak(string contractName)
        {
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

            int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                string? AWS_ACCESS_KEY_ID = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                string? AWS_SECRET_ACCESS_KEY = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                string? AWS_SESSION_TOKEN = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
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
                    _logger.LogInformation("Fetched {ContractName}.yaml from S3 bucket {BucketName}.", contractName, AWS_BUCKET_NAME);

                    return await File.ReadAllTextAsync(localFile);
                }
                catch (Minio.Exceptions.AccessDeniedException ex)
                {
                    await RefreshS3EnvironmentVariables();
                    _logger.LogInformation("Access denied when trying to fetch {ContractName}.yaml from S3. Error: {Error}", contractName, ex.Message);
                    continue;   // let's try again
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        throw new RpcException(new Status(StatusCode.Internal, $"Couldn't find a local {contractName}.yaml file in the Includes directory. And couldn't retrieve it from the S3 bucket after {maxRetries} attempts. Error: {ex.Message}"));
                    }
                    _logger.LogWarning("Attempt {Attempt} of {MaxRetries} failed to fetch {ContractName}.yaml from S3. Error: {Error}", attempt, maxRetries, contractName, ex.Message);
                    await Task.Delay(1000 * attempt); // Exponential backoff
                }
            }

            throw new RpcException(new Status(StatusCode.Internal, $"Couldn't find a local {contractName}.yaml file in the Includes directory. And couldn't retrieve it from the S3 bucket after {maxRetries} attempts."));
        }

        private async Task<bool> RefreshS3EnvironmentVariables()
        {
            string? DATALAB_USERNAME = Environment.GetEnvironmentVariable("DATALAB_USERNAME");
            string? DATALAB_PASSWORD = Environment.GetEnvironmentVariable("DATALAB_PASSWORD");

            var accessToken = await _oAuth2Services.GetAccessTokenAsync(DATALAB_USERNAME, DATALAB_PASSWORD);
            var (ak, sk, session) = await ExchangeViaStsAsync(accessToken);
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", ak);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", sk);
            Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", session);
            return true;
        }

        private static async Task<(string ak, string sk, string token)> ExchangeViaStsAsync(string accessToken)
        {
            using var http = new HttpClient();

            var uri = new UriBuilder("https://minio.dive.edito.eu/")
            {
                Query =
                    "Action=AssumeRoleWithWebIdentity" +
                    $"&WebIdentityToken={Uri.EscapeDataString(accessToken)}" +
                    "&DurationSeconds=3600" +
                    "&Version=2011-06-15"
            };

            var resp = await http.PostAsync(uri.Uri, null);
            resp.EnsureSuccessStatusCode();

            var xml = await resp.Content.ReadAsStringAsync();

            var root = XDocument.Parse(xml).Root!;
            var ns = root.GetDefaultNamespace();

            string Get(string name) => root.Descendants(ns + name).First().Value;

            return (
                ak: Get("AccessKeyId"),
                sk: Get("SecretAccessKey"),
                token: Get("SessionToken")
            );
        }
    }
}
