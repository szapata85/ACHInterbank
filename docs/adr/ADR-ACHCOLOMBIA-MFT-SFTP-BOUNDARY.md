# ADR — ACH Colombia MFT/SFTP Boundary Contract

## 5.1 Título, estado y alcance

- **Título:** ACH Colombia MFT/SFTP Boundary Contract
- **Estado:** `ACCEPTED_FOR_IMPLEMENTATION`
- **Fecha:** 2026-10-08
- **Ámbito:** exclusivamente ACH Colombia.
- **Fase:** FASE 1 — decisión arquitectónica/documental; sin implementación funcional.

`ACCEPTED_FOR_IMPLEMENTATION` significa únicamente que este contrato arquitectónico interno queda aceptado como base para continuar FASE 2 y posteriores. **No** significa homologación con ACH Colombia, producción, UAT, aceptación de usuario, aprobación comercial ni certificación de infraestructura externa.

> **Nota de cierre FASE 9 (2026-10-09):** la decisión de este ADR permanece vigente. Las referencias `CURRENT (SC900)` describen el baseline que justificó la decisión y se conservan como contexto histórico. FASE 4 implementó posteriormente ambos modos previstos (`ManagedFolder` y `SftpEndpoint` hacia infraestructura CFA), sin cambiar la decisión de ownership. El estado actual es **IMPLEMENTACIÓN COMPLETADA / VALIDACIÓN EJECUTABLE FINAL PENDIENTE**; no existe homologación, UAT ni producción.

Este ADR no modifica comportamiento CENIT ni redefine contratos CENIT.

## 5.2 Contexto

**Jerarquía de autoridad usada en este ADR:**

1. ACH Colombia V35, secciones 2.4.2, 2.4.2.1 y 2.4.2.2, para responsabilidades externas SFTP/MFT.
2. Código SC900 para el comportamiento técnico actualmente implementado.
3. `PLAN_AJUSTE_ACHINTERBANK_ACH_COLOMBIA_QUARTZ_MFT_SFTP.md` para el modelo objetivo acordado.
4. `MEMORY.md`, `docs/CURRENT_PROJECT_STATUS.md` y `docs/ai/MFT_EXTERNAL_READINESS_AUDIT.md` para estado y restricciones internas.

ACH Colombia V35 documenta que su MFT/GoAnywhere (`GAW`) se conecta al **SFTP expuesto por la Entidad Financiera**; el usuario SFTP, el puerto y la ruta son creados/indicados/expuestos por la Entidad. La Entidad deposita archivos en su SFTP; GoAnywhere los recoge y los transfiere a S3 de ACH Colombia; Integra consume desde S3. Para salida desde ACH Colombia, Integra dispone archivos en S3 y GoAnywhere los entrega a la ruta SFTP de cada entidad financiera. V35 también asigna a la Entidad Financiera la responsabilidad de configurar, operar y asegurar ese canal SFTP, incluyendo accesos, credenciales, rutas, permisos y controles de seguridad.

**CURRENT (SC900):**

- `IAchColombiaManagedMftAdapter` define la frontera de handoff/pickup/archive en Application.
- `AchColombiaManagedMftFolderAdapter` es la única implementación concreta registrada actualmente por `Cfa.ACHInterbank.External.DependencyInjectionService`.
- El adapter actual implementa una frontera filesystem administrada: publicación outbound mediante archivo temporal + move final, claim inbound hacia processing y archive.
- `AchColombiaManagedMftConfigurationProvider` resuelve efectivamente habilitación y rutas del modo filesystem, además de `MaximumFileBytes`; no utiliza `Provider`, `Protocol`, `Endpoint`, `Port`, `Principal` ni `ProtectedCredential` para abrir una sesión remota.
- `Provider`, `Protocol`, `Endpoint`, `Port`, `Principal` y credencial protegida se administran/persisten, pero hoy no seleccionan un transporte alternativo ni existe un adapter SFTP efectivo.
- `AchColombiaManagedFileExchangeService` construye el NACHA-M, genera el sobre digital `.env`, conserva contenido/hash/auditoría y entrega el artefacto a `IAchColombiaManagedMftAdapter`.

Por tanto, el problema de esta fase no es implementar SFTP, sino fijar la semántica de la frontera para impedir que una fase posterior modele incorrectamente un “SFTP propiedad de ACH Colombia”.

## 5.3 Decisión principal

