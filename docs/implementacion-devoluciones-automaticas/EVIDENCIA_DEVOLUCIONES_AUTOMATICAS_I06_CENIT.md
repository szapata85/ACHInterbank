# EVIDENCIA — I06-CENIT (2026-11-03)

## Origen y alcance verificable

- Archivo base único: `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I05_CENIT_2026-11-03.zip`.
- SHA-256 origen: `bddb26b63c1546edfdc03e72373a273823563e378d40435a751f80fd6f0c606c`.
- `zipfile.ZipFile.testzip()` de origen: `None` (sin error CRC), 4.934 archivos.
- Alcance: implementación de **lectura diagnóstica** y pruebas en fuente. No hubo transporte, simulación de envío ni modificaciones de saldo.

## Comandos realmente ejecutados

| Comprobación | Resultado | Naturaleza |
|---|---|---|
| `python scripts/validate-solution-projects.py` | `SOLUTION GRAPH: OK`, 7/7 proyectos | Estática |
| `python scripts/test/automatic_returns_i06_cenit_static_check.py` | `62/62` | Estática, no xUnit |
| I01–I05 CENIT scripts estáticos | `29/29`, `50/50`, `57/57`, `71/71`, `84/84` | Regresión estática |
| I02/I03/I05/I06/I07/I08 ACH scripts estáticos | `18/18`, `25/25`, `33/33`, `28/28`, `24/24`, `25/25` | Regresión estática |
| I04 ACH script histórico | `32/35` | Pendiente histórico **abierto** |
| `python -m py_compile scripts/test/automatic_returns_i06_cenit_static_check.py` | Sin error | Sintaxis Python |
| `dotnet --info` | `dotnet: command not found` | No disponible |

**No ejecutado:** compilación/restauración .NET, pruebas xUnit incorporadas, SQL Server, PostgreSQL, escenarios HTTP autenticados, ciclo nocturno/corte real, MFT/SEBRA, acuse atribuible, conciliación financiera, homologación. No existe resultado que permita afirmar funcionalidad certificada.

## Casos agregados a la batería xUnit

1. Reserva sin artefacto: informativa y sin escritura.
2. Archivo cifrado íntegro: diagnóstico aún después de la ventana y con modo DESACTIVADO; sin afirmar envío.
3. SHA-256 adulterado, texto no protegido y estructura física cambiada: reportar incoherencia.
4. Devolución de ruta manual y ACK CENIT existente: alertar sin atribuir la aceptación a I05.
5. Traza I05 presente en una exportación CENIT diferente: conflicto para revisión, no entrega.
6. Llaves de descifrado distintas y ACK ACH Colombia: bloqueo/aislamiento.
7. Reserva con cámara ACH Colombia: no visible en consulta I06 CENIT.
8. Límite 1–100, indicador de truncamiento, autenticación y ausencia de POST/PUT/DELETE.

## Semántica de resultados

`INTEGRO_LOCAL` **solo** confirma que los bytes cifrados disponibles coinciden con el hash/estructura registrados. `PENDIENTE_SIN_TRANSMISION` **no** es una orden pendiente de despacho ni un archivo apto para envío. `RESPUESTA_EXTERNA_NO_ATRIBUIBLE_A_I05` identifica una observación potencialmente relacionada con la transacción pero sin nexo comprobable con el archivo local. `FinanciallyReconciled=false` expresa la falta de confirmación financiera, **no** un asiento a corregir automáticamente.

## Integridad de entrega

Los resultados definitivos de conteos del ZIP de salida, listas añadidas/modificadas/conservadas, hashes y prueba CRC están en `MANIFIESTO_DEVOLUCIONES_AUTOMATICAS_I06_CENIT.json`, `INVENTARIO_CONSERVADOS_I06_CENIT.tsv`, `VERIFICACION_INTEGRIDAD_DEVOLUCIONES_AUTOMATICAS_I06_CENIT.json` y `.zip.sha256`. Ninguno de esos archivos prueba ejecución bancaria.

**Producción: NO-GO.** CENIT I06 financiero/operativo todavía abierto; ACH I09-B todavía pendiente.
