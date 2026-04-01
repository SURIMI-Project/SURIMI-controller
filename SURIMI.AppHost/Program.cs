IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var poseidon = builder.AddContainer("poseidon", "nicolaspayette/poseidon", "latest")
    .WithHttpEndpoint( port: 50051, targetPort: 50051, name: "poseidon");

var market = builder.AddContainer("market", "rikkert242/market", "latest")
    .WithHttpEndpoint(port: 5001, targetPort: 5001, name: "market")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIDQ2Gadjlo16vHLjLp8pqhKrv_QOGnMwU3UJfyJoQGR5Gh4KHGh2cy5FQ3ZYU0g3YllaWVB0MmVhanVXZE5zRGo")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var cmsy = builder.AddContainer("cmsy", "rikkert242/cmsy", "latest")
    .WithHttpEndpoint(port: 5021, targetPort: 5021, name: "cmsy")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIDQ2Gadjlo16vHLjLp8pqhKrv_QOGnMwU3UJfyJoQGR5Gh4KHGh2cy5FQ3ZYU0g3YllaWVB0MmVhanVXZE5zRGo")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var aggregator = builder.AddContainer("aggregator", "rikkert242/aggregator", "latest")
    .WithHttpEndpoint(port: 5188, targetPort: 5188, name: "aggregator")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIDQ2Gadjlo16vHLjLp8pqhKrv_QOGnMwU3UJfyJoQGR5Gh4KHGh2cy5FQ3ZYU0g3YllaWVB0MmVhanVXZE5zRGo")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var ecopath = builder.AddContainer("ecopath", "rikkert242/surimiecopath", "latest")
    .WithHttpEndpoint(port: 7890, targetPort: 7890, name: "ecopath")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIDQ2Gadjlo16vHLjLp8pqhKrv_QOGnMwU3UJfyJoQGR5Gh4KHGh2cy5FQ3ZYU0g3YllaWVB0MmVhanVXZE5zRGo test")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var environment = builder.AddContainer("environment", "rikkert242/surimienvironment", "latest")
    .WithHttpEndpoint(port: 5839, targetPort: 5839, name: "environment");

var fisheriesAuthority = builder.AddContainer("fisheriesauthority", "rikkert242/surimifisheriesauthority", "latest")
    .WithHttpEndpoint(port: 5493, targetPort: 5493, name: "fisheriesauthority");

var valueChain = builder.AddContainer("valuechain", "rikkert242/surimivaluechain", "latest")
    .WithHttpEndpoint(port: 7990, targetPort: 7990, name: "valuechain");

var surimicontroller = builder.AddProject<Projects.SURIMI_controller>("surimicontroller")
//    .WithReference(poseidon)                                      // Strange that you can't use this reference here...
//    .WithReference(ecopath)
    .WithEnvironment("POSEIDON_URL", "http://localhost:50051")      // I don't know why you can't use http://poseidon:50051 in this place... but this also works
    .WithEnvironment("MARKET_URL", "http://localhost:5001")         // idem. Didn't test
    .WithEnvironment("CMSY_URL", "http://localhost:5021")
    .WithEnvironment("AGGREGATOR_URL", "http://localhost:5188")
    .WithEnvironment("ECOPATH_URL", "http://localhost:7890")
    .WithEnvironment("VALUECHAIN_URL", "http://localhost:7990")
    .WithEnvironment("ENVIRONMENT_URL", "http://localhost:5839")
    .WithEnvironment("FISHERIES_AUTHORITY_URL", "http://localhost:5493")

    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIDQ2Gadjlo16vHLjLp8pqhKrv_QOGnMwU3UJfyJoQGR5Gh4KHGh2cy5FQ3ZYU0g3YllaWVB0MmVhanVXZE5zRGo test")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

builder.AddProject<Projects.SURIMI_gui>("surimigui")
    .WithReference(surimicontroller)
    .WithEnvironment("CONTROLLER_URL", "http://surimicontroller:5092")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIDQ2Gadjlo16vHLjLp8pqhKrv_QOGnMwU3UJfyJoQGR5Gh4KHGh2cy5FQ3ZYU0g3YllaWVB0MmVhanVXZE5zRGo")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "edito-datalab-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv");

builder.Build().Run();