**DECISIÓN:** ACH Colombia **NO** se modelará como propietario del SFTP al que ACHInterbank publica.

El concepto arquitectónico estable será:

**`CFA MFT Boundary`**

Flujo outbound conceptual:

```text
ACHInterbank
    |
    v
CFA MFT Boundary / SFTP-MFT expuesto por CFA
    |
    v
GoAnywhere / MFT de ACH Colombia
    |
    v
S3 / Integra ACH Colombia
```

Flujo inbound conceptual:

```text
Integra / S3 ACH Colombia
    |
    v
GoAnywhere / MFT de ACH Colombia
    |
    v
CFA MFT Boundary / SFTP-MFT CFA
    |
    v
ACHInterbank
```

ACHInterbank interactúa con el **CFA MFT Boundary**. GoAnywhere de ACH Colombia interactúa externamente con esa misma frontera conforme al contrato operativo de ACH Colombia V35 y la configuración de infraestructura acordada.

## 5.4 Responsabilidades

| Responsabilidad | Owner | Observación |
|---|---|---|
| Generación NACHA-M | ACHInterbank | **CURRENT.** Usa el motor/perfiles vigentes antes de construir el artefacto outbound. |
| Construcción del sobre `.env` | ACHInterbank | **CURRENT.** `AchColombiaManagedFileExchangeService` usa el servicio de sobre digital antes del handoff. |
| Validación de nombre/formato/hash antes del boundary | ACHInterbank | **CURRENT.** Política de nombre, generación NACHA-M y SHA-256 forman parte del flujo actual; el folder adapter también valida identidad de contenido en el handoff. |
| Publicación outbound en CFA MFT Boundary | ACHInterbank | **CURRENT** para `ManagedFolder`; **TARGET** para cualquier otro mecanismo interno. La responsabilidad termina en la frontera CFA, no en S3/Integra. |
| Servidor SFTP expuesto | CFA / Infraestructura | V35 asigna a la Entidad Financiera la exposición y operación del SFTP. “Owner” aquí significa owner operativo de la frontera, no propiedad de software ACH Colombia. |
| Creación/administración del usuario SFTP externo | CFA / Infraestructura | V35: usuario SFTP creado e indicado por la Entidad. |
| Puerto SFTP expuesto | CFA / Infraestructura | V35: puerto de conexión expuesto por la Entidad. Valor concreto: **PENDING_DEPLOYMENT_CONFIGURATION**. |
| Rutas INPUT/OUTPUT | CFA / Infraestructura | V35: ruta/directorio indicado por la Entidad. Nombres físicos: **PENDING_PATH_CONFIGURATION**. |
| Whitelist/firewall | CFA / Infraestructura | CFA permite el origen autorizado de ACH conforme al contrato externo. No es lógica de negocio de ACHInterbank. |
| Pickup de INPUT por GoAnywhere | ACH Colombia / GoAnywhere | V35: GoAnywhere detecta los archivos depositados por la Entidad y los mueve hacia S3. |
| Transporte CFA -> ACH Colombia | ACH Colombia / GoAnywhere + frontera CFA disponible | GoAnywhere ejecuta el transporte posterior al boundary; ACHInterbank no administra GoAnywhere. |
| S3 / Integra | ACH Colombia | V35: GoAnywhere deposita en S3 e Integra consume/procesa desde S3. |
| Publicación de respuestas/salidas de ACH Colombia | ACH Colombia / Integra + GoAnywhere | Integra dispone en S3; GoAnywhere entrega a la ruta SFTP de la Entidad. |
| Pickup inbound por ACHInterbank | ACHInterbank | **CURRENT** mediante `PickupInboundAsync` desde la frontera administrada. |
| Archive local/aplicativo inbound | ACHInterbank | **CURRENT** mediante `ArchiveInboundAsync` y trazabilidad persistida. |
| Retry de aplicación | ACHInterbank | Retry al generar/publicar hasta el CFA MFT Boundary y al reclamar/procesar/archivar inbound ya depositado. |
| Retry de transporte MFT externo | Enterprise MFT / infraestructura externa | El transporte posterior y sus reintentos pertenecen al canal MFT/GoAnywhere y al contrato operativo externo. No se coordinan por suposición implícita desde ACHInterbank. |
| Evidencia de pickup/aceptación externa | **PENDING_EXTERNAL_OBSERVABILITY_CONTRACT** | SC900 no dispone hoy de una evidencia técnica suficiente de pickup de GoAnywhere ni aceptación de Integra/ACH Colombia. |

