IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var poseidon = builder.AddContainer("poseidon", "nicolaspayette/poseidon", "latest")
    .WithHttpEndpoint( port: 50051, targetPort: 50051, name: "poseidon");

var market = builder.AddContainer("market", "rikkert242/market", "latest")
    .WithHttpEndpoint(port: 5001, targetPort: 5001, name: "market");

var cmsy = builder.AddContainer("cmsy", "rikkert242/cmsy", "latest")
    .WithHttpEndpoint(port: 5020, targetPort: 5020, name: "cmsy")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIMV5bohxF07ApBj29lMLK7HwkH-DoB8dxV2JOHpTonKEGh4KHGh2cy5Ca3hHdmRuNTlKT3hBcHhRTU5zUlVqM0E")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");
;

var aggregator = builder.AddContainer("aggregator", "rikkert242/aggregator", "latest")
    .WithHttpEndpoint(port: 5188, targetPort: 5188, name: "aggregator")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIMV5bohxF07ApBj29lMLK7HwkH-DoB8dxV2JOHpTonKEGh4KHGh2cy5Ca3hHdmRuNTlKT3hBcHhRTU5zUlVqM0E")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var ecopath = builder.AddContainer("ecopath", "rikkert242/ecopath", "latest")
    .WithHttpEndpoint(port: 7890, targetPort: 8080, name: "ecopath")
    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIMV5bohxF07ApBj29lMLK7HwkH-DoB8dxV2JOHpTonKEGh4KHGh2cy5Ca3hHdmRuNTlKT3hBcHhRTU5zUlVqM0E")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

var valueChain = builder.AddContainer("valuechain", "rikkert242/surimivaluechain", "latest")
    .WithHttpEndpoint(port: 7990, targetPort: 8080, name: "valuechain");

var surimicontroller = builder.AddProject<Projects.SurimiController>("surimicontroller")
//    .WithReference(poseidon)                                      // Strange that you can't use this reference here...
//    .WithReference(ecopath)
    .WithEnvironment("POSEIDON_URL", "http://localhost:50051")      // I don't know why you can't use http://poseidon:50051 in this place... but this also works
    .WithEnvironment("MARKET_URL", "http://localhost:5001")         // idem. Didn't test
    .WithEnvironment("CMSY_URL", "http://localhost:5020")
    .WithEnvironment("AGGREGATOR_URL", "http://localhost:5188")
    .WithEnvironment("ECOPATH_URL", "http://localhost:7890")
    .WithEnvironment("VALUECHAIN_URL", "http://localhost:7990")

    .WithEnvironment("VAULT_ADDR", "https://vault.dive.edito.eu")
    .WithEnvironment("VAULT_TOKEN", "hvs.CAESIMV5bohxF07ApBj29lMLK7HwkH-DoB8dxV2JOHpTonKEGh4KHGh2cy5Ca3hHdmRuNTlKT3hBcHhRTU5zUlVqM0E")
    .WithEnvironment("VAULT_TOP_DIR", "rikkert")
    .WithEnvironment("VAULT_RELATIVE_PATH", "s3-credentials")
    .WithEnvironment("VAULT_MOUNT", "secret-kv")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

builder.AddProject<Projects.SurimiGUI>("surimigui")
    .WithReference(surimicontroller)
    .WithEnvironment("CONTROLLER_URL", "http://surimicontroller:8080");

builder.Build().Run();