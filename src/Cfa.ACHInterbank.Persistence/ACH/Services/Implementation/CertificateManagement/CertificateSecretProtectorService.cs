using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

[Scoped]
public class CertificateSecretProtectorService : ICertificateSecretProtector
{
    public Task EnsureAcceptableAsync(CertificateStorageMode mode, string? secretRef, string? password, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(password))
        {
            // Password must only be used in-memory and never persisted.
        }

        if (mode != CertificateStorageMode.DatabaseEncrypted)
        {
            throw new InvalidOperationException("Solo se admite material privado cifrado en la base de datos.");
        }

        return Task.CompletedTask;
    }
}