## 5.5 Semántica INPUT / OUTPUT

Los términos INPUT/OUTPUT se definen **desde la perspectiva de la frontera CFA para el intercambio con ACH Colombia**, no como nombres físicos obligatorios de directorio.

- **INPUT:** ruta de CFA donde se publican archivos que salen de CFA hacia ACH Colombia para que GoAnywhere los recoja.
- **OUTPUT:** ruta de CFA donde GoAnywhere entrega archivos provenientes de ACH Colombia para consumo de ACHInterbank.

Configuración física:

```text
InputPath  = PENDING_DEPLOYMENT_CONFIGURATION
OutputPath = PENDING_DEPLOYMENT_CONFIGURATION
```

No se fijan `/input`, `/output` ni otros nombres físicos en este ADR. Los nombres/rutas definitivos pertenecen a `PENDING_PATH_CONFIGURATION`.

La terminología actual de SC900 (`OutboundLocation`, `InboundLocation`, `ProcessingPath`, `ArchiveLocation`) permanece **CURRENT** y sin cambios en esta fase. INPUT/OUTPUT es semántica contractual de la frontera, no un rename de propiedades existentes.

## 5.6 Modos internos soportados

### A. ManagedFolder

**CURRENT / ARCHITECTURALLY SUPPORTED.**

ACHInterbank accede directamente a un filesystem/storage compartido o montado que representa el **CFA MFT Boundary**.

`AchColombiaManagedMftFolderAdapter` es conceptualmente compatible con este modo y es la implementación real actual de SC900.

Este modo no implica que el filesystem sea S3 de ACH Colombia ni que ACHInterbank opere GoAnywhere. La frontera termina en infraestructura CFA.

### B. SftpEndpoint

**TARGET — NOT IMPLEMENTED.**

ACHInterbank podrá conectarse mediante SFTP al servidor corporativo CFA cuando la topología de despliegue no exponga el boundary como filesystem compartido/montado.

`SftpEndpoint` apunta a infraestructura **CFA**.

`SftpEndpoint` **NO** apunta a un “SFTP de ACH Colombia”.

FASE 1 no agrega adapter SFTP, cliente SFTP, librería, DI ni selección de transporte.

## 5.7 Selección del modo

`ManagedFolder` y `SftpEndpoint` son alternativas de integración **interna** entre ACHInterbank y el CFA MFT Boundary.

No existe evidencia suficiente para declarar uno de ellos como único modo productivo. La selección final queda como:

**`PENDING_DEPLOYMENT_TOPOLOGY_DECISION`**

**CURRENT:** SC900 registra únicamente `AchColombiaManagedMftFolderAdapter`; `Provider` y `Protocol` no seleccionan adapter.

**TARGET:** el diseño de fases posteriores podrá hacer que `Provider`/`Protocol` participen en la selección del mecanismo correspondiente, preservando la abstracción `IAchColombiaManagedMftAdapter`.

Este ADR no implementa resolver ni modifica DI.

## 5.8 Regulación versus operación CFA

Se establece como invariante arquitectónica:

**`REGULATORY CYCLE POLICY != CFA OPERATIONAL SCHEDULING`**

La política regulatoria describe ventanas/cutoffs de ACH Colombia. La programación operacional describe cuándo CFA decide iniciar automatizaciones dentro de las restricciones permitidas.

Ejemplos conocidos para la candidata 7C, sin activarla:

- **C1**
  - cutoff del modelo regulatorio candidato: **07:00**;
  - regla operacional conocida: primera tarea automática **06:30**.
- **C7**
  - cutoff del modelo regulatorio candidato: **18:00**;
  - regla operacional conocida: **no iniciar nuevas generaciones automáticas después de 17:50**.

`06:30` y `17:50` **NO** reemplazan `StartTime`/`CutoffTime` regulatorios y no deben hardcodearse dentro de la política regulatoria para simular programación operativa.

La parametrización operativa se implementará posteriormente en FASE 2 y su ejecución dinámica en FASE 3.

Para **C2-C6** no se fija ni se infiere una hora automática exacta en este ADR. Si negocio/operación requiere esos instantes, quedan bajo `PENDING_C2_C6_OPERATIONAL_TIMES`.

## 5.9 ACHCOL-7C-CANDIDATE

