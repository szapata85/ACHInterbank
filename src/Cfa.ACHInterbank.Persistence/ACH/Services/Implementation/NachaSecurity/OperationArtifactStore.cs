using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.NachaSecurity;

[Scoped]
public class OperationArtifactStore : IOperationArtifactStore
{
    private readonly OperationArtifactOptions _options;

    public OperationArtifactStore(IOptions<OperationArtifactOptions> options)
    {
        _options = options.Value ?? new OperationArtifactOptions();
        Directory.CreateDirectory(_options.BasePath);
    }

    public async Task<string> SaveAsync(string operationId, string extension, byte[] content, CancellationToken cancellationToken = default)
    {
        if (content.Length > _options.MaxFileSizeMb * 1024 * 1024)
        {
            throw new InvalidOperationException("Archivo excede tamaño máximo permitido.");
        }

        var safeExt = extension.StartsWith('.') ? extension : $".{extension}";
        var operationFolder = Path.Combine(_options.BasePath, operationId);
        Directory.CreateDirectory(operationFolder);
        var fileName = $"artifact{safeExt}";
        var fullPath = Path.Combine(operationFolder, fileName);

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return Path.GetRelativePath(_options.BasePath, fullPath).Replace("\\", "/", StringComparison.Ordinal);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.BasePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.BasePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
