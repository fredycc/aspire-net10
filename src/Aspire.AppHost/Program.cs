IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> database = builder
    .AddPostgres("database")
    .WithImage("postgres:17")
    .WithBindMount("../../.containers/db", "/var/lib/postgresql/data")
    .AddDatabase("clean-architecture");

IResourceBuilder<SeqResource> seq = builder.AddSeq("seq")
    .WithEndpoint(5341, 5341);

builder.AddProject<Projects.Web_Api>("web-api")
    .WithEnvironment("ConnectionStrings__Database", database)
    .WithReference(database)
    .WithReference(seq)
    .WaitFor(database)
    .WaitFor(seq);

builder.Build().Run();