**CURRENT:**

- `ACHCOL-7C-CANDIDATE` existe.
- Permanece **INACTIVE / NOT ACTIVATABLE**.
- `EffectiveFrom` continúa pendiente (`null` en el catálogo candidato actual).
- El inicio oficial de C4 continúa pendiente de autoridad formal; el código candidato conserva `StartTime = null` para C4.
- ACH Colombia V35 histórica de cinco ciclos permanece separada e intacta como política histórica.

Este ADR no activa la candidata, no define `EffectiveFrom` y no resuelve C4 por inferencia. En particular, no eleva `11:01 a. m.` a valor regulatorio oficial.

**TARGET:** el diseño posterior puede soportar técnicamente una política 7C sin activarla hasta que se resuelvan `PENDING_7C_EFFECTIVE_FROM` y `PENDING_7C_C4_START_AUTHORITY`.

## 5.10 Estados y semántica de custodia

**CURRENT (SC900):** `AchManagedFileTransferStatus.Transferred` se asigna cuando `IAchColombiaManagedMftAdapter.HandoffOutboundAsync` retorna éxito. Con el único adapter real actual, `AchColombiaManagedMftFolderAdapter`, ese éxito significa que el archivo fue confirmado en el **ManagedFolder boundary** (`HANDOFF_COMMITTED` o archivo idéntico ya comprometido).

Por tanto, `Transferred` actualmente significa **handoff exitoso al boundary filesystem administrado por CFA**.

`Transferred` **NO** significa:

- pickup por GoAnywhere;
- entrega a S3 de ACH Colombia;
- procesamiento por Integra;
- aceptación por ACH Colombia.

Como semántica objetivo puede usarse conceptualmente el término:

**`PublishedToMftBoundary`**

pero FASE 1 **NO** renombra enums, no cambia DB, no modifica código y no agrega un estado persistido. La diferencia queda registrada para fases posteriores.

## 5.11 Evidencia de pickup

**CURRENT:** ACHInterbank no dispone de evidencia técnica suficiente para afirmar pickup de GoAnywhere ni aceptación remota por ACH Colombia/Integra.

Clasificación:

**`PENDING_EXTERNAL_OBSERVABILITY_CONTRACT`**

No se inventa ACK ni receipt.

Si en el futuro la plataforma MFT expone un contrato formal verificable —por ejemplo audit event, receipt, API, desaparición del archivo con semántica fuerte acordada u otro mecanismo explícito— podrá modelarse una evidencia posterior de custodia/pickup/aceptación según lo que dicho contrato realmente pruebe.

La simple publicación al CFA MFT Boundary **NO** será interpretada como aceptación ACH Colombia.

## 5.12 Ownership de retries

Los límites de retry quedan explícitos:

### OUTBOUND

**ACHInterbank** es responsable de reintentar mientras intenta generar y publicar correctamente el artefacto al **CFA MFT Boundary**, respetando su idempotencia, contenido retenido, hash, límites y estados de ejecución.

Una vez que la custodia del boundary ha sido confirmada por el contrato del adapter interno, los retries de transporte posterior pertenecen al **Enterprise MFT / infraestructura externa / GoAnywhere**, según el contrato operativo aplicable.

### INBOUND

**ACHInterbank** es responsable de reclamar, procesar y archivar idempotentemente el artefacto que ya fue depositado en el CFA MFT Boundary.

La entrega desde ACH Colombia/GoAnywhere hasta el boundary y sus reintentos externos pertenecen al canal MFT externo.

No se coordinarán retries entre ACHInterbank y MFT mediante suposiciones implícitas. La idempotencia de ACHInterbank debe permitir reintentos sin duplicar efectos funcionales.

## 5.13 Seguridad

Contrato de seguridad para fases posteriores; **sin implementación en FASE 1**:

- secretos no se devolverán al frontend después de su almacenamiento;
- secretos no se escribirán en logs;
- configuración sensible se mantendrá protegida;
- un futuro `SftpEndpoint` deberá validar la host key del servidor CFA;
- no se aceptará ciegamente una host key en producción;
- se aplicará mínimo privilegio a identidades, rutas y permisos;
- rutas, permisos y controles del SFTP serán administrados por infraestructura CFA;
- la IP origen autorizada de ACH se manejará como whitelist/firewall externo, no como lógica de negocio ACHInterbank.

