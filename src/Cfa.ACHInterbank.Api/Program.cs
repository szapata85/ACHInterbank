using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Application;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Api.Encryption;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

AppSettings.Settings = builder.Configuration.GetSection("appSettings").Get<AppSettings>()!.GetMethodExtensions();

builder.Host.UseNLog();
//builder.WebHost.ConfigureKestrel(option =>
//{
//    option.AddServerHeader = false;
//});
builder.Services.Configure<DigitalEnvelopeOptions>(builder.Configuration.GetSection("DigitalEnvelope"));
builder.Services.AddSingleton<IDigitalEnvelopePolicy, DigitalEnvelopePolicy>();
builder.Services.AddWebApi()
                .AddApplication()
                .AddPersistence(builder.Configuration)
                .AddExternal(builder.Configuration);

// Add services to the container.
builder.WebHost.UseIISIntegration();
var app = builder.Build();

app.ConfigureHandler();
app.Run();