# Running in Docker Compose

In addition to its availability as a .NET CLI tool, the `dotnet monitor` tool is available as a prebuilt Docker image that can be run in container runtimes and orchestrators, such as Docker Compose.

For Dockerfiles and repository information, see [Running in Docker](./docker.md)

For a Kubernetes sample, see [Running in Kubernetes](./kubernetes.md)

## Non-root considerations

Starting with .NET 8.0, both the sample ASP.NET application and dotnet-monitor run as non-root. If both the application and dotnet-monitor are 8+, no additional configuration is required. Otherwise, a [user](https://docs.docker.com/reference/compose-file/services/#user) configuration may need to be added to the application, dotnet-monitor, or both.

## Example Deployment

The [Docker Compose](../samples/Docker/compose.yaml) example demonstrates a deployment of the .NET Monitor 8 container image monitoring an application container as services.

## Example Details

* __Listen Mode__: The `dotnet monitor` tool is configured to run in `listen` mode. The tool establishes a diagnostic communication channel at the specified Unix Domain Socket path by the `DOTNETMONITOR_DiagnosticPort__EndpointName` environment variable. The application container has a `DOTNET_DiagnosticPorts` environment variable specified so that the application's runtime will communicate with the `dotnet monitor` instance at the specified Unix Domain Socket path. The application runtime will be suspended (e.g. no managed code execution) until it establishes communication with `dotnet monitor`. Application startup time will depend on how long it takes for the `dotnet monitor` container to run, but this should be quick.
> **7.0+**: Setting `DOTNETMONITOR_DiagnosticPort__EndpointName` is not necessary if (1) `DOTNETMONITOR_Storage__DefaultSharedPath` is set and (2) `DOTNETMONITOR_DiagnosticPort__ConnectionMode` is set to `Listen`; the combination of these settings automatically creates a Unix Domain Socket named `dotnet-monitor.sock` under the default shared path.  The `DOTNETMONITOR_DiagnosticPort__EndpointName` setting is still available to use if the default behavior needs to be overridden.
* __Multiple Application Containers__: The `dotnet monitor` tool is capable of monitoring multiple processes at the same time. When running in `listen` mode, each of the applications can connect to the same communication channel path in order to allow `dotnet monitor` to observe them.
* __Dumps Temporary Folder__: The `dotnet monitor` tool is configured to instruct the application runtime to produce dump files at the path specified by the `DOTNETMONITOR_Storage__DumpTempFolder` environment variable. Without specifying this variable, the application runtime will create dump files in the `/tmp` directory of its container, which is not accessible by the `dotnet monitor` container in this example.
> **7.0+**: Setting `DOTNETMONITOR_Storage__DumpTempFolder` is not necessary if `DOTNETMONITOR_Storage__DefaultSharedPath` is set; dumps will be produced in a subfolder under the specified shared path. The `DOTNETMONITOR_Storage__DumpTempFolder` setting is still available to use if the default behavior needs to be overridden.
* __Disabled Authentication__: By default, the `dotnet monitor` tool requires authentication for any of the URLs specified by the `--urls` command line parameter or configured via the `ASPNETCORE_Urls`/`DOTNETMONITOR_Urls` environment variables (see [Authentication](./authentication.md) for further details). For the purposes of this example, authentication has been disabled by using the `--no-auth` command line switch. __Use of this command line switch is NOT recommended for production scenarios__ as it would allow anything with access to the URLs to be able to capture dumps, traces, etc (which may contain sensitive information, such as keys) of any process that `dotnet monitor` is able to monitor.
* __Disabled HTTPS__: By default, the `dotnet monitor` tool has HTTPS enabled on the default artifact URLs, which are configured to be `https://+:52323` in the Docker image. For the purposes of this example, the artifact URLs have been changed to `http://localhost:52323` using the `DOTNETMONITOR_Urls` environment variable such that a TLS certificate is not required. __Disabling HTTPS is NOT recommended for production scenarios.__
* __Command Line Arguments__: Starting in 7.0, the default command line arguments for the `dotnet monitor` tool have been moved to the CMD instruction of the Docker image (previously, they were part of the ENTRYPOINT instruction). If the arguments of the image are overridden, then the default arguments must be explicitly specified in the overridden setting or set via configuration. See [7.0 Compatibility](compatibility/7.0/README.md#docker-container-entrypoint-split) for details.
* __Default Shared Path__: Starting in 7.0, setting the default shared path instructs the `dotnet monitor` tool to automatically share all artifacts (dumps, diagnostic ports, shared libraries, etc) with the target applications in the specified path. This setting simplifies configuration so that a setting for each feature that contains sharable information does not need to necessarily be specified.

## Recommended container limits

The following are the recommended memory and CPU minimums and limits for the `dotnet monitor` container.

```yaml
resources:
  reservations:
    cpus: '0.050'
    memory: 50M
  limits:
    cpus: '0.250'
    memory: 256M
```

How much memory and CPU is consumed by dotnet-monitor is dependent on which scenarios are being executed:
- Metrics consume a negligible amount of resources, although using custom metrics can affect this.
- Operations such as traces and logs may require memory in the main application container that will automatically be allocated by the runtime.
- Resource consumption by trace operations is also dependent on which providers are enabled, as well as the [buffer size](./api/definitions.md#eventprovidersconfiguration) allocated in the runtime.
- It is not recommended to use highly verbose [log levels](./api/definitions.md#loglevel) while under load. This causes a lot of CPU usage in the dotnet-monitor container and more memory pressure in the main application container.
- Dumps also temporarily increase the amount of memory consumed by the application container.
