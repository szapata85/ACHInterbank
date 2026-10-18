using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class OpenApiDocumentMetadataTransformer : IOpenApiDocumentTransformer
{
    private const string DocumentDescription = """
        ACHInterbank expone contratos HTTP para las capacidades de integración interbancaria implementadas por la aplicación. Este documento OpenAPI, presentado en Scalar, describe el contrato publicado; las reglas funcionales específicas se detallan en cada operación cuando corresponda.

        ### Cámaras y NACHA-M

        ACH Colombia y CENIT se manejan como cámaras independientes. Una operación puede pertenecer a una de ellas, ser transversal a ambas o corresponder a lógica interna de ACHInterbank. Las reglas de una cámara no deben interpretarse automáticamente como aplicables a la otra. NACHA-M se interpreta siempre en su contexto: ACH Colombia y CENIT poseen lineamientos propios, y las operaciones específicas indicarán el contexto aplicable cuando corresponda.

        ### Seguridad y convenciones

        La autenticación documentada se representa mediante el esquema Bearer JWT. Cada operación expresa estructuralmente, según su metadata de autorización publicada en OpenAPI, si requiere ese esquema. Authorization se representa mediante ese modelo de seguridad y no como parámetro header. Las operaciones se agrupan por tags funcionales y publican un operationId estable y único para facilitar su identificación técnica y el consumo por herramientas. Idempotency-Key y X-Correlation-ID solo aparecen en las operaciones que los consumen; no son headers obligatorios globales.

        ### Quick Start

        1. **Autenticarse.** Enviar `POST /Auth/login` con el contrato `LoginRequest`. Por ejemplo, con datos ficticios:

           ```json
           {
             "username": "usuario.demo",
             "password": "ClaveDemo-123!"
           }
           ```

        2. **Obtener el JWT.** La respuesta `200` usa `BaseResponseModel`; el token emitido se encuentra en `data.token`.

        3. **Usar el token.** En las operaciones protegidas, enviar el esquema Bearer documentado por OpenAPI:

           ```http
           Authorization: Bearer <token>
           ```

           `Authorization` no es un parámetro OpenAPI independiente. Una operación protegida también puede exigir las autorizaciones o políticas que publique su contrato.

        4. **Realizar una primera consulta protegida.** `GET /clearing-houses` consulta el listado/configuración de cámaras mediante una operación protegida para una identidad autorizada.

        5. **Descubrir códigos antes de configurar una cámara.** Consultar `GET /clearing-houses/payment-rail-options`: su `Code` es el valor utilizable como `PaymentRailCode`. Consultar `GET /clearing-houses/nacha-profiles?clearingHouseCode=<codigo-camara>`: su `Id` es el valor utilizable como `NachaProfileId`. `clearingHouseCode` conserva el contexto de cámara; los códigos de catálogo deben descubrirse mediante estas operaciones y no inventarse.

        6. **Continuar hacia ciclos.** Con una cámara identificada, consultar `GET /clearing-houses/{id}/cycles` o `GET /ach-cycles?clearingHouseId={id}` según la necesidad de integración.
        """;

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        document.Info ??= new OpenApiInfo();
        document.Info.Title = "ACHInterbank API";
        document.Info.Version = "v1";
        document.Info.Description = DocumentDescription;

        return Task.CompletedTask;
    }
}
