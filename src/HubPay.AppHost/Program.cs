var builder = DistributedApplication.CreateBuilder(args);

//var postgres = builder.AddPostgres("postgres")
//    .WithDataVolume("hubpay-postgres-data")
//    .AddDatabase("hubpay");

//var redis = builder.AddRedis("redis")
//    .WithDataVolume("hubpay-redis-data");

var webapi = builder.AddProject<Projects.HubPay_WebApi>("webapi")
    //.WithReference(postgres)
    //.WithReference(redis)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithEnvironment("HubPay__EnableSwagger", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var blazor = builder.AddProject<Projects.HubPay_Frontend_Blazor>("blazor")
    .WithReference(webapi)
    .WithExternalHttpEndpoints()
    .WaitFor(webapi);

blazor.WithEnvironment("ApiBaseUrl", webapi.GetEndpoint("https"));

builder.Build().Run();
