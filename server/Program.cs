using Server.Meta;
using Shared.Meta.Api;

Shared.StaticLog.Info("==== starting server ====");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IMeta, MetaServer>();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();
app.MapControllers();

app.Run();
