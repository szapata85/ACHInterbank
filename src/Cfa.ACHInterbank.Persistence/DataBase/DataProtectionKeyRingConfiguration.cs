using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public static class DataProtectionKeyRingConfiguration
{
    public const string ApplicationName = "Cfa.ACHInterbank";
    public const string ValidationPurpose = "Cfa.ACHInterbank.DataProtection.KeyRingValidation.v1";
}

internal sealed class DataProtectionKeyRingStartupValidator : IHostedService
{
    private readonly IDataProtectionProvider _provider;

    public DataProtectionKeyRingStartupValidator(IDataProtectionProvider provider)
    {
        _provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var protector = _provider.CreateProtector(DataProtectionKeyRingConfiguration.ValidationPurpose);
        var probe = RandomNumberGenerator.GetBytes(32);
        byte[]? protectedProbe = null;
        byte[]? recoveredProbe = null;

        try
        {
            protectedProbe = protector.Protect(probe);
            recoveredProbe = protector.Unprotect(protectedProbe);
            if (!CryptographicOperations.FixedTimeEquals(probe, recoveredProbe))
            {
                throw new CryptographicException("Data Protection key ring validation failed.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(probe);
            if (protectedProbe is not null) CryptographicOperations.ZeroMemory(protectedProbe);
            if (recoveredProbe is not null) CryptographicOperations.ZeroMemory(recoveredProbe);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
