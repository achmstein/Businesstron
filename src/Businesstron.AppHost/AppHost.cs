using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

// Public hostname the deployed site answers on (Caddy vhost + VITE_API_URL).
// Override via AppHost configuration/user-secrets: "PublicHost".
// Trim so a stray newline in the PublicHost config/variable can't corrupt the
// Caddy vhost label (which would silently break routing to the site).
var publicHost = builder.Configuration["PublicHost"] ?? "businesstron.nighttax.com.au";

builder.AddDockerComposeEnvironment("businesstron-compose")
    // No Aspire dashboard in the deployed compose: this is a shared host and the
    // dashboard's host port (18888) collides with the other stack (Renewtron). The
    // app doesn't need it; local `aspire run` still gets the normal dev dashboard.
    .WithDashboard(false)
    .ConfigureComposeFile(compose =>
    {
        // The host's caddy-docker-proxy lives on an external `caddy` network.
        compose.AddNetwork(new Network
        {
            Name = "caddy",
            External = true
        });

        // Persists UI-entered integration credentials (settings.overrides.json)
        // across redeploys. The server mounts this at /data (see below).
        compose.AddVolume(new Volume
        {
            Name = "businesstron-data",
            Driver = "local"
        });
    });

var pgPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: pgPassword)
    .WithDataVolume("businesstron-pg-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");

var db = postgres.AddDatabase("BusinesstronDb");

// Single container: the .NET server hosts the API AND the built React SPA (wwwroot).
// The host's caddy-docker-proxy routes the public host straight to it.
var server = builder.AddProject<Projects.Businesstron_Web>("businesstron-server")
    .WithReference(db)
    .WaitFor(postgres)
    // Ontraport + 2Captcha credentials are now entered from the Settings UI and
    // written to the overrides file on the persistent /data volume (below) — no
    // longer injected as build/deploy secrets.
    .WithEnvironment("Storage__OverridesPath", "/data/settings.overrides.json")
    .WithHttpHealthCheck("/health")
    .PublishAsDockerFile()
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Restart = "unless-stopped";
        service.Ports.Clear();
        service.Networks.Add("caddy");
        // Mount the named volume so UI-entered credentials survive redeploys, and
        // run as root so the app can write to it (the base image's user varies).
        service.User = "root";
        service.AddVolume(new Volume
        {
            Name = "businesstron-data",
            Type = "volume",
            Source = "businesstron-data",
            Target = "/data"
        });
        service.Labels["caddy"] = publicHost;
        service.Labels["caddy.reverse_proxy"] = "{{upstreams 8080}}";
    });

if (builder.ExecutionContext.IsRunMode)
{
    // Local dev: Vite dev server with HMR; WithReference injects the /api proxy target.
    builder.AddViteApp("webfrontend", "../Businesstron.Web/ClientApp")
        .WithReference(server)
        .WaitFor(server);
}

builder.Build().Run();
