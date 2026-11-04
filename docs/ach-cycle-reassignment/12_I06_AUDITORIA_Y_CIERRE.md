# I06 — Auditoría de cierre estático ACH-CR-001

**Fecha:** 2026-10-25. **Base:** `ACHInterbank_ACH_CR_I05_COMPLETO.zip`. **Resultado objetivo:** `ACHInterbank_ACH_CR_I06_COMPLETO.zip`. **Naturaleza:** cierre de implementación *estática*, no de funcionamiento probado ni de producción.

## 1. Inventario de integración

- **Domain:** sin nuevas entidades ni estados financieros; no se agregan dependencias EF/Quartz.
- **Application:** contratos del servicio read-only I01 y comando I02; sin dependencia directa a infraestructura.
- **Persistence:** consulta I01, movimiento I02 con `Serializable`, auditoría existente `AchTransactionStateEvent`, `IAchBatchRepository`, `ITransactionPersister` y frontera `IAchFileExportAuditService` reforzada I05.
- **Quartz:** handler I03 como adaptador `ITaskHandler` del sistema dinámico, task seed `Disabled`, `ManualExecutionEnabled=false`, `RetryOnFailure=false`, `RequestsRecovery=false` y `AchCycleReassignment:EnableExecution` false cuando falta.
- **Proyección:** I04 acepta solo ciclo futuro de día hábil en creación no-CENIT. Una transacción existente no cambia fecha valor por el comando I02.
- **CENIT:** motores de cola/optimización/compensación existentes no se sustituyen. CENIT/ACH Colombia conservan política de cámara, perfiles y reglas diferentes.
- **NACHA:** I05 integra membresías exactas y admisión serializable en rutas conocidas, sin poder afirmar atomicidad de DB + archivos + red.

## 2. Cambios I06 (no funcionales)

1. Corrección focal en `validate-cycle-reassignment-i05-static.py`: verificar memoria **histórica** I05 aun cuando `INCREMENT_CURRENT=I06`; evita false-negative documental sin debilitar requisitos de evidencia.
2. Tres textos obsoletos en comentarios/descripción del servicio I02, handler I03 y seeder Quartz quedan sustituidos por `homologación pendiente`, manteniendo Disabled.
3. Nuevo validador I06 y documentos finales as-built, plan de prueba/operación GO/NO-GO, CHANGELOG y MEMORY_STATE.
4. **No** se cambian ramas de ejecución de negocio, contratos de cámara, DB schema, migraciones, MFT ni golden files en I06.

## 3. Estado de escenarios

| Escenario | Estado razonable en I06 | Evidencia pendiente |
|---|---|---|
| Sábado→lunes (originación) | Código y tests escritos | Build + test calendario + datos de cámara |
| C1 vence, pasar al próximo admisible mismo día | Código I01/I02/I03 + tests escritos | Base relacional, Quartz opt-in controlado |
| Ciclo restringido para el tipo | Filtrado por `ICycleTransactionPolicy` | Catálogo efectivo y contraejemplos |
| Último ciclo, fecha valor distinta | **No autorizado / sin traslado** | Política contractual aprobada o resolución explícita |
| Archivo NACHA iniciado, carrera exportación vs I02 | Validación + registro bajo Serializable en rutas conocidas | SQL Server y PostgreSQL, dos procesos, deadlock/retry |
| Archivo no llegó/ACK desconocido | Fail-closed | Conciliación con contraparte, nunca reenvío ciego |
| CENIT con cola/liquidez/ejecución iniciada | Guardia conservadora | Prueba de regresión en entorno representativo |
| Candidatos inelegibles ocupan límite de escaneo | Riesgo de inanición reconocido | Medición con volumen, sin optimizar a ciegas |

## 4. Pruebas estáticas de este incremento

Ejecutar sobre el ZIP final y publicar resultados objetivos de:

- `validate-cycle-reassignment-static.py --zip` y scripts `-i01-` a `-i06-`.
- `validate-solution-projects.py` (7 proyectos) y verificación de referencias entre capas.
- `zipfile.testzip()`, verificación de duplicados, diff ruta/CRC/tamaño I05 → I06 y SHA-256 del ZIP final.
- Confirmación explícita: `dotnet`/SQL/Docker/cámara no disponibles → *no ejecutados*, jamás reemplazados por simulaciones ficticias.

El archivo externo `ACH_CR_I06_VALIDATION.json` incluye comandos, resultados y hashes después de generar el ZIP. No incrustar el propio SHA-256 en el ZIP porque produciría una referencia circular.

## 5. Hallazgos pendientes de aceptación

1. **Riesgo release (NO-GO):** sin compilación, xUnit, SQL Server/PostgreSQL ni ensayo real de dos escritores; no se puede certificar doble-envío cero solo con `Serializable` leído.
2. **Guardia conservadora:** archivo NACHA de otra operación del ciclo fuente puede inmovilizar una transacción no enviada. Esta es una política segura por defecto que exige monitoreo/manual review, no un defecto automáticamente corregible.
3. **Starvation:** el límite `MaxScanned` puede examinar mismos IDs omitidos en cada corrida. Medir antes de añadir cursor persistente.
4. **Multiarchivo:** admisión DB y publicación física no comparten commit global; una partición fallida puede dejar evidencias parciales. Nunca reintentar sin reconciliación.
5. **Rollover fecha valor:** por diseño no se modifica a un nuevo día durante traslado; no inferir autorización contractual.
6. **ACH Colombia 7C candidata:** no activa; faltan `EffectiveFrom` y confirmación del comienzo de C4.

## 6. Criterio de cierre

**Cierre I00–I06:** sí, en entrega estática de código/documentación y artefacto íntegro. **Homologado:** no. **UAT:** no iniciada. **Producción:** NO-GO. **Incrementos restantes:** 0; los trabajos de validación y posibles remediaciones posteriores son nuevos acuerdos de alcance.
