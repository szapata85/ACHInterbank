using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Api.OpenApi;
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
    /// <summary>Lista los certificados registrados por el servicio de sobre digital.</summary>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar certificados de sobre digital")]
    [EndpointDescription("Consulta el inventario del servicio y devuelve por cada registro su identificador, archivo, tipo, disponibilidad de llave privada y metadatos del certificado almacenado. La operación es de solo lectura y el controller devuelve 200 con la colección obtenida.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    [ProducesResponseType(typeof(IEnumerable<DigitalEnvelopeCertificateResponse>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de certificados registrados por el servicio de sobre digital.")]
    public async Task<ActionResult<IEnumerable<DigitalEnvelopeCertificateResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var certificates = await _service.ListAsync(cancellationToken);
        var response = certificates.Select(MapToResponse);
        return Ok(response);
    }
    /// <summary>Informa que la operación simplificada de carga ya no está disponible.</summary>
    [EndpointSummary("Carga simplificada de certificados retirada")]
    [EndpointDescription("La operación no procesa un archivo ni crea un certificado. Siempre devuelve 410 con ProblemDetails e indica usar las operaciones de gestión public o private.")]
    [HttpPost]
    [Authorize(Policy = P1Policies.CertificatesUploadPublic)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    [OpenApiResponseDescription(StatusCodes.Status410Gone, "La carga simplificada está retirada y el cuerpo ProblemDetails remite a las operaciones de gestión public o private.")]
    public ActionResult UploadAsync()
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "Endpoint retirado",
            Detail = "Use /nacha-security/certificates/management/public o /nacha-security/certificates/management/private."
        });
    }
    /// <summary>Solicita eliminar del inventario de sobre digital el certificado identificado por id.</summary>
    /// <param name="id">Identificador del certificado que se solicita eliminar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Eliminar certificado de sobre digital")]
    [EndpointDescription("Invoca la eliminación del servicio y, al completar la llamada, devuelve 204 sin cuerpo. La implementación actual del repositorio no produce 404 cuando el identificador no existe: simplemente no elimina ninguna entidad.")]
    [HttpDelete("{id:int}")]
    [Authorize(Policy = P1Policies.CertificatesRevoke)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [OpenApiResponseDescription(StatusCodes.Status204NoContent, "La solicitud de eliminación finalizó sin cuerpo; la implementación actual también usa este resultado cuando no existe una entidad con el id indicado.")]
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

    /// <summary>Contrato de lectura de un certificado registrado para el servicio de sobre digital.</summary>
    public class DigitalEnvelopeCertificateResponse
    {
        /// <summary>Identificador persistido del certificado.</summary>
        public int Id { get; set; }

        /// <summary>Nombre de archivo almacenado para el certificado.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>Tipo de certificado de sobre digital persistido.</summary>
        public DigitalEnvelopeCertificateType Type { get; set; }

        /// <summary>Indica si el registro contiene llave privada.</summary>
        public bool HasPrivateKey { get; set; }

        /// <summary>Subject persistido del certificado.</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>Issuer persistido del certificado.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>Thumbprint persistido del certificado.</summary>
        public string Thumbprint { get; set; } = string.Empty;

        /// <summary>Fecha inicial de vigencia almacenada cuando está disponible.</summary>
        public DateTime? NotBefore { get; set; }

        /// <summary>Fecha final de vigencia almacenada cuando está disponible.</summary>
        public DateTime? NotAfter { get; set; }

        /// <summary>Fecha y hora de registro almacenada por el servicio.</summary>
        public DateTime UploadedAt { get; set; }
    }
}
