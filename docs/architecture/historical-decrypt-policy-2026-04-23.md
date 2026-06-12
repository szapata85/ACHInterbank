> Nota G3.5.2: las referencias a proveedores de secretos retirados son historicas y obsoletas desde el cleanup `ebf7a8a5`; no describen el stack vigente.

# Historical Decrypt Policy (expired-but-retained) — 2026-04-23

## Estados funcionales
- **Active**
- **Expired**
- **Revoked**
- **Replaced**
- **Purged/Deleted** (sin metadata utilizable o sin `SecretRef` resoluble)

## Reglas por estado
| Estado | Firmar nuevo | Cifrar nuevo | Descifrar histórico |
|---|---|---|---|
| Active | Sí | Sí | Sí |
| Expired | No | No | Sí (si retención cumple) |
| Revoked | No | No | Solo si política lo permite (por defecto: No) |
| Replaced | No para nuevo | No para nuevo | Sí para histórico si `SecretRef` válido |
| Purged/Deleted | No | No | No |

## Regla central
- `Expired` se permite **únicamente** en `HistoricalDecrypt`.
- Nunca se usa `Expired` para operaciones nuevas de salida.

## Condiciones obligatorias para historical decrypt
1. Existe versión histórica en BD para `InboundDecryption`.
2. Existe `SecretRef` en la versión.
3. proveedor de secretos retirado resuelve secreto.
4. Operación auditada con `usageReason=HistoricalDecrypt`.

## Criterio de resolución
- Se selecciona versión histórica por `issuer+serial` (y opcionalmente thumbprint) proveniente del `recipientInfo.certificateInfo` del sobre digital.
- No se usa “certificado activo actual” para decrypt histórico si el sobre refiere otra versión.

## Política de retención/purge
- **No purgar** versiones con potencial de decrypt histórico dentro de ventana regulatoria/operativa.
- **Se puede purgar** cuando la ventana expiró y existe aprobación de cumplimiento.
- Purga implica pérdida de capacidad de decrypt histórico para esos artefactos.

## Criterios de rechazo
- versión histórica no encontrada;
- `SecretRef` faltante/inválido;
- secreto purgado/no resoluble en proveedor de secretos retirado;
- `Revoked` sin permiso explícito de historical decrypt.

## Evidencia operativa recomendada (UAT)
- Ejecutar `scripts/proveedor de secretos retirado/run-historical-decrypt-e2e-uat.sh`.
- Confirmar en logs y BD:
  - `CertificateUsageLogs.OperationType = HistoricalDecrypt`.
  - `ContextJson` incluye `UsageReason=HistoricalDecrypt` y `SecretRefMasked`.
  - no hay persistencia de material privado en tablas de metadata.
