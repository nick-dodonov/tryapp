using Microsoft.AspNetCore.Mvc;
using Docker.DotNet;
using Locator;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DockerConfig>(builder.Configuration.GetSection(nameof(DockerConfig)));
builder.Services.AddSingleton<DockerClient>(sp => CreateDockerConfiguration(
        sp.GetRequiredService<IOptions<DockerConfig>>().Value,
        sp.GetRequiredService<ILogger<DockerClient>>())
    .CreateClient());
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/stands", async ([FromServices] DockerClient dockerClient) =>
{
    var containers = await dockerClient.Containers.ListContainersAsync(new() { All = true });
    var stands = containers
        .Where(c => c.State == "running")
        .SelectMany(c => c.Labels)
        .Where(l => l.Key == "com.docker.stack.namespace" && l.Value.StartsWith("stand-"))
        .Select(l => l.Value)
        .ToList();
    
    return Results.Json(new { stands });
});

app.UseCors();

app.Run();
return;

DockerClientConfiguration CreateDockerConfiguration(DockerConfig config, ILogger logger)
{
    var result = CreateDockerConfigurationExt(config);
    logger.LogInformation($"Docker: {result.configuration.EndpointBaseUri} ({result.reason})");
    return result.configuration;
}

(DockerClientConfiguration configuration, string reason) CreateDockerConfigurationExt(DockerConfig config)
{
    var url = config.Url;
    if (url == null)
        return (new(), "default");
    if (Path.Exists(url))
        url = $"unix://{Path.GetFullPath(url)}";
    return (new(new Uri(url)), "config");
}