using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchColombiaManagedMftCredentialComponent(
    AchDbContext context,
    AchColombiaManagedMftAdministrationComponent administration,
    IEncryptionService encryption)
{
    public async Task<AchManagedMftAdministrationDto> SetCredentialAsync(
        SetAchManagedMftCredentialRequest value,
        string actor,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value.CredentialType) || string.IsNullOrWhiteSpace(value.Secret))
            throw new ArgumentException("La credencial es obligatoria.");

        var entity = await administration.GetOrCreateConfigurationEntityAsync(ct);
        if (entity.ConcurrencyToken != value.ConcurrencyToken)
            throw new DbUpdateConcurrencyException("ACHCOL_MFT_CONFIGURATION_CONFLICT");

        var credentialType = AchColombiaManagedMftImplementationSupport.LimitRequired(value.CredentialType, 40);
        if (string.Equals(entity.Provider, "SftpEndpoint", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(credentialType, "Password", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("ACHCOL_MFT_SFTP_CREDENTIAL_TYPE_UNSUPPORTED");
        }

        entity.CredentialType = credentialType;
        entity.ProtectedCredential = encryption.Encrypt(value.Secret);
        entity.CredentialUpdatedAtUtc = DateTime.UtcNow;
        entity.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return AchColombiaManagedMftAdministrationComponent.MapAdministration(entity);
    }
}
