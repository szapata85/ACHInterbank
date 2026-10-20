using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

// Ejecutor local sin PowerShell, sin Bash en el host y sin archivo .env.
// La única fuente de la contraseña de sa es appsettings.Development.json.
try
{
    return Execute(args);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception.Message}");
    return 1;
}

static int Execute(string[] args)
{
    if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
    {
        PrintHelp();
        return args.Length == 0 ? 1 : 0;
    }

    string action = args[0].ToLowerInvariant();
    HashSet<string> allowedActions = ["config", "infra", "migrate", "up", "down", "reset", "status", "logs"];
    if (!allowedActions.Contains(action))
        throw new InvalidOperationException($"Acción no reconocida: {args[0]}. Use --help.");

    HashSet<string> options = new(args.Skip(1), StringComparer.OrdinalIgnoreCase);
    if (options.Count != args.Length - 1)
        throw new InvalidOperationException("No repita opciones.");
    string[] validOptions = ["--seed", "--no-seed", "--no-auto-migrations", "--no-build", "--confirm-data-loss"];
    string? invalid = options.FirstOrDefault(option => !validOptions.Contains(option, StringComparer.OrdinalIgnoreCase));
    if (invalid is not null)
        throw new InvalidOperationException($"Opción no reconocida: {invalid}.");
    if (options.Contains("--seed") && options.Contains("--no-seed"))
        throw new InvalidOperationException("No use --seed y --no-seed a la vez.");
    if (action != "up" && (options.Contains("--seed") || options.Contains("--no-seed") ||
                            options.Contains("--no-auto-migrations") || options.Contains("--no-build")))
        throw new InvalidOperationException("Las opciones --seed, --no-seed, --no-auto-migrations y --no-build solo aplican a 'up'.");
    if (action != "reset" && options.Contains("--confirm-data-loss"))
        throw new InvalidOperationException("--confirm-data-loss solo aplica a 'reset'.");
    if (action == "reset" && !options.Contains("--confirm-data-loss"))
        throw new InvalidOperationException("'reset' destruye datos del volumen SQL Server. Repita con --confirm-data-loss.");

    string root = LocateRepository();
    string settingsFile = Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "appsettings.Development.json");
    using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsFile));
    JsonElement db = settings.RootElement.GetProperty("Database");
    string provider = db.GetProperty("Provider").GetString() ?? "";
    if (provider != "SqlServer")
        throw new InvalidOperationException("Cambie Database:Provider a SqlServer en appsettings.Development.json.");

    string connection = settings.RootElement.GetProperty("ConnectionStrings")
        .GetProperty("SqlConnection").GetString() ?? "";
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("ConnectionStrings:SqlConnection está vacío.");

    DbConnectionStringBuilder sqlHost = new() { ConnectionString = connection };
    string user = GetValue(sqlHost, "User Id", "User ID", "UID") ?? "";
    string password = GetValue(sqlHost, "Password", "Pwd") ?? "";
    string? dbName = GetValue(sqlHost, "Database", "Initial Catalog");
    string address = GetValue(sqlHost, "Server", "Data Source", "Address", "Addr", "Network Address") ?? "";
    string trusted = GetValue(sqlHost, "Trusted_Connection", "Integrated Security") ?? "";
    if (!string.Equals(user, "sa", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("La cadena SQL requiere User Id=sa y Password para el contenedor.");
    if (trusted.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        trusted.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
        trusted.Equals("sspi", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Elimine Trusted_Connection/Integrated Security; use autenticación SQL.");
    if (password.Length < 8)
        throw new InvalidOperationException("La contraseña de sa debe cumplir la política de SQL Server (recomendados 12 o más caracteres).");
    if (dbName is null || !Regex.IsMatch(dbName, "^[A-Za-z][A-Za-z0-9_]{0,127}$"))
        throw new InvalidOperationException("El nombre de la base debe contener letras, números o guion bajo, comenzando con una letra.");

    Match serverMatch = Regex.Match(address, @"^(?:localhost|127\.0\.0\.1)(?:,(\d{1,5}))?$", RegexOptions.IgnoreCase);
    if (!serverMatch.Success)
        throw new InvalidOperationException("Use Server=127.0.0.1,1433 (o localhost,puerto) en la cadena de Development; no LocalDB.");
    int port = serverMatch.Groups[1].Success ? int.Parse(serverMatch.Groups[1].Value) : 1433;
    if (port is < 1 or > 65535)
        throw new InvalidOperationException("Puerto SQL Server inválido.");

    // ADO.NET del host usa 127.0.0.1; dentro de Docker la API usa el DNS del servicio.
    string serverKey = FindKey(sqlHost, "Server", "Data Source", "Address", "Addr", "Network Address")!;
    sqlHost[serverKey] = "sqlserver,1433";

    Dictionary<string, string> composeEnv = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MSSQL_SA_PASSWORD"] = password,
        ["MSSQL_DB"] = dbName,
        ["SQLSERVER_HOST_PORT"] = port.ToString(),
        ["ACH_DOCKER_SQL_CONNECTION"] = sqlHost.ConnectionString,
        ["DATABASE_APPLY_MIGRATIONS"] = (db.GetProperty("ApplyMigrations").GetBoolean() && !options.Contains("--no-auto-migrations")).ToString().ToLowerInvariant(),
        ["DATABASE_APPLY_SEED"] = ((db.GetProperty("ApplySeed").GetBoolean() || options.Contains("--seed")) && !options.Contains("--no-seed")).ToString().ToLowerInvariant()
    };

    switch (action)
    {
        case "config":
            // No mostrar `docker compose config`: contiene secretos resueltos.
            Compose(root, composeEnv, "config", "-q");
            Console.WriteLine("Compose validado. La contraseña no se imprimió.");
            break;
        case "infra":
            EnsureQuartz(root, composeEnv);
            break;
        case "migrate":
            EnsureQuartz(root, composeEnv);
            Dictionary<string, string> efEnv = new(composeEnv, StringComparer.OrdinalIgnoreCase)
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["Database__Provider"] = "SqlServer",
                // EF en el host usa la cadena original, no sqlserver:1433.
                ["ConnectionStrings__SqlConnection"] = connection
            };
            // dotnet-ef es herramienta local (.config/dotnet-tools.json).
            // En una instalación nueva el SDK está, pero la herramienta aún no.
            Console.WriteLine("Restaurando herramientas .NET locales (dotnet-ef)...");
            Run(root, efEnv, "dotnet", "tool", "restore");
            // dotnet-ef requiere project.assets.json de ambos proyectos al leer sus metadatos.
            // No restaurar solo la herramienta: la restauración de NuGet es independiente.
            Console.WriteLine("Restaurando paquetes NuGet de la API...");
            Run(root, efEnv, "dotnet", "restore", "src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj");
            Console.WriteLine("Restaurando paquetes NuGet de las migraciones SQL Server...");
            Run(root, efEnv, "dotnet", "restore", "src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj");
            Run(root, efEnv, "dotnet", "ef", "database", "update",
                "--project", "src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer",
                "--startup-project", "src/Cfa.ACHInterbank.Api", "--context", "AchDbContext");
            Console.WriteLine("Migraciones EF SQL Server aplicadas correctamente.");
            break;
        case "up":
            if (options.Contains("--no-build"))
                Compose(root, composeEnv, "up", "-d");
            else
                Compose(root, composeEnv, "up", "-d", "--build");
            break;
        case "down":
            Compose(root, composeEnv, "down", "--remove-orphans");
            break;
        case "reset":
            Compose(root, composeEnv, "down", "-v", "--remove-orphans");
            Console.WriteLine("Volumen de SQL Server de este proyecto eliminado. No se borraron volúmenes de otros proyectos.");
            break;
        case "status":
            Compose(root, composeEnv, "ps", "-a");
            break;
        case "logs":
            Compose(root, composeEnv, "logs", "--tail", "100", "sqlserver", "quartz-schema", "achinterbank-api", "achinterbank-spa");
            break;
    }
    return 0;
}

static void EnsureQuartz(string root, IReadOnlyDictionary<string, string> env)
{
    Compose(root, env, "up", "-d", "sqlserver", "quartz-schema");
    Compose(root, env, "wait", "quartz-schema");
    string containerId = Capture(root, env, "docker", "compose", "-f", "docker-compose.yml", "ps", "-a", "-q", "quartz-schema").Trim();
    if (string.IsNullOrEmpty(containerId))
        throw new InvalidOperationException("El contenedor de Quartz no se encontró.");
    string status = Capture(root, env, "docker", "inspect", "--format", "{{.State.ExitCode}}", containerId).Trim();
    if (status != "0")
        throw new InvalidOperationException($"Quartz terminó con código {status}. Revise los logs de quartz-schema.");
}

static string LocateRepository()
{
    DirectoryInfo? candidate = new(Directory.GetCurrentDirectory());
    while (candidate is not null)
    {
        if (File.Exists(Path.Combine(candidate.FullName, "docker-compose.yml")) &&
            File.Exists(Path.Combine(candidate.FullName, "src", "Cfa.ACHInterbank.Api", "appsettings.Development.json")))
            return candidate.FullName;
        candidate = candidate.Parent;
    }
    throw new InvalidOperationException("Ejecute desde la raíz del repositorio ACHInterbank NAV6.");
}

static string? FindKey(DbConnectionStringBuilder builder, params string[] keys)
    => keys.FirstOrDefault(builder.ContainsKey);

static string? GetValue(DbConnectionStringBuilder builder, params string[] keys)
{
    string? key = FindKey(builder, keys);
    return key is null ? null : Convert.ToString(builder[key]);
}

static void Compose(string workingDirectory, IReadOnlyDictionary<string, string> environment, params string[] arguments)
    => Run(workingDirectory, environment, "docker", ["compose", "-f", "docker-compose.yml", .. arguments]);

static void Run(string workingDirectory, IReadOnlyDictionary<string, string> environment, string executable, params string[] arguments)
{
    using Process process = Start(workingDirectory, environment, executable, false, arguments);
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"{executable} terminó con código {process.ExitCode}. Revise la salida anterior.");
}

