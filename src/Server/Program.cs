using Server.Meta;
using Server.Rtc;
using Shared;
using Shared.Meta.Api;
using Shared.Rtc;

StaticLog.Info("==== starting server ====");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//TODO: add custom console formatter with category recolor to simplify debug
//  https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
builder.Services
    .AddSingleton<IMeta, MetaServer>()
    .AddSingleton<IRtcService, SipRtcService>()
    .AddHostedService<SipRtcService>()
    ;
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.IncludeFields = true;
        //to correctly serialize RTCSessionDescriptionInit.type to handle in .js
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Adding CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() // Allow all origins
            .AllowAnyMethod() // Allow all HTTP methods (GET, POST, etc.)
            .AllowAnyHeader(); // Allow all headers
    });
});

var app = builder.Build();
app.Logger.LogInformation("Starting server (TODO: build version)");

// Configure the HTTP request pipeline.
app.UseAuthorization();
// Apply CORS middleware
app.UseCors();

app.MapControllers();

app.Run();
