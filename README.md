# SURIMI

## Build Docker image

The Surimi-controller docker image will be built and pushed to the GitHub Container Registry (ghcr.io) as part of the build process. 

The Surimi-gui docker image will NOT be built and pushed!!!

To build and push the Surimi-GUI docker image, run the following command in the terminal:

```bash
PS C:\Users\<user>\source\repos\SURIMI-controller\surimi-gui> dotnet build /t:BuildPushDockerImage -v:detailed
```