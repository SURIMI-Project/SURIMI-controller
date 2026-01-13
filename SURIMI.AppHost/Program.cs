IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var poseidon = builder.AddContainer("poseidon", "nicolaspayette/poseidon", "latest")
    .WithHttpEndpoint( port: 50051, targetPort: 50051, name: "poseidon");

var market = builder.AddContainer("market", "rikkert242/market", "latest")
    .WithHttpEndpoint(port: 5001, targetPort: 5001, name: "market");

//var cmsy = builder.AddContainer("cmsy", "rikkert242/cmsy", "latest")
//    .WithHttpEndpoint(port: 5020, targetPort: 5020, name: "cmsy");

//var ecopath = builder.AddProject<Projects.Ecopath>("ecopath")
//    .WithReplicas(5);

var ecopath = builder.AddContainer("ecopath", "rikkert242/ecopath", "latest")
    .WithHttpEndpoint(port: 7890, targetPort: 8080, name: "ecopath");

//var valueChain = builder.AddContainer("valuechain", "rikkert242/surimivaluechain", "latest")
//    .WithHttpEndpoint(port: 7990, targetPort: 8080, name: "valuechain");

var surimicontroller = builder.AddProject<Projects.SurimiController>("surimicontroller")
//    .WithReference(poseidon)                                      // Strange that you can't use this reference here...
//    .WithReference(ecopath)
    .WithEnvironment("POSEIDON_URL", "http://localhost:50051")      // I don't know why you can't use http://poseidon:50051 in this place... but this also works
    .WithEnvironment("MARKET_URL", "http://localhost:5001")         // idem. Didn't test
    .WithEnvironment("CMSY_URL", "http://localhost:5020")
    .WithEnvironment("ECOPATH_URL", "http://localhost:7890")
    .WithEnvironment("VALUECHAIN_URL", "http://localhost:7990")

    .WithEnvironment("AWS_ACCESS_KEY_ID", "9P0NGCEM974A6BYR24E2")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "6bUuUksBms7QGXA+OHvbu1VIH+ViUmbSfyJVXVEq")
    .WithEnvironment("AWS_SESSION_TOKEN", "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJhY2Nlc3NLZXkiOiI5UDBOR0NFTTk3NEE2QllSMjRFMiIsImFjciI6IjAiLCJhbGxvd2VkLW9yaWdpbnMiOlsiKiJdLCJhdWQiOlsibWluaW8iLCJhY2NvdW50Il0sImF1dGhfdGltZSI6MTc2NTg5MjQyOCwiYXpwIjoib255eGlhLW1pbmlvIiwiZW1haWwiOiJyaWsua3JlZWZ0ZW5iZXJnQHNwaW5zb2Z0Lm5sIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsImV4cCI6MTc2NjAwNjkwMiwiZmFtaWx5X25hbWUiOiJLcmVlZnRlbmJlcmciLCJnaXZlbl9uYW1lIjoiUmlrIiwiZ3JvdXBzIjpbIkVESVRPX1VTRVIiLCJzdXJpbWkiXSwiaWF0IjoxNzY1OTIwNTAyLCJpc3MiOiJodHRwczovL2F1dGguZGl2ZS5lZGl0by5ldS9hdXRoL3JlYWxtcy9kYXRhbGFiIiwianRpIjoiZGM1NWRmNjItN2NiYi00NDA0LTk2YTAtZTg2Y2QxMzUxNTEzIiwibmFtZSI6IlJpayBLcmVlZnRlbmJlcmciLCJwb2xpY3kiOiJzdHNvbmx5IiwicHJlZmVycmVkX3VzZXJuYW1lIjoicmlra2VydCIsInJlYWxtX2FjY2VzcyI6eyJyb2xlcyI6WyJkZWZhdWx0LXJvbGVzLWRhdGFsYWIiLCJvZmZsaW5lX2FjY2VzcyIsInVtYV9hdXRob3JpemF0aW9uIl19LCJyZXNvdXJjZV9hY2Nlc3MiOnsiYWNjb3VudCI6eyJyb2xlcyI6WyJtYW5hZ2UtYWNjb3VudCIsIm1hbmFnZS1hY2NvdW50LWxpbmtzIiwidmlldy1wcm9maWxlIl19LCJtaW5pbyI6eyJyb2xlcyI6WyJzdHNvbmx5Il19fSwic2NvcGUiOiJvcGVuaWQgZW1haWwgcHJvZmlsZSIsInNlc3Npb25fc3RhdGUiOiIwYTA4YmJlNy1iODZkLTQzNjQtYjBmZC01ZDM0ZDU5NGQ5NzAiLCJzaWQiOiIwYTA4YmJlNy1iODZkLTQzNjQtYjBmZC01ZDM0ZDU5NGQ5NzAiLCJzdWIiOiIzMTJmZDE4MC1jYzVjLTQ1YmQtYTY4OS1hNDBjMDI4NmJjYjQiLCJ0eXAiOiJCZWFyZXIifQ.lPnAiSeBUyo6Oek8CXX54Ej0_BnEMf9bvQsvm17XZ_Gc8QTTQ-fx8KWV_nquWfrs__llegSVJF-2pEjnWckSPw")
    .WithEnvironment("AWS_S3_ENDPOINT", "minio.dive.edito.eu")
    .WithEnvironment("AWS_DEFAULT_REGION", "waw3-1")
    .WithEnvironment("AWS_BUCKET_NAME", "oidc-rikkert");

builder.AddProject<Projects.SurimiGUI>("surimigui")
    .WithReference(surimicontroller)
    .WithEnvironment("CONTROLLER_URL", "http://surimicontroller:8080");

builder.Build().Run();