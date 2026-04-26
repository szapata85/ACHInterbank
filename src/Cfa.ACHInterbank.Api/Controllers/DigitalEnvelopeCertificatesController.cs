using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-security/certificates")]
[Authorize]
public class DigitalEnvelopeCertificatesController : ControllerBase
{
    private readonly IDigitalEnvelopeCertificateService _service;

    public DigitalEnvelopeCertificatesController(IDigitalEnvelopeCertificateService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DigitalEnvelopeCertificateResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var certificates = await _service.ListAsync(cancellationToken);
        var response = certificates.Select(MapToResponse);
        return Ok(response);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

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
                ? X509CertificateLoader.LoadCertificate(rawData)
                : X509CertificateLoader.LoadPkcs12(rawData, request.Password, X509KeyStorageFlags.MachineKeySet);
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

        var saved = await _service.UpsertAsync(entity, cancellationToken);
        return Ok(MapToResponse(saved));
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [EndpointSummary("DELETE {id:int}: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación '{id:int}'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: modifica información. Genera auditoría: sí, mediante los servicios de operación/auditoría cuando aplica al flujo.")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
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
