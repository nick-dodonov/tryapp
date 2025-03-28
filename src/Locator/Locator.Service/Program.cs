using Docker.DotNet;
using Locator.Api;
using Locator.Service;
using Locator.Service.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.DefaultConfigure<LocatorConfig>(builder.Configuration);
builder.Services.Configure<DockerConfig>(builder.Configuration.GetSection(nameof(DockerConfig)));
builder.Services.AddSingleton<DockerClient>(sp => CreateDockerConfiguration(
        sp.GetRequiredService<IOptions<DockerConfig>>().Value,
        sp.GetRequiredService<ILogger<DockerClient>>())
    .CreateClient());
builder.Services.AddSingleton<ILocator, DockerLocator>();
builder.Services.AddCors(options => options.AddDefaultPolicy(
    policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var factory = LoggerFactory.Create(
    builder => builder.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "[HH:mm:ss.ffffff] ";
        o.UseUtcTimestamp = true;
    }));

var app = builder.Build();

app.UseCors();
app.MapControllers();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapGet("/info", () => Task.FromResult("TODO: version"));

app.Run();
return;

static DockerClientConfiguration CreateDockerConfiguration(DockerConfig config, ILogger logger)
{
    var result = CreateDockerConfigurationExt(config);
    logger.LogInformation($"Docker: {result.configuration.EndpointBaseUri} ({result.reason})");
    return result.configuration;
}

static (DockerClientConfiguration configuration, string reason) CreateDockerConfigurationExt(DockerConfig config)
{
    var url = config.Url;
    if (string.IsNullOrEmpty(url))
        return (new(), "default");
    if (Path.Exists(url))
        url = $"unix://{Path.GetFullPath(url)}";
    return (new(new Uri(url)), "config");
}