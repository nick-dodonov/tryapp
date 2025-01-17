using Server.Info;
using Server.Logic;
using Server.Meta;
using Shared.Log;
using Shared.Meta.Api;
using Shared.Tp;
using Shared.Tp.Rtc;
using Shared.Tp.Rtc.Sip;

Slog.Info($"==== starting server build {BuildInfo.Timestamp} ====");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//TODO: add custom console formatter with category recolor to simplify debug
//  https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
builder.Services
    .AddSingleton<IMeta, MetaServer>()
    .AddSingleton<SipRtcService>()
    .AddSingleton<IRtcService>(sp => sp.GetRequiredService<SipRtcService>())
    .AddSingleton<ITpApi>(sp => sp.GetRequiredService<SipRtcService>())
    .AddHostedService<SipRtcService>()
    .AddSingleton<LogicSession>()
    .AddHostedService<LogicSession>()
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

// Configure the HTTP request pipeline.
app.UseAuthorization();
// Apply CORS middleware
app.UseCors();

app.MapControllers();

app.Run();
