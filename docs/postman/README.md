# Postman Collection - ACHInterbank API

Archivo generado:
- `ACHInterbank-Full.postman_collection.json`

Incluye endpoints detectados automáticamente desde los controladores en `src/Cfa.ACHInterbank.Api/Controllers`.

## Variables recomendadas en Postman
- `baseUrl`: `http://localhost:843`
- `token`: JWT válido (sin prefijo `Bearer`)

## Notas
- Para endpoints con `[FromBody]`, se incluyó un JSON de ejemplo.
- Cuando no fue posible inferir un DTO exacto, el body incluye un objeto placeholder:
  - `{"_note":"Ejemplo para <Tipo>","value":"sample"}`
- Endpoints con `multipart/form-data` (por ejemplo upload de certificado) quedan listados, pero deberás cambiar el body manualmente a `form-data` en Postman.
