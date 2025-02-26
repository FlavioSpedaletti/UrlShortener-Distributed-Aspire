var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .AddDatabase("url-shortener");

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.UrlShorting_Api>("urlshorting-api")
        .WithReference(postgres)
        .WithReference(redis)
        .WaitFor(postgres)
        .WaitFor(redis);

builder.Build().Run();
