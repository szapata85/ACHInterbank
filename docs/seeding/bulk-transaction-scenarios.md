# Bulk transaction seeding (Development / Testing)

Este proyecto ahora incluye un seeder automático para escenarios de carga masiva ACH:

- `SEED-BULK-VALID-*`: lote completamente válido.
- `SEED-BULK-MIXED-*`: lote con mezcla de tipos permitidos (`Credit`, `Debit`, `Prenotification`, `Reversal`).
- `SEED-BULK-PARTIAL-ANCHOR-*`: anclas para pruebas de éxito parcial (incluye referencias `SEED-BULK-PARTIAL-EXIST-001/002` que deben chocar si se reutilizan en un request masivo).
- `SEED-BULK-VOLUME-*`: lote de volumen razonable (180 transacciones) para pruebas básicas de performance y UI.

## Cómo ejecutar el seeding

1. Levantar la API en `Development` o `Testing`.
2. Ejecutar el endpoint:

```http
POST /Maintenance/seed
```

También se puede ejecutar por startup si se integra `DbInitializer.SeedAllAsync(...)` en un pipeline de entorno controlado.

## Payload sugerido para probar éxito parcial con el endpoint `/transactions/bulk`

Usa este ejemplo después del seeding para validar respuesta parcial:

```json
{
  "batchReference": "MANUAL-PARTIAL-TEST-001",
  "chunkSize": 100,
  "transactions": [
    {
      "amount": 250000,
      "reference": "SEED-BULK-PARTIAL-EXIST-001",
      "type": 1,
      "accountType": 1,
      "isPrenotification": false,
      "destinationInstitutionId": 1,
      "sourceAccountNumber": "122000999001",
      "destinationAccountNumber": "411000999001",
      "companyName": "EMPRESA SEMILLA",
      "companyIdentification": "900123456",
      "companyEntryDescriptionId": 1,
      "recipientIdNumber": "1011121314",
      "recipientName": "CLIENTE EXISTENTE",
      "requiresIdentityValidation": false,
      "addendas": [{ "addendaType": "05", "information": "PRUEBA PARCIAL 1" }]
    },
    {
      "amount": 300000,
      "reference": "MANUAL-PARTIAL-OK-001",
      "type": 1,
      "accountType": 1,
      "isPrenotification": false,
      "destinationInstitutionId": 1,
      "sourceAccountNumber": "122000999002",
      "destinationAccountNumber": "411000999002",
      "companyName": "EMPRESA SEMILLA",
      "companyIdentification": "900123456",
      "companyEntryDescriptionId": 1,
      "recipientIdNumber": "2021222324",
      "recipientName": "CLIENTE NUEVO",
      "requiresIdentityValidation": true,
      "addendas": [{ "addendaType": "05", "information": "PRUEBA PARCIAL 2" }]
    }
  ]
}
```

Esperado: 1 fallida por referencia existente + 1 exitosa.

> Nota: si en tu ambiente el catálogo activo no inicia en `Id=1`, ajusta `companyEntryDescriptionId` al primer concepto activo disponible.

## Recomendaciones para ambientes

- **Development**: mantener habilitado para demos funcionales y validación visual de la pantalla masiva.
- **Testing**: útil para pruebas automáticas de regresión (incluyendo escenarios de parcialidad y volumen).
- **Producción**: no habilitar seeders de escenarios de prueba.
