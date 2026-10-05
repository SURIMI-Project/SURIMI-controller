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

### Secrets
The GitHubToken and the BsrToken are used by this script as secrets. Add them to your secrets if they aren't already there and you get errors like:

```
warning : Your request could not be authenticated by the GitHub Packages service. Please ensure your access token is valid and has the appropriate scopes configured
```

And add the secrets using the following commands:
```bash
PS C:\Users\<user>\source\repos\SURIMI-controller\surimi-gui> dotnet user-secrets set "DockerBuild:GitHubToken" "<github token>"
PS C:\Users\<user>\source\repos\SURIMI-controller\surimi-gui> dotnet user-secrets set "DockerBuild:BsrToken" "<bsr token>"
```

You can see the secrets by right-clicking on the csproj file and selecting "Manage User Secrets". This will open a secrets.json file where you can view and edit your secrets.

If you still get the authentication error, clear the cache with:
```bash
docker builder prune -f
```
Then try to build and push the docker image again.


# Copy the configuration files from local to the S3 bucket

* go to the Developer Powershell
* Check if an alias is present:
`& "C:\Program Files\MinioClient\mc.exe" alias list`
* Add the alias with the following command:
`& "C:\Program Files\MinioClient\mc.exe" alias set surimi https://s3.waw3-1.cloudferro.com 78540c1dda814532a000c8430c9f2558 "YOUR_SECRET_ACCESS_KEY"`

* run the following command to copy the configuration files from your local machine to the S3 bucket:

```powershell
PS C:\Users\<user>\source\repos\SURIMI-controller> .\Sync_S3_Bucket.ps1
```

* The script will prompt you for the bucketname

