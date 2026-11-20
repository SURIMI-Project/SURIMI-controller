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

CMSY experiment initialization has no fixed controller-side deadline because it can take more than 10 minutes when all services run together. The call retains the experiment cancellation token, and simulations wait for initialization to complete before running.

## SURIMI-only monitoring on Onyxia

The manifests in [`deploy/surimi-monitoring`](deploy/surimi-monitoring) install Grafana OSS, Prometheus and a Kubernetes Metrics API exporter, and Loki with Alloy for container logs. They create only namespaced resources. Both collectors select `app.kubernetes.io/instance=surimi-372040` in `user-rikkert`; neither Jupyter nor Jaeger matches. If Onyxia relaunches SURIMI with a new instance label, update the selector in `kustomization.yaml` and reapply. Historical data from the old SURIMI release remains until retention expires.

Copy the `deploy/surimi-monitoring` directory into your Onyxia workspace or use a checkout containing it. From that checkout, run in Bash:

```bash
NS=user-rikkert
kubectl -n "$NS" top pods -l 'app.kubernetes.io/instance=surimi-372040' --containers
openssl rand -hex 24 | tr -d '\n' | kubectl -n "$NS" create secret generic surimi-monitoring-grafana --from-file=admin-password=/dev/stdin
kubectl apply -k deploy/surimi-monitoring
kubectl -n "$NS" rollout status deployment/surimi-monitoring --timeout=5m
```

Create the Secret only on first install. On subsequent updates, reapply the manifests without replacing it. The 5 GiB volume uses the default StorageClass and preserves data across monitoring pod restarts. If the PVC stays `Pending` or the deployment is rejected, check `kubectl -n "$NS" describe pvc surimi-monitoring` and `kubectl -n "$NS" get events --sort-by=.lastTimestamp`. Kubernetes can reject a Role grant even when `auth can-i create roles` says `yes` if the installer does not already hold the requested permissions.

Get the password in a private terminal with `kubectl -n "$NS" get secret surimi-monitoring-grafana -o jsonpath='{.data.admin-password}' | base64 -d; echo`. From a machine with authorized Kubernetes access and your browser, run `kubectl -n user-rikkert port-forward deployment/surimi-monitoring 3000:3000`. Open <http://localhost:3000/d/surimi-workloads> and sign in as `admin`. A port-forward started **inside** a remote notebook listens there, not on your workstation; if your only access is through Jupyter, use an Onyxia-approved private proxy or authenticated ingress rather than exposing the dashboard publicly. Prometheus, Loki and Alloy listen only on loopback inside the monitoring pod.

After a minute, check that **SURIMI workloads** shows CPU and memory only for the selected pods. In Grafana Explore, `up{job="surimi-pod-metrics"}` should be `1`; in Loki, use `{pod=~"surimi-controller-.+"}` to check for controller logs during a run. If data is missing, inspect the `metrics-exporter` and `alloy` container logs in `deployment/surimi-monitoring`. The Metrics API provides recent samples, not precise accounting; Prometheus keeps up to seven days or its configured size limit, and Loki targets three days of logs. Monitor volume usage if the simulation emits a high volume of logs. Jaeger is unchanged.

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

