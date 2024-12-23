using Server.Meta;
using Server.Rtc;
using Shared.Meta.Api;

Shared.StaticLog.Info("==== starting server ====");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddSingleton<IMeta, MetaServer>()
    .AddSingleton<RtcService>()
    .AddHostedService<RtcService>()
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

// Configure the HTTP request pipeline.
app.UseAuthorization();
app.MapControllers();

app.Run();
