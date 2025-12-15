using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-security/certificates")]
[Authorize]
public class DigitalEnvelopeCertificatesController : ControllerBase
{
    private readonly IDigitalEnvelopeCertificateRepository _repository;

    public DigitalEnvelopeCertificatesController(IDigitalEnvelopeCertificateRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DigitalEnvelopeCertificateResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var certificates = await _repository.ListAsync(cancellationToken);
        var response = certificates.Select(MapToResponse);
        return Ok(response);
    }

    [HttpPost]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<ActionResult<DigitalEnvelopeCertificateResponse>> UploadAsync([FromForm] UploadCertificateRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("No se ha proporcionado ningún archivo de certificado.");
        }

        await using var memory = new MemoryStream();
        await request.File.CopyToAsync(memory, cancellationToken);
        var rawData = memory.ToArray();

        X509Certificate2 certificate;
        try
        {
            certificate = string.IsNullOrWhiteSpace(request.Password)
                ? new X509Certificate2(rawData)
                : new X509Certificate2(rawData, request.Password, X509KeyStorageFlags.MachineKeySet);
        }
        catch (CryptographicException)
        {
            return BadRequest("El certificado o la contraseña son inválidos.");
        }

        var entity = new DigitalEnvelopeCertificate
        {
            FileName = request.File.FileName,
            RawData = rawData,
            Password = request.Password,
            Type = request.Type,
            HasPrivateKey = certificate.HasPrivateKey,
            Subject = certificate.Subject,
            Issuer = certificate.Issuer,
            Thumbprint = certificate.Thumbprint,
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter
        };

        var saved = await _repository.SaveAsync(entity, cancellationToken);
        return Ok(MapToResponse(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static DigitalEnvelopeCertificateResponse MapToResponse(DigitalEnvelopeCertificate certificate)
    {
        return new DigitalEnvelopeCertificateResponse
        {
            Id = certificate.Id,
            FileName = certificate.FileName,
            Type = certificate.Type,
            HasPrivateKey = certificate.HasPrivateKey,
            Subject = certificate.Subject ?? string.Empty,
            Issuer = certificate.Issuer ?? string.Empty,
            Thumbprint = certificate.Thumbprint ?? string.Empty,
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter,
            UploadedAt = certificate.UploadedAt
        };
    }

    public class UploadCertificateRequest
    {
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }

        [FromForm(Name = "type")]
        public DigitalEnvelopeCertificateType Type { get; set; }

        [FromForm(Name = "password")]
        public string? Password { get; set; }
    }

    public class DigitalEnvelopeCertificateResponse
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DigitalEnvelopeCertificateType Type { get; set; }
        public bool HasPrivateKey { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Thumbprint { get; set; } = string.Empty;
        public DateTime? NotBefore { get; set; }
        public DateTime? NotAfter { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
