using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Application;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Api.Encryption;
using NLog.Web;
using System.Text;

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

var crashLogPath = Path.Combine(builder.Environment.ContentRootPath, "crash.log");
AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    LogCrash(crashLogPath, args.ExceptionObject as Exception, "UnhandledException");
};
TaskScheduler.UnobservedTaskException += (_, args) =>
{
    LogCrash(crashLogPath, args.Exception, "UnobservedTaskException");
    args.SetObserved();
};

// Add services to the container.
builder.WebHost.UseIISIntegration();
var app = builder.Build();

app.ConfigureHandler();
app.Run();

static void LogCrash(string logPath, Exception? exception, string source)
{
    if (exception is null)
    {
        return;
    }

    try
    {
        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var message = new StringBuilder()
            .AppendLine($"[{DateTimeOffset.UtcNow:O}] {source}")
            .AppendLine($"Message: {exception.Message}")
            .AppendLine(exception.StackTrace)
            .AppendLine(new string('-', 80))
            .ToString();

        File.AppendAllText(logPath, message);
    }
    catch
    {
        // Avoid throwing from crash logging.
    }
}
