using System.Net.Sockets;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Renci.SshNet.Common;

namespace Cfa.ACHInterbank.External.Connections;

internal sealed class AchColombiaManagedMftConnectionTester : IAchColombiaManagedMftConnectionTester
{
    private readonly IAchColombiaManagedMftConfigurationProvider _configurationProvider;
    private readonly IEncryptionService _encryption;
    private readonly IAchColombiaSftpSessionFactory _sessionFactory;

    internal AchColombiaManagedMftConnectionTester(
        IAchColombiaManagedMftConfigurationProvider configurationProvider,
        IEncryptionService encryption,
        IAchColombiaSftpSessionFactory sessionFactory)
    {
        _configurationProvider = configurationProvider;
        _encryption = encryption;
        _sessionFactory = sessionFactory;
    }

    public async Task<AchManagedMftConnectionTestResult> TestAsync(CancellationToken ct = default)
    {
        try
        {
            var configuration = await _configurationProvider.GetEffectiveAsync(ct);
            if (IsManagedFolder(configuration))
            {
                return TestManagedFolder(configuration);
            }

            if (IsSftp(configuration))
            {
                return TestSftp(configuration, ct);
            }

            return Failed("ACHCOL_MFT_CONNECTION_CONFIGURATION_INVALID", "La configuración de conexión no es válida.");
        }
        catch (AchColombiaSftpHostKeyException)
        {
            return Failed("ACHCOL_MFT_SFTP_HOST_KEY_REJECTED", "La huella del servidor no es válida.");
        }
        catch (SshAuthenticationException)
        {
            return Failed("ACHCOL_MFT_SFTP_AUTHENTICATION_FAILED", "Usuario o credencial incorrecta.");
        }
        catch (SftpPermissionDeniedException)
        {
            return Failed("ACHCOL_MFT_SFTP_ACCESS_DENIED", "No hay acceso a la carpeta configurada.");
        }
        catch (SftpPathNotFoundException)
        {
            return Failed("ACHCOL_MFT_SFTP_PATH_NOT_FOUND", "No hay acceso a la carpeta configurada.");
        }
        catch (UnauthorizedAccessException)
        {
            return Failed("ACHCOL_MFT_FOLDER_ACCESS_DENIED", "No hay acceso a la carpeta configurada.");
        }
        catch (DirectoryNotFoundException)
        {
            return Failed("ACHCOL_MFT_FOLDER_PATH_NOT_FOUND", "No hay acceso a la carpeta configurada.");
        }
        catch (ArgumentException)
        {
            return Failed("ACHCOL_MFT_CONNECTION_CONFIGURATION_INVALID", "La configuración de conexión no es válida.");
        }
        catch (InvalidOperationException)
        {
            return Failed("ACHCOL_MFT_CONNECTION_CONFIGURATION_INVALID", "La configuración de conexión no es válida.");
        }
        catch (SocketException)
        {
            return Failed("ACHCOL_MFT_CONNECTION_FAILED", "No fue posible conectar.");
        }
        catch (SshConnectionException)
        {
            return Failed("ACHCOL_MFT_CONNECTION_FAILED", "No fue posible conectar.");
        }
        catch (TimeoutException)
        {
            return Failed("ACHCOL_MFT_CONNECTION_FAILED", "No fue posible conectar.");
        }
        catch (IOException)
        {
            return Failed("ACHCOL_MFT_CONNECTION_FAILED", "No fue posible conectar.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Failed("ACHCOL_MFT_CONNECTION_FAILED", "No fue posible conectar.");
        }
    }

    private static AchManagedMftConnectionTestResult TestManagedFolder(AchManagedMftEffectiveConfiguration configuration)
    {
        foreach (var path in new[] { configuration.OutboundPath, configuration.InboundPath, configuration.ArchivePath })
        {
            var fullPath = ResolveDirectory(path);
            if (!Directory.Exists(fullPath))
            {
                return Failed("ACHCOL_MFT_FOLDER_PATH_NOT_FOUND", "No hay acceso a la carpeta configurada.");
            }

            _ = Directory.EnumerateFileSystemEntries(fullPath).Take(1).ToArray();
        }

        return Success();
    }

    private AchManagedMftConnectionTestResult TestSftp(AchManagedMftEffectiveConfiguration configuration, CancellationToken ct)
    {
        ValidateSftp(configuration);
        ct.ThrowIfCancellationRequested();

        var password = _encryption.Decrypt(configuration.ProtectedCredential!);
        try
        {
            using var session = _sessionFactory.Connect(configuration, password);
            foreach (var path in new[] { configuration.OutboundPath, configuration.InboundPath, configuration.ArchivePath })
            {
                ct.ThrowIfCancellationRequested();
                _ = session.List(path);
            }
        }
        finally
        {
            password = string.Empty;
        }

        return Success();
    }

    private static void ValidateSftp(AchManagedMftEffectiveConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Endpoint)
            || configuration.Port is not (>= 1 and <= 65535)
            || string.IsNullOrWhiteSpace(configuration.Principal))
        {
            throw new InvalidOperationException("ACHCOL_MFT_SFTP_ENDPOINT_INCOMPLETE");
        }

        if (!string.Equals(configuration.CredentialType, "Password", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(configuration.ProtectedCredential))
        {
            throw new InvalidOperationException("ACHCOL_MFT_SFTP_PASSWORD_CREDENTIAL_REQUIRED");
        }

        AchColombiaSftpHostKeyFingerprint.NormalizeRequired(configuration.SftpHostKeyFingerprint);
        if (configuration.SftpTimeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException("ACHCOL_MFT_SFTP_TIMEOUT_INVALID");
        }
    }

    private static string ResolveDirectory(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException("ACHCOL_MFT_PATH_NOT_CONFIGURED");
        }

        var full = Path.GetFullPath(configured).TrimEnd(Path.DirectorySeparatorChar);
        if (string.Equals(full, Path.GetPathRoot(full)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ACHCOL_MFT_ROOT_PATH_NOT_ALLOWED");
        }

        return full;
    }

    private static bool IsManagedFolder(AchManagedMftEffectiveConfiguration configuration)
        => string.Equals(configuration.Provider, "ManagedFolder", StringComparison.OrdinalIgnoreCase)
           && string.Equals(configuration.Protocol, "ManagedFile", StringComparison.OrdinalIgnoreCase);

    private static bool IsSftp(AchManagedMftEffectiveConfiguration configuration)
        => string.Equals(configuration.Provider, "SftpEndpoint", StringComparison.OrdinalIgnoreCase)
           && string.Equals(configuration.Protocol, "Sftp", StringComparison.OrdinalIgnoreCase);

    private static AchManagedMftConnectionTestResult Success()
        => new(true, "ACHCOL_MFT_CONNECTION_OK", "Conexión exitosa.");

    private static AchManagedMftConnectionTestResult Failed(string code, string message)
        => new(false, code, message);
}
