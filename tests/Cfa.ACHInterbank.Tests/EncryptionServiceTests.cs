using Cfa.ACHInterbank.Application.Services.EncryptionService.Implementations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public sealed class EncryptionServiceTests
{
    [Fact]
    public void Constructor_WithoutConfiguredKey_ShouldNotFailWhileServiceIsOnlyComposed()
    {
        using var service = new EncryptionService(Options.Create(new ApplicationEncryptionOptions()));

        Assert.NotNull(service);
    }

    [Fact]
    public void Encrypt_WithoutConfiguredKey_ShouldFailClosed()
    {
        using var service = new EncryptionService(Options.Create(new ApplicationEncryptionOptions()));

        var exception = Assert.Throws<InvalidOperationException>(() => service.Encrypt("secret"));

        Assert.Equal("appSettings:key es obligatorio para el servicio de cifrado.", exception.Message);
    }

    [Fact]
    public void EncryptAndDecrypt_WithValidThirtyTwoByteKey_ShouldRoundTrip()
    {
        using var service = new EncryptionService(Options.Create(new ApplicationEncryptionOptions
        {
            Key = "0123456789ABCDEF0123456789ABCDEF"
        }));

        var ciphertext = service.Encrypt("credencial-local");
        var plaintext = service.Decrypt(ciphertext);

        Assert.Equal("credencial-local", plaintext);
    }
}
