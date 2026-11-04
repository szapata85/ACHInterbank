# INFORME TÉCNICO — I05-CENIT (2026-11-03)

## Alcance ejecutado

Se implementó **generación local, explícita y autorizada de un archivo NACHA-M CENIT** a partir de una preparación I03 aprobada en I04 por una segunda persona. El resultado es una evidencia cifrada con huella SHA-256 y trazabilidad; no constituye instrucción de compensación, cambio de saldo, envío, acuse o conciliación. La línea base única es el ZIP acumulativo I04-CENIT, comprobado por CRC, no versiones anteriores.

## Decisiones técnicas

1. **Aislamiento:** el nuevo servicio no llama `AchReturnsService`, no usa modelos de candidatos ACH Colombia ni cambia su modo; la selección y los permisos están en `/api/cenit/...`.
2. **Fuente original:** consulta una vinculación final inequívoca del archivo entrante NACHA-M con cámara CENIT confirmada, registros 1/5/6 y cuenta/monto/código/DFIs/secuencia consistentes. No redondea, inventa ni normaliza silenciosamente registros bancarios.
3. **Autorización:** `POST` requiere permiso `P1Policies.ConfigManage` e identidad en el JWT. La reserva debe estar `APROBADA_SIN_ENVIO` con soporte y justificación. La auditoría identifica al preparador y exige segunda persona; el generador no puede ser el preparador. Revalida `CenitReturnReviewService.ValidateApprovalAsync` de I04 bajo transacción de aislamiento serializable en BD relacional.
4. **Física del archivo:** el motor común NACHA-M `INachaFileBuilder.BuildReturnOutAsync` se usa exclusivamente con perfil de salida CENIT, versión normativa `2026-05-07`, `PersistAudit=false`, sin fallback legacy. El servicio verifica registros 1/5/6/7/8/9, relleno, cuentas, DFI, importes y adenda 99 con secuencias. En I05 se limita a un solo PPD/CCD ordinario por archivo. Las pruebas físicas CENIT existentes continúan sin cambios.
5. **Secuencia:** reutiliza `IAchReturnTraceSequenceService` para obtener 8+7 cifras y evitar colisión a nivel infraestructura. La ejecución relacional utiliza transacción serializable; la comprobación de rollback/concurrencia está pendiente de base real.
6. **Persistencia segura:** amplía la tabla existente `CenitReturnPreparations` con contenido protegido por DataProtection, SHA-256, nombre interno, perfil, secuencia, recuento, usuario/fecha; migraciones SQL Server y PostgreSQL reversibles, snapshots consistentes, `Status` como token de concurrencia y auditoría `AchDbContext`. No hay nueva tabla ni copia NACHA en texto claro.
7. **Sin transporte:** nombre local `NO_TRANSMITIR_CENIT_I05_<id>.txt`; no es naming externo, no hay integración SFTP/MFT/SEBRA, no se crean `AchReturnsGenerated`, `AchFileExport`, eventos financieros, asientos ni jobs. `AUTOMATICO` no crea ejecución sin intervención humana. La descarga requiere `ConfigManage` y vuelve a comprobar modo/ventana, con `Cache-Control: no-store`.

## API y estados

| Operación | Ruta | Permiso | Efecto |
|---|---|---|---|
| Consulta | `GET /api/cenit/devoluciones-automaticas/artefactos/{id}` | ConfigRead | Solo metadatos, `Transmittable=false` |
| Generación local | `POST /api/cenit/devoluciones-automaticas/artefactos/{id}/generar-local` | ConfigManage | Genera una sola vez, guarda cifrado y SHA-256; devuelve descarga no transmisible |
| Descarga local | `GET /api/cenit/devoluciones-automaticas/artefactos/{id}/descargar-local` | ConfigManage | Revalida y verifica huella al descifrar, sin envío |

Transición única I05: `APROBADA_SIN_ENVIO → GENERADA_SIN_ENVIO`. No hay transición a `ENVIADA`, `LIQUIDADA` ni `CONCILIADA`. No se alteraron pantallas SPA ni servicios de ACH Colombia. La revisión I04 mantiene intactas sus acciones de aprobar/descartar.

## Fuentes normativas adjuntas consideradas

- *Manual de Especificaciones Formato NACHA-M CENIT*, 7 mayo 2026, §7.2.1 y Anexo 1.6: devolución como operación nueva, registro 7 adenda 99, referencia de secuencia original de 15 cifras, formato de 106 caracteres y controles de lote/archivo.
- *DSP-152 Anexo 2*, 27 febrero 2025, §3/§4: sesiones, próxima ventana y misma fecha valor, excepciones con plazos especiales.
- *Anexo A – causales CENIT*, 28 noviembre 2023: modalidades R06/R23 y devolución de devolución R60–R69, que no deben extrapolarse automáticamente.

La generación para CTX, R06/R23/R34, prenotificaciones o devoluciones de devoluciones **no está incluida** y no debe inferirse aprobada. Falta comprobar vigencia/compatibilidad completa con homologación del Banco de la República.

## Evidencia e integridad

Ver `EVIDENCIA_DEVOLUCIONES_AUTOMATICAS_I05_CENIT.md`, resultados estáticos, manifiesto e inventario SHA-256. Verificación de fuente I05 **84/84**; regresiones CENIT I01–I04 correctas estáticamente. No se compiló ni ejecutó xUnit, SQL ni integración bancaria. **NO-GO para ambas cámaras.**

## Próximo paso y no incluido

I06-CENIT debe resolver nombre externo y secuencia de archivos, coordinación con la generación manual heredada, persistencia/retención aprobada, firmas, transporte autorizado, idempotencia de acuses, reconciliación y pruebas en ambas bases reales; **no se ha iniciado**. ACH I09-B sigue pendiente y I09-A es condicional.
