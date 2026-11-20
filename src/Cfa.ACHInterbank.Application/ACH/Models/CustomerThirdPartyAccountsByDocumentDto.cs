using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Customers.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Lectura de las cuentas canónicas de terceros de un cliente identificado por documento.
/// Los estados de prenotificación se devuelven por contexto de cámara/naturaleza/servicio,
/// sin inferir una autorización transaccional global.
/// </summary>
public sealed record CustomerThirdPartyAccountsByDocumentDto
{
    public CustomerDocumentLookupDto Customer { get; init; } = new();
    public PagedResponse<ThirdPartyAccountReadDto> Accounts { get; init; } = new();
}
