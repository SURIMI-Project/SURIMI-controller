IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var poseidon = builder.AddContainer("poseidon", "nicolaspayette/poseidon", "latest")
    .WithHttpEndpoint( port: 50051, targetPort: 50051, name: "poseidon");

var market = builder.AddContainer("market", "rikkert242/market", "latest")
    .WithHttpEndpoint(port: 5001, targetPort: 5001, name: "market");

var cmsy = builder.AddContainer("cmsy", "rikkert242/cmsy", "latest")
    .WithHttpEndpoint(port: 5020, targetPort: 5020, name: "cmsy");

//var ecopath = builder.AddProject<Projects.Ecopath>("ecopath")
//    .WithReplicas(5);

var ecopath = builder.AddContainer("ecopath", "rikkert242/ecopath", "latest")
    .WithHttpEndpoint(port: 7890, targetPort: 8080, name: "ecopath");

var valueChain = builder.AddContainer("valuechain", "rikkert242/surimivaluechain", "latest")
    .WithHttpEndpoint(port: 7990, targetPort: 8080, name: "valuechain");

var surimicontroller = builder.AddProject<Projects.SurimiController>("surimicontroller")
//    .WithReference(poseidon)                                      // Strange that you can't use this reference here...
//    .WithReference(ecopath)
    .WithEnvironment("POSEIDON_URL", "http://localhost:50051")      // I don't know why you can't use http://poseidon:50051 in this place... but this also works
    .WithEnvironment("MARKET_URL", "http://localhost:5001")         // idem. Didn't test
    .WithEnvironment("CMSY_URL", "http://localhost:5020")
    .WithEnvironment("ECOPATH_URL", "http://localhost:7890")
    .WithEnvironment("VALUECHAIN_URL", "http://localhost:7990");

builder.AddProject<Projects.SurimiGUI>("surimigui")
    .WithReference(surimicontroller)
    .WithEnvironment("CONTROLLER_URL", "http://surimicontroller:8080");

builder.Build().Run();