using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Security;
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
    [EndpointSummary("Inventario de certificados de sobre digital")]
    [EndpointDescription("Qué hace: lista certificados de sobre digital registrados con metadatos de vigencia y huella. Cuándo se usa: en revisiones operativas previas a cifrado, descifrado y validación de firma. Perfil consumidor: seguridad bancaria, operación ACH y auditoría técnica. Permiso requerido: policy del controller/método vigente en código. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas de acceso. Riesgos operativos: no identificar un certificado expirado puede interrumpir intercambio seguro con contrapartes. Errores esperados: 401/403 por autorización y 500 ante fallas internas. Relación ACH/CENIT/NACHA-M: administra confianza criptográfica usada por operaciones de sobre digital NACHA-M. Precauciones para desarrollo u operación: validar vigencia y tipo de certificado antes de usarlo en productivo.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    public async Task<ActionResult<IEnumerable<DigitalEnvelopeCertificateResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var certificates = await _service.ListAsync(cancellationToken);
        var response = certificates.Select(MapToResponse);
        return Ok(response);
    }
    [EndpointSummary("Endpoint de carga retirado")]
    [EndpointDescription("La carga simplificada fue retirada para impedir persistencia de contraseñas o material privado sin protección. Use los endpoints de gestión público y privado.")]
    [HttpPost]
    [Authorize(Policy = P1Policies.CertificatesUploadPublic)]
    public ActionResult UploadAsync()
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "Endpoint retirado",
            Detail = "Use /nacha-security/certificates/management/public o /nacha-security/certificates/management/private."
        });
    }
    [EndpointSummary("Eliminar certificado de sobre digital")]
    [EndpointDescription("Qué hace: elimina un certificado registrado en inventario de sobre digital. Cuándo se usa: en limpieza controlada o retiro operativo. Perfil consumidor: seguridad bancaria y administradores ACH. Permiso requerido: policy del controller/método vigente en código. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: borrar certificado activo puede interrumpir cifrado/validación. Errores esperados: 404 no encontrado; 409 por reglas de uso; 401/403. Relación ACH/CENIT/NACHA-M: gestiona material de confianza para NACHA-M seguro. Precauciones para desarrollo u operación: validar dependencia activa antes de eliminar.")]
    [HttpDelete("{id:int}")]
    [Authorize(Policy = P1Policies.CertificatesRevoke)]
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