static string Capture(string workingDirectory, IReadOnlyDictionary<string, string> environment, string executable, params string[] arguments)
{
    using Process process = Start(workingDirectory, environment, executable, true, arguments);
    string output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"{executable} terminó con código {process.ExitCode}.");
    return output;
}

static Process Start(string workingDirectory, IReadOnlyDictionary<string, string> environment, string executable,
    bool captureOutput, params string[] arguments)
{
    ProcessStartInfo start = new(executable)
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = captureOutput
    };
    foreach (string arg in arguments)
        start.ArgumentList.Add(arg);
    foreach ((string key, string value) in environment)
        start.Environment[key] = value;
    return Process.Start(start) ?? throw new InvalidOperationException($"No se pudo iniciar {executable}.");
}

static void PrintHelp()
{
    Console.WriteLine("""
        ACHInterbank NAV6 - Docker SQL Server sin PowerShell (Windows CMD / Linux / macOS)
        Uso: dotnet run --project tools/AchInterbank.DockerCli -- ACCION [opciones]

          config                         Valida YAML sin mostrar contraseña
          infra                          Levanta SQL Server e inicializa Quartz
          migrate                        Inicializa SQL Server y aplica migraciones EF
          up [--seed] [--no-auto-migrations] [--no-build] [--no-seed]
                                         Construye/levanta API, SPA y SQL Server
          status                         Lista contenedores del proyecto
          logs                           Últimas 100 líneas de logs
          down                           Apaga el stack; conserva volumen SQL
          reset --confirm-data-loss      Elimina contenedores y volumen SQL (DESTRUCTIVO)

        La contraseña se lee de src/Cfa.ACHInterbank.Api/appsettings.Development.json.
        No se utiliza PowerShell, Bash en el host, ni un archivo .env adicional.
        """);
}
