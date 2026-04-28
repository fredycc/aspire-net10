using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("postgres-password", "postgres");

IResourceBuilder<PostgresDatabaseResource> database = builder
    .AddPostgres("database")
    .WithImage("postgres:17")
    .WithBindMount("../../.containers/db/init.sql", "/docker-entrypoint-initdb.d/init.sql")
    .WithDataVolume()
    .WithPassword(postgresPassword)
    .AddDatabase("clean-architecture");

if (!builder.Environment.IsDevelopment())
{
    IResourceBuilder<ContainerResource> loki = builder.AddContainer("loki", "grafana/loki:3.7.0")
        .WithEndpoint(3100, 3100, name: "otlp")
        .WithVolume("loki-data", "/var/loki")
        .WithEnvironment("ACCEPT_DATABASE_LOCK", "true");

    // Grafana datasource config — env var name is misleading (not LDAP), see https://grafana.com/docs/grafana/latest/setup-grafana/configure-grafana/feature-toggles/
    IResourceBuilder<ContainerResource> grafana = builder.AddContainer("grafana", "grafana/grafana:13.0.1")
        .WithEndpoint(3000, 3000)
        .WithVolume("grafana-data", "/var/lib/grafana")
        .WithEnvironment("GF_DATASOURCES_LDAP_SECRET_JSON", @"{""datasources"":[{""name"":""Loki"",""type"":""loki"",""access"":""proxy"",""url"":""http://loki:3100"",""isDefault"":""true""}]}");

    builder.AddProject<Projects.Web_Api>("web-api")
        .WithEnvironment("ConnectionStrings__Database", database)
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://loki:3100")
        .WithReference(database)
        .WaitFor(database)
        .WaitFor(loki)
        .WaitFor(grafana);
}
else
{
    builder.AddProject<Projects.Web_Api>("web-api")
        .WithEnvironment("ConnectionStrings__Database", database)
        .WithReference(database)
        .WaitFor(database);
}

builder.Build().Run();
