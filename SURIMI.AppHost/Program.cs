IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var poseidon = builder.AddContainer("poseidon", "nicolaspayette/poseidon", "latest")
    .WithHttpEndpoint( port: 50051, targetPort: 50051, name: "poseidon");

var ecopath = builder.AddProject<Projects.Ecopath>("ecopath");

var surimicontroller = builder.AddProject<Projects.SurimiController>("surimicontroller")
//    .WithReference(poseidon)                                      // Strange that you can't use this reference here...
    .WithReference(ecopath)
    .WithEnvironment("POSEIDON_URL", "http://localhost:50051")      // I don't know why you can't use http://poseidon:50051 in this place... but this also works
    .WithEnvironment("ECOPATH_URL", "http://ecopath:8080");

builder.AddProject<Projects.SurimiGUI>("surimigui")
    .WithReference(surimicontroller)
    .WithEnvironment("CONTROLLER_URL", "http://surimicontroller:8080");

builder.Build().Run();