using Common.Logic;
using Common.Meta;
using Microsoft.Extensions.Options;
using Server;
using Server.Logic;
using Server.Meta;
using Shared.Boot.Asp.Version;
using Shared.Boot.Version;
using Shared.Log;
using Shared.Tp;
using Shared.Tp.Ext.Misc;
using Shared.Tp.Rtc;
using Shared.Tp.Rtc.Sip;

var version = new AspVersionProvider().ReadBuildVersion();
Slog.Info($">>>> starting build: {version.ToShortInfo()}");
var builder = WebApplication.CreateBuilder(args);

//TODO: add custom console formatter with category recolor to simplify debug
//  https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
builder.AddPrettyConsoleLoggerProvider();

// Add services to the container.
var configuration = builder.Configuration;
builder.Services
    .AddSingleton<IMeta, MetaServer>()
    .Configure<SipRtcConfig>(configuration.GetSection(nameof(SipRtcConfig)))
    .AddSingleton<SipRtcService>()
    .AddSingleton<IRtcService>(sp => sp.GetRequiredService<SipRtcService>())
    .Configure<DumpLink.Options>(configuration.GetSection(nameof(DumpLink)))
    .Configure<SyncOptions>(configuration.GetSection($"{nameof(ServerSession)}:{nameof(SyncOptions)}"))
    .AddSingleton<ITpApi>(sp => CommonSession.CreateApi(
        sp.GetRequiredService<SipRtcService>(), 
        null,
        sp.GetRequiredService<IOptionsMonitor<DumpLink.Options>>(),
        sp.GetRequiredService<ILoggerFactory>()))
    .AddSingleton<ServerSession>()
    .AddHostedService<ServerSession>(sp => sp.GetRequiredService<ServerSession>())
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

app.UseAuthorization();
app.UseCors(); // Apply CORS middleware

app.MapControllers();

Slog.Info("==== running service");
app.Run();
Slog.Info("<<<< exiting service");
