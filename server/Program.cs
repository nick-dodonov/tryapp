using Server.Meta;
using Server.Rtc;
using Shared.Meta.Api;
using Shared.Rtc;

Shared.StaticLog.Info("==== starting server ====");
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

var app = builder.Build();
app.Logger.LogInformation("Starting server");

// Configure the HTTP request pipeline.
app.UseAuthorization();
app.MapControllers();

app.Run();
