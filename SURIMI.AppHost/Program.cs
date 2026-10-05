IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var vaultToken = builder.Configuration["VaultToken"]
    ?? throw new InvalidOperationException("Missing configuration value 'VaultToken'. Set it via 'dotnet user-secrets set \"VaultToken\" \"<token>\"' in SURIMI.AppHost.");

var poseidon = builder.AddContainer("poseidon", "ghcr.io/surimi-project/surimiposeidon", "latest")
    .WithHttpEndpoint( port: 50051, targetPort: 50051, name: "poseidon");

var market = builder.AddContainer("market", "ghcr.io/official-ewe/surimimarket", "latest")
    .WithHttpEndpoint(port: 5001, targetPort: 5001, name: "market")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", vaultToken)
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var cmsy = builder.AddContainer("cmsy", "ghcr.io/surimi-project/surimicmsy", "latest")
    .WithHttpEndpoint(port: 5021, targetPort: 5021, name: "cmsy")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", vaultToken)
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var outputCreator = builder.AddContainer("outputcreator", "ghcr.io/surimi-project/surimioutputcreator", "latest")
    .WithHttpEndpoint(port: 5189, targetPort: 5189, name: "outputcreator")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", vaultToken)
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var ecopath = builder.AddContainer("ecopath", "ghcr.io/surimi-project/surimiecopath", "latest")
    .WithHttpEndpoint(port: 7890, targetPort: 7890, name: "ecopath")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", vaultToken)
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var fisheriesAuthority = builder.AddContainer("fisheriesauthority", "ghcr.io/surimi-project/surimifisheriesauthority", "latest")
    .WithHttpEndpoint(port: 5493, targetPort: 5493, name: "fisheriesauthority")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", vaultToken)
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var valueChain = builder.AddContainer("valuechain", "ghcr.io/surimi-project/surimivaluechain", "latest")
    .WithHttpEndpoint(port: 7990, targetPort: 7990, name: "valuechain");

var surimicontroller = builder.AddProject<Projects.SURIMI_controller>("surimicontroller")
//    .WithReference(poseidon)                                      // Strange that you can't use this reference here...
//    .WithReference(ecopath)
    .WithEnvironment("POSEIDON_URL", "http://localhost:50051")      // I don't know why you can't use http://poseidon:50051 in this place... but this also works
    .WithEnvironment("MARKET_URL", "http://localhost:5001")         // idem. Didn't test
    .WithEnvironment("CMSY_URL", "http://localhost:5021")
    .WithEnvironment("OUTPUT_CREATOR_URL", "http://localhost:5189")
    .WithEnvironment("ECOPATH_URL", "http://localhost:7890")
    .WithEnvironment("VALUECHAIN_URL", "http://localhost:7990")
    .WithEnvironment("FISHERIES_AUTHORITY_URL", "http://localhost:5493")

    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", vaultToken)
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
    .WithEnvironment("VAULT_TOKEN", vaultToken)
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "edito-datalab-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv");

builder.Build().Run();