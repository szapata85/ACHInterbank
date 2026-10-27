# F5.1 — Homologación de fuentes `Proc_Transacciones` para ACHCOL sin ciclo interno

**Estado:** `IMPLEMENTED_UNVERIFIED` (inventario + guard código; campos externos no homologados).  
**Base:** `ACHInterbank_ACHCOL_CFA_F4_3_UNIFICADO.zip`.  
**Siguiente:** F5.2, adapter por entrada + DryRun sin Live.

## Alcance y evidencia

- Se cotejó el catálogo `IntegrationCatalogBootstrapper.BuildProcTransaccionesTechnicalCatalog` (25 entradas, 2 salidas `RTAACH` / `RTALOC`) con `IntegrationMappingBootstrapper.ProcTransaccionesSourcePathFor`, el mapper tradicional y el contexto NACHA persistido.
- El WSDL suministrado confirma operación `Proc_Transacciones` y su `soapAction`, pero **referencia XSD externos (`xsd0`/`xsd1`) no entregados**. El catálogo es evidencia del repositorio, **no** homologación de esquema por CFA.
- Estados: **DirectCandidate** = fuente observable en NACHA, **Conditional** = conversión/catálogos/regla pendiente, **Unresolved** = origen no probado. Ninguno implica autorización externa para `Live`.

## Matriz campo a campo

| Parámetro | Fuente NACHA / contexto candidata | Estado | Riesgo / condición |
|---|---|---|---|
| `TREG` | "6" (tipo de registro 6) | `Conditional` | Constante de operación; confirmar regla del contrato CFA |
| `TIPTRAN` | EntryDetail.TransactionCode | `DirectCandidate` | Validación monetaria F4.1; confirmar equivalencia del código |
| `BCORECEP` | EntryDetail.ReceivingParticipantEntityCode / FinancialInstitutions.TransitCode | `Conditional` | Resolver institución y normalizar 3 dígitos, no truncar 8 a 3 |
| `BCOORIG` | BatchHeader.OriginParticipantEntityCode / FinancialInstitutions.TransitCode | `Conditional` | Requiere asociación única con catálogo de instituciones |
| `NORIG` | BatchHeader.CompanyName / entidad originadora | `Conditional` | Homologar si es empresa o institución |
| `NCTAORIG` | No documentada | `Unresolved` | Cuenta del originador no está inequívocamente en EntryDetail receptor |
| `IDORIG` | BatchHeader.CompanyId | `Conditional` | Confirmar si es NIT/identificación contractual |
| `DESTRAN` | BatchHeader.CompanyEntryDescription + Addenda | `Conditional` | Normalización de descripción requiere regla aprobada |
| `FECEFEC` | BatchHeader.EffectiveEntryDate | `Conditional` | Formato/fecha efectiva debe ser confirmado |
| `NCTARECEP` | EntryDetail.AccountNumber | `DirectCandidate` | Cuenta receptora; validar modalidad/tipo |
| `MONTO` | EntryDetail.Amount | `DirectCandidate` | Confirmar unidad y precisión monetaria |
| `NRECEP` | EntryDetail.RecipUserName | `DirectCandidate` | Verificar reglas de longitud y codificación |
| `IDRECEP` | EntryDetail.RecipIdNumber | `DirectCandidate` | Verificar tipo documento/longitud |
| `DISCRE` | EntryDetail.DiscreData | `DirectCandidate` | Opcional; no enviar por defecto sin mapeo publicado |
| `CONV` | No documentada | `Unresolved` | No hay origen inequívoco de convenio |
| `PROD` | No documentada | `Unresolved` | No hay origen inequívoco de producto |
| `INFPAG` | AddendaRecord.PaymentRelatedInformation + BatchHeader | `Conditional` | Debe validar addenda y política de información de pago |
| `IDTRAN` | EntryDetail.SequenceNumber (últimos 7) | `Conditional` | Trace de 15 dígitos y rango/transacción homologada; no usar ID de BD |
| `IDLOTE` | BatchHeader.BatchNumber | `Conditional` | Convertir 7 dígitos a 6 solo bajo política de rango existente |
| `REGLOTE` | No documentada | `Unresolved` | Registro secuencial CFA aún no homologado |
| `IREVER` | No documentada | `Unresolved` | Confirmar semántica de indicador 0/1 y tipos de movimiento |
| `LIBRE` | No documentada | `Unresolved` | Sin regla de negocio aprobada |
| `IDCAMCOMPE` | ClearingHouse ACHCOL | `Conditional` | Identificador CFA/cámara requiere homologación; nunca ciclo externo N |
| `DIRECCIONIP` | No documentada | `Unresolved` | IP no aparece en NACHA; no inventar |
| `LIBRE1` | No documentada | `Unresolved` | Sin regla de negocio aprobada |

## Hallazgos críticos

1. `IDTRAN`: el mapeo legado `transaction.traceSequenceNumber` no existe en un `EntryDetail` aislado; usar el trace validado requerirá homologar longitud y representación CFA. **No usar `EntryDetailID` ni GUID interno.**
2. `IDLOTE`: el código existente `ToFunctionalBatchId` restringe 7→6 dígitos y el rango. Confirmar que esa transformación es aplicable al nuevo origen y evitar truncamiento.
3. `IDCAMCOMPE`: el bootstrap actual usa `cycle.clearingHouseId` y el mapper exige entidad `AchCycle`; ACHCOL ya conserva cámara independiente de ciclo. **No usar `ExternalCycleNumber` como ID de cámara o ciclo.**
4. `BCOORIG`/`BCORECEP`: el NACHA trae códigos de participante de ocho dígitos, mientras el mapper legado utiliza transit code institucional de tres. Exige resolución por catálogos autorizados y consistencia de CFA.
5. `NCTAORIG`, `CONV`, `PROD`, `IREVER`, `REGLOTE`, `LIBRE`, `DIRECCIONIP`, `LIBRE1`: sin evidencia suficiente; conservar política fail-closed, especialmente los obligatorios.
6. La ruta tradicional admite `AchTransaction`/`AchCycle` y ahora rechaza explícitamente `SourceKind=AchColFileBased` antes de generar mapping, hasta que exista resolución por entrada en F5.2.
7. La aplicación permite un motor configurable de mappings; la validación real deberá aplicar sobre el mapping publicado y snapshot correcto, sin modificar los mapeos del programa paralelo CFAUX.

## Cambios incorporados

- `ProcTransaccionesRequestMapper.cs`: barrera para impedir asignar ciclos/transacciones ficticios en la API legacy para ACHCOL.
- `AchColProcTransaccionesFieldSources.cs`: catálogo tipado y pequeño, centralizado, sin nueva tabla, sin nueva cola y sin servicio externo.
- `AchColProcTransaccionesFieldSourcesF51Tests.cs`: cobertura de 25 parámetros, no equivalencia `N`/ciclo, bloqueo del contrato y separación de origen.
- Se actualizan `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, `README_PAQUETE_UNIFICADO.md`.

## Decisiones F5.1

- **DEC-061**: fuentes del catálogo son candidatas, nunca valores CFA certificados por defecto.
- **DEC-062**: mapa de 25 entradas + 2 salidas separado; jamás usar campos de respuesta como entradas ni datos `SEED` en despacho.
- **DEC-063**: `IDCAMCOMPE` se deriva del contexto cámara homologado, jamás de `ExternalCycleNumber` ni `AchCycleId` inventado.
- **DEC-064**: no modificar rutas ni mapping sets CFAUX o `CycleBound`; la API legada rechaza ACHCOL, hasta que F5.2 adapte el mapper por entrada.
- **DEC-065**: con XSD importados faltantes, decisión de semántica de campos pendiente de CFA; se continúa implementación sin habilitar Live ni inventar contrato.

## Verificación y pendientes

- Comprobaciones estructurales locales de inventario y guard: aplicadas en código; xUnit no ejecutado (`dotnet` ausente).
- **F5.2**: implementar resolución segura por `EntryDetail`, evaluar mapping sets publicados/campos obligatorios, emitir SOAP solo en `DryRun` y preservar bloqueos para valores no homologados.
- **F6**: obtener esquemas XSD, validar contrato real con CFA, ejecutar pruebas .NET, PostgreSQL/SQL Server y homologación E2E. Sin despliegue financiero Live aprobado.