**CURRENT:** SC900 ya persiste la credencial mediante protección y no la utiliza todavía para abrir una sesión SFTP efectiva. Este ADR no amplía ese comportamiento.

## 5.14 No objetivos

FASE 1 **NO**:

- implementa SFTP;
- implementa resolver de adapters/transporte;
- cambia Quartz;
- cambia polling outbound o inbound;
- cambia el modelo persistente;
- crea entidades o migraciones;
- cambia DTOs;
- cambia API;
- cambia SPA;
- cambia estados DB;
- cambia handlers;
- cambia adapters;
- agrega librerías o cliente SFTP;
- cambia DI;
- activa `ACHCOL-7C-CANDIDATE`;
- resuelve `EffectiveFrom`;
- resuelve C4;
- define horas automáticas C2-C6;
- homologa GoAnywhere;
- conecta o prueba infraestructura productiva/external;
- modifica CENIT.

## 5.15 Decisiones pendientes

- **`PENDING_DEPLOYMENT_TOPOLOGY_DECISION`** — decidir si producción integra ACHInterbank con el CFA MFT Boundary mediante `ManagedFolder` o `SftpEndpoint` (o si ambos permanecen disponibles por despliegue).
- **`PENDING_PATH_CONFIGURATION`** — definir nombres/rutas físicas INPUT/OUTPUT, processing y archive conforme a la topología aprobada.
- **`PENDING_EXTERNAL_OBSERVABILITY_CONTRACT`** — definir evidencia formal que pruebe pickup/custodia/aceptación externa y su semántica exacta.
- **`PENDING_7C_EFFECTIVE_FROM`** — obtener vigencia oficial de la política candidata de siete ciclos.
- **`PENDING_7C_C4_START_AUTHORITY`** — obtener autoridad formal utilizable para el inicio de C4.
- **`PENDING_C2_C6_OPERATIONAL_TIMES`** — definir horas automáticas exactas para C2-C6 solo si negocio/operación decide configurarlas.

Estos pendientes no se reinterpretan automáticamente como bloqueos de FASE 2. FASE 2 puede modelar la programación operativa de forma conservadora sin hardcodear topología, rutas, C4, `EffectiveFrom` ni horas C2-C6 no confirmadas.

## 5.16 Consecuencias

### Positivas

- separa claramente responsabilidades entre ACHInterbank, infraestructura CFA y ACH Colombia;
- evita confundir un SFTP de CFA con un supuesto “SFTP de ACH Colombia”;
- mantiene compatibilidad arquitectónica tanto con deployment `ManagedFolder` como con un futuro `SftpEndpoint`;
- preserva el dominio y el puerto `IAchColombiaManagedMftAdapter` como frontera de aplicación;
- permite evolucionar hacia programación operacional persistente y Quartz dinámico sin contaminar la política regulatoria;
- preserva la semántica actual sin convertir `Transferred` en evidencia de aceptación externa.

### Costos / trade-offs

- fases posteriores requieren modelo persistente de programación operacional;
- si la topología productiva usa SFTP, será necesario implementar resolver/adapter SFTP y sus controles de seguridad en una fase posterior;
- requiere definición de topología, rutas y contrato de observabilidad externos;
- requiere homologación externa antes de afirmar entrega real/aceptación por ACH Colombia.

## 5.17 Criterios de aceptación del ADR

- [x] CFA es propietario/expositor operativo del SFTP de la frontera de la Entidad Financiera.
- [x] GoAnywhere ACH se conecta al SFTP CFA.
- [x] ACHInterbank publica/consume en el CFA MFT Boundary.
- [x] `ManagedFolder` permanece válido.
- [x] `SftpEndpoint` se define sin implementarse.
- [x] No se atribuye el SFTP a ACH Colombia.
- [x] INPUT/OUTPUT están semánticamente definidos sin inventar rutas físicas.
- [x] Regulación y operación CFA están separadas.
- [x] `06:30` y `17:50` se clasifican como reglas operacionales, no como reemplazos de cutoffs regulatorios.
- [x] C2-C6 no reciben horarios automáticos inventados.
- [x] `Transferred` no equivale a aceptación ACH Colombia.
- [x] Ownership de retries está definido por lado del boundary.
- [x] `ACHCOL-7C-CANDIDATE` permanece inactiva y no activable mientras sus bloqueos documentales sigan abiertos.
- [x] No hubo cambios funcionales en FASE 1.
