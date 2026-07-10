# SURIMI controller

The SURIMI controller is the main component of the SURIMI system, responsible for managing and orchestrating the various services and components within the system. 

It's written in C# and .NET 10.

## Solution structure
The solution contains the following projects:
- SURIMI.AppHost: The Aspire application host project, which launches all necessary services and `simulates` the Kubernetes cluster.
- SURIMI.Controller: The main controller project, which contains the core logic and functionality of the SURIMI system.
- SURIMI.Conroller.Tests: The test project for the SURIMI.Controller, which contains unit tests and integration tests to ensure the correctness of the controller's behavior.
- SURIMI.Gui: The graphical user interface project, which provides a user-friendly interface for interacting with the SURIMI system.

## Simulation Architecture
[See detailed documentation](docs/simulation-architecture.md)

## Build Docker image

The Surimi-controller docker image will be built and pushed to the GitHub Container Registry (ghcr.io) as part of the CI/CD build process. 

The Surimi-gui docker image will NOT be built and pushed!!!

To build and push the Surimi-GUI docker image, run the following command in the terminal:

```bash
PS C:\Users\<user>\source\repos\SURIMI-controller\surimi-gui> dotnet build /t:BuildPushDockerImage -v:detailed
```