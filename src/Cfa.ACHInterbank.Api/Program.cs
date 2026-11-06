using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Application;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Api.Encryption;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Configuracion temprana: tambien registra errores ocurridos antes de builder.Build().
LogManager.Setup().LoadConfigurationFromFile(Path.Combine(builder.Environment.ContentRootPath, "NLog.config"));
var runtimeLogger = LogManager.GetLogger("ACHInterbank.Runtime");

// Un proveedor de registro y un solo destino NLog; evita duplicados de AddNLog().
builder.Logging.ClearProviders();
builder.Host.UseNLog();

AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    if (args.ExceptionObject is Exception exception)
    {
        runtimeLogger.Fatal(exception, "UnhandledException. IsTerminating={IsTerminating}", args.IsTerminating);
    }
    else
    {
        runtimeLogger.Fatal("UnhandledException. NonExceptionObjectType={ErrorType}", args.ExceptionObject?.GetType().FullName);
    }

    // Intentar vaciar los registros antes de la finalizacion forzosa del proceso.
    LogManager.Flush(TimeSpan.FromSeconds(2));
};
TaskScheduler.UnobservedTaskException += (_, args) =>
{
    runtimeLogger.Error(args.Exception, "UnobservedTaskException");
    args.SetObserved();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    runtimeLogger.Info("ProcessExit. ExitCode={ExitCode}", Environment.ExitCode);
    LogManager.Flush(TimeSpan.FromSeconds(2));
};
Console.CancelKeyPress += (_, args) =>
    runtimeLogger.Info("CancelKeyPress. SpecialKey={SpecialKey}", args.SpecialKey);

// Evitar divulgar la version del servidor Kestrel en respuestas directas.
builder.WebHost.ConfigureKestrel(option => option.AddServerHeader = false);
builder.Services.Configure<DigitalEnvelopeOptions>(builder.Configuration.GetSection("DigitalEnvelope"));
builder.Services.Configure<DigitalEnvelopeCertificateOptions>(builder.Configuration.GetSection("DigitalEnvelope:CertificateResolver"));
builder.Services.Configure<DigitalEnvelopeSignatureValidationOptions>(builder.Configuration.GetSection("DigitalEnvelope:SignatureValidation"));
builder.Services.AddSingleton<IDigitalEnvelopePolicy, DigitalEnvelopePolicy>();
builder.Services.AddWebApi(builder.Configuration)
                .AddApplication()
                .AddPersistence(builder.Configuration, builder.Environment)
                .AddExternal(builder.Configuration);

// Add services to the container.
builder.WebHost.UseIISIntegration();
var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => runtimeLogger.Info("ApplicationStarted"));
app.Lifetime.ApplicationStopping.Register(() => runtimeLogger.Info("ApplicationStopping"));
app.Lifetime.ApplicationStopped.Register(() => runtimeLogger.Info("ApplicationStopped"));

app.ConfigureHandler();
try
{
    app.Run();
}
finally
{
    // Garantiza el vaciado de la cola asincrona durante una detencion normal.
    LogManager.Shutdown();
}
