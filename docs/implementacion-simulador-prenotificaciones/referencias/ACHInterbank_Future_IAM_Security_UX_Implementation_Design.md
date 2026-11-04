# ACHInterbank — Diseño de implementación futura para IAM, seguridad y UX/UI

**Estado:** propuesta ejecutable para implementación futura  
**Objetivo:** permitir que ChatGPT, trabajando en modo chat sobre el repositorio, ejecute una modernización controlada de creación de usuarios, roles, contraseñas, MFA, auditoría, UX/UI y branding dinámico sin alterar la lógica normativa/operacional ACH/CENIT no relacionada.  
**Base técnica esperada:** .NET + EF Core + Angular 21 + Angular Material 21.  
**Naturaleza del documento:** plan de ejecución, contrato técnico, máquina de estados y formato de memoria. No constituye por sí solo certificación de cumplimiento.

---

## 1. Propósito

Este documento define una implementación futura, por fases, para corregir las brechas detectadas en el R10 de ACHInterbank y dejar una base IAM/seguridad apta para un sistema financiero de alto control. Está escrito para que una sesión de ChatGPT pueda ejecutar el trabajo de forma incremental, verificable y recuperable entre conversaciones.

El diseño preserva explícitamente:

- Angular Material como biblioteca principal de UI.
- Branding dinámico por entidad/participante.
- Clean Architecture y separación Domain / Application / Persistence / API / Frontend.
- Configuración y catálogos como fuente para reglas ACH/CENIT, sin introducir lógica transaccional hardcodeada.
- El comportamiento NACHA-M, ciclos, causales, enrutamiento y compensación, salvo que una fase IAM necesite una integración estrictamente transversal.
- El uso de EF Core Code First y migraciones versionadas.

No se debe utilizar este programa para cambiar reglas monetarias, ciclos, formato NACHA-M, causales, liquidación, archivos o conectividad productiva.

---

## 2. Hallazgos que originan el programa

La revisión R10 identificó como prioridades:

1. Hash de contraseñas con SHA-256 directo.
2. Usuario administrador inicial con credencial conocida en seed.
3. Política de contraseña aplicada de forma insuficiente en backend y administrable con autorización demasiado amplia.
4. Multirol y permisos amplios que rompen segregación de funciones.
5. Ausencia de MFA/OTP en autenticación local.
6. Recuperación de contraseña y revocación de sesiones incompletas.
7. Auditoría IAM insuficiente para altas, bajas, cambios de rol y cambios de políticas.
8. Confianza insegura en headers de proxy/IP si no se configura una cadena confiable.
9. CSP mejorable y exposición del token SPA a JavaScript.
10. Branding dinámico viable, pero con validación del lado servidor insuficiente para SVG/imágenes/colores.

### 2.1 Restricción normativa abierta

El manual ACH V35 suministrado referencia el documento **GIR-GRINS-010 — Instructivo de cuentas de usuarios y contraseñas**, pero ese instructivo no está disponible en el paquete documental revisado. Por tanto:

- se implementará una línea base de seguridad moderna y comprobable;
- no se afirmará cumplimiento exacto de GIR-GRINS-010 hasta tener el documento vigente;
- cualquier requisito posterior que contradiga esta propuesta debe abrir una decisión normativa y ajustar la implementación mediante migración controlada.

---

## 3. Principios no negociables

### 3.1 Seguridad

1. Nunca almacenar contraseñas en texto claro ni con hashes rápidos sin work factor.
2. Nunca incorporar credenciales reales o predeterminadas conocidas al código, migraciones, seeds, documentación o pruebas.
3. No permitir que la UI sea la única barrera de autorización.
4. Todo permiso crítico debe ser validado en backend.
5. Todo usuario humano debe operar bajo mínimo privilegio.
6. El Administrador de Usuarios debe estar segregado de los roles operativos.
7. Una operación sensible debe dejar evidencia auditable.
8. MFA debe ser obligatorio para usuarios privilegiados y recomendado/obligatorio para todos los humanos según el modo de autenticación adoptado.
9. Los tokens de activación/reset se almacenan únicamente en forma no reversible o hashada.
10. Cambiar contraseña o detectar compromiso debe permitir revocar sesiones activas.

### 3.2 Arquitectura

1. Domain contiene invariantes, no detalles de Angular ni EF.
2. Application contiene casos de uso y contratos.
3. Persistence implementa almacenamiento, hashes, repositorios, Data Protection y migraciones.
4. API aplica autenticación, autorización, rate limits y composición.
5. Angular consume contratos; nunca duplica la autoridad de seguridad.
6. La configuración de seguridad debe tener defaults seguros.
7. Producción debe fallar de forma segura si falta configuración crítica.

### 3.3 UX/UI

1. Mantener Angular Material.
2. Mantener branding dinámico.
3. No pedir al administrador que conozca o elija la contraseña permanente de otro usuario.
4. Explicar claramente rol, permisos, estado y riesgos antes de guardar.
5. Mostrar feedback accesible, no depender solo de color.
6. Acciones destructivas o sensibles requieren confirmación contextual.
7. La administración de usuarios debe ser usable por teclado, lectores de pantalla y con contraste WCAG AA.

### 3.4 Evidencia

No se puede declarar una fase como `DONE` si falta cualquiera de estos elementos aplicables:

- código implementado;
- migración cuando exista cambio de esquema;
- pruebas focales;
- build del proyecto afectado;
- regresión relevante;
- evidencia de runtime cuando la fase lo requiera;
- actualización de `MEMORY.md`.

---

## 4. Alcance y fuera de alcance

### Incluido

- creación, activación, desactivación y reactivación de usuarios;
- roles, permisos y segregación de funciones;
- bootstrap del primer administrador;
- hashing de contraseñas y migración progresiva;
- política de contraseña;
- invitación/activación y reset seguro;
- MFA y/o integración OIDC;
- manejo de sesiones y revocación;
- auditoría de seguridad;
- Angular Material para administración IAM;
- hardening de branding dinámico;
- CSP, forwarded headers, rate limiting y cookies/tokens;
- pruebas de seguridad y release gate.

### Excluido salvo dependencia directa

- reglas NACHA-M;
- causales Rxx/DEV;
- ciclos ACH/CENIT;
- conciliación/compensación/liquidación;
- integración con redes o infraestructura productiva;
- cambios al motor transaccional;
- rediseño visual general de la aplicación fuera de IAM/seguridad/branding.

---

## 5. Estado de memoria para ejecución en modo chat

El proyecto ya utiliza `MEMORY.md` como memoria durable. Este programa **no debe crear una memoria paralela** ni reemplazar el contenido existente. Debe agregar/actualizar entradas compactas con IDs estables.

### 5.1 IDs reservados para este programa

- `DEC-IAM-*` decisiones de arquitectura IAM.
- `INV-IAM-*` invariantes.
- `GAP-IAM-*` brechas abiertas.
- `CLOSE-IAM-*` cierres con evidencia.
- `EVID-IAM-*` evidencia relevante.
- `TRAP-IAM-*` trampas conocidas.
- `NEXT-IAM-*` siguiente trabajo concreto.
- `NORM-IAM-*` puntos normativos pendientes.

### 5.2 State block canónico

ChatGPT debe mantener conceptualmente este estado y reflejarlo de forma compacta en `MEMORY.md`:

```yaml
program: ACHInterbank-IAM-Security-UX
program_status: NOT_STARTED | IN_PROGRESS | BLOCKED | COMPLETE
baseline_commit: <git-sha-or-UNKNOWN>
target_branch: <branch>
active_phase: P0 | P1 | P2 | P3 | P4 | P5 | P6 | P7 | P8 | P9 | P10 | P11
active_phase_status: NOT_STARTED | IN_PROGRESS | BLOCKED | DONE

invariants:
  - id: INV-IAM-001
    text: Human users cannot combine UserAdmin with operational roles.
  - id: INV-IAM-002
    text: Password verification is authoritative on the backend.
  - id: INV-IAM-003
    text: No default known credentials exist in source, seeds or migrations.
  - id: INV-IAM-004
    text: Dynamic branding and Angular Material are preserved.
  - id: INV-IAM-005
    text: No ACH/CENIT transaction behavior changes as part of this program.

open_gaps: []
decisions: []
closures: []
evidence: []
files_touched: []
migrations: []
tests_passed: []
blocked_by: []
next_action: <single concrete action>
last_verified_commit: <sha-or-null>
```

### 5.3 Reglas para actualizar memoria

- No guardar logs completos.
- No guardar secretos, tokens, OTP, contraseñas, certificados ni cadenas de conexión.
- Una decisión debe registrar **qué se decidió y por qué**, no una narrativa extensa.
- Un cierre necesita evidencia reproducible: prueba, build, commit o runtime.
- Si cambia el commit, no trasladar automáticamente una certificación de release anterior.
- Si una sesión se queda sin contexto, reconstruir estado desde `AGENTS.md`, `MEMORY.md`, este documento, `git status`, `git log -1` y las pruebas existentes.

### 5.4 Ejemplo de actualización de memoria

```text
DEC-IAM-001: Human users use exactly one functional role. UserAdmin is mutually exclusive with all operational roles. Service accounts, if any, are governed separately.
INV-IAM-001: Backend rejects any role assignment violating SoD even if the API is called directly.
GAP-IAM-001: GIR-GRINS-010 is not present; exact ACH password-policy conformity remains unverified.
NEXT-IAM-001: Replace SHA-256 password storage with versioned PasswordHasher/PBKDF2 and progressive rehash.
```

---

## 6. Protocolo maestro de ejecución para ChatGPT

Cada sesión de implementación debe comenzar con esta secuencia.

### 6.1 Bootstrap de sesión

1. Leer `AGENTS.md`.
2. Leer `MEMORY.md` de forma acotada, priorizando `IAM` y el estado actual.
3. Leer este documento.
4. Leer `ACHInterbank_R10_Security_UX_Audit.md`.
5. Ejecutar:

```text
git status --short
git log -1 --oneline
git diff --stat
```

6. Verificar que no se pisan cambios no relacionados.
7. Confirmar la fase activa desde memoria.
8. Inspeccionar código real antes de modificarlo.
9. Ejecutar la prueba/base mínima de la fase antes del cambio cuando sea posible.
10. Aplicar cambios mínimos y coherentes.

### 6.2 Regla de continuidad

ChatGPT puede continuar automáticamente a la siguiente fase **solo si**:

- la fase actual está completamente verificada;
- no existe decisión destructiva pendiente;
- no aparece una contradicción normativa;
- no hay cambios locales del usuario que puedan ser sobrescritos;
- no se requiere secreto/infraestructura externa.

Si alguna condición falla, marcar `BLOCKED`, documentar el bloqueo y detenerse sin inventar resultados.

### 6.3 Reporte obligatorio al terminar cada fase

```markdown
## Phase Completion Report — Px

- Status: DONE | BLOCKED
- Commit/base: <sha>
- Files changed:
  - ...
- Migration(s):
  - ...
- Tests executed:
  - `<command>` → PASS/FAIL
- Security assertions verified:
  - ...
- Remaining gaps:
  - ...
- MEMORY.md updated:
  - IDs: ...
- Next phase:
  - ...
```

---

## 7. Arquitectura objetivo IAM

### 7.1 Entidades de dominio sugeridas

#### User

Campos mínimos conceptuales:

```text
Id
ParticipantId / TenantId
UserName
NormalizedUserName
Email
NormalizedEmail
DisplayName
Status              // PendingActivation, Active, Suspended, Disabled, Locked
RoleId              // para humanos, una sola asignación funcional
PasswordHash
PasswordHashVersion // permite migración progresiva
PasswordChangedAtUtc
SecurityStamp
FailedAccessCount
LockoutEndUtc
MfaStatus            // Disabled, PendingEnrollment, Enabled, RecoveryRequired
LastLoginAtUtc
CreatedAtUtc
CreatedBy
UpdatedAtUtc
UpdatedBy
RowVersion
```

Si el diseño actual usa una tabla `UserRole`, puede mantenerse para compatibilidad de esquema, pero el backend debe imponer unicidad para usuarios humanos y la base de datos debe tener un constraint/índice que lo respalde si el modelo lo permite.

#### Role

```text
Id
Code
Name
Description
IsPrivileged
IsOperational
IsUserAdministrator
IsAssignable
IsSystem
```

#### Permission

```text
Id
Code
Description
RiskLevel
```

#### RolePermission

Mapeo explícito de rol a permiso. No se permite un permiso “comodín” que, por accidente, otorgue administración de usuarios a un operador.

#### SecuritySession

```text
Id
UserId
SessionId / Jti
RefreshTokenHash        // si se usa refresh token
IssuedAtUtc
ExpiresAtUtc
RevokedAtUtc
RevocationReason
ClientFingerprint?      // usar solo datos no invasivos; no PII innecesaria
IpAddressNormalized?    // sujeto a política de privacidad
```

#### OneTimeToken

Para activación, reset y recovery:

```text
Id
UserId
Purpose
TokenHash
IssuedAtUtc
ExpiresAtUtc
ConsumedAtUtc
SupersededAtUtc
CreatedFromIp?
```

El token en claro existe solo en tránsito hacia el usuario y nunca se persiste.

#### SecurityAuditEvent

```text
Id
OccurredAtUtc
ActorUserId
ActorType
TargetType
TargetId
Action
Outcome
CorrelationId
SourceIp
BeforeJsonRedacted
AfterJsonRedacted
Reason
MetadataJsonRedacted
```

No registrar password, hash, OTP, secreto MFA, token, recovery code ni datos financieros sensibles.

#### BrandingConfiguration

```text
ParticipantId
BrandName
LogoAssetId
PrimaryColor
SecondaryColor
SurfaceColor
BackgroundColor
LoginBackgroundAssetId
UpdatedAtUtc
UpdatedBy
RowVersion
```

Los assets se validan en backend antes de quedar disponibles.

---

## 8. Modelo objetivo de roles y segregación

### 8.1 Regla principal

**Usuario humano = un rol funcional activo.**

El Administrador de Usuarios se considera rol de seguridad y no puede combinarse con un rol operativo.

### 8.2 Roles funcionales

Los nombres exactos se deben reconciliar con el código real antes de migrar. Conceptualmente se soportan:

- Administrador de Usuarios.
- Administrador/Approver operativo.
- Operador.
- Tesorería.
- Consultas.
- Auditoría.

No inventar roles adicionales si no están soportados por requisitos del proyecto.

### 8.3 Matriz de incompatibilidad mínima

| Rol | Puede combinarse con UserAdmin | Puede administrar usuarios | Puede asignar roles | Puede modificar password policy | Puede operar transacciones |
|---|---:|---:|---:|---:|---:|
| UserAdmin | N/A | Sí | Sí | Según permiso explícito | No |
| Operational Admin/Approver | No | No | No | No | Sí |
| Operator | No | No | No | No | Sí |
| Treasury | No | No | No | No | Según contrato |
| Queries | No | No | No | No | No |
| Auditor | No | No | No | No | No; solo lectura |

### 8.4 Protección del último administrador

El sistema debe impedir:

- desactivar al último UserAdmin activo de una entidad;
- quitarle el rol al último UserAdmin activo;
- auto-desactivación si deja la entidad sin administrador;
- eliminar físicamente usuarios como mecanismo normal de baja.

La baja estándar es desactivación/revocación preservando evidencia.

### 8.5 Cuenta de servicio

Si existen service accounts:

- no usar flujos de usuario humano;
- no compartir credenciales;
- preferir certificados/managed identity/client credentials;
- excluirlas del “un rol humano” solo mediante tipo de cuenta explícito;
- registrar propietario, propósito, expiración y rotación.

---

## 9. Arquitectura objetivo de autenticación

### 9.1 Estrategia recomendada

Diseñar un contrato `IAuthenticationProvider` o equivalente para soportar:

```text
AuthenticationMode = Local | Oidc
```

#### Producción preferida

- OIDC corporativo.
- MFA en IdP.
- Claims mínimos.
- Mapeo explícito a usuario/rol local.
- No autoaprovisionar privilegios por claims no validados.

#### Local

Reservado para desarrollo, pruebas controladas y/o contingencia expresamente aprobada.

Debe incluir:

- PasswordHasher con work factor.
- MFA TOTP para roles privilegiados.
- lockout.
- rate limiting.
- sesiones revocables.

La configuración de producción debe impedir que `Local` se habilite accidentalmente si la política exige OIDC.

### 9.2 MFA

Implementación local recomendada:

- TOTP RFC 6238.
- Secreto generado con CSPRNG.
- Secreto cifrado en reposo mediante Data Protection/KMS apropiado.
- QR solo durante enrollment.
- Confirmar con un código antes de activar.
- Recovery codes de un solo uso almacenados como hash.
- Regenerar recovery codes invalida los anteriores.
- Deshabilitar MFA requiere autenticación reforzada y auditoría.

Para cuentas privilegiadas, MFA es obligatorio antes de habilitar el usuario para operación administrativa.

### 9.3 Step-up authentication

Solicitar reautenticación/MFA reciente para acciones como:

- cambiar rol privilegiado;
- regenerar recovery codes;
- deshabilitar MFA;
- cambiar password policy;
- cambiar configuración de autenticación;
- revocar sesiones de terceros;
- actualizar branding si el contenido puede impactar toda una entidad.

---

## 10. Contraseñas y ciclo de vida

### 10.1 Hash objetivo

Preferencia:

1. `PasswordHasher<TUser>`/PBKDF2 si se prioriza integración nativa .NET y mantenimiento simple.
2. Argon2id si el proyecto acepta una dependencia mantenida y parámetros documentados.

El hash debe incluir versión/algoritmo para permitir rehash futuro.

### 10.2 Migración progresiva desde SHA-256

No forzar reset masivo si puede evitarse de forma segura.

Flujo:

```text
login
  -> detectar versión hash
  -> si legacy SHA-256:
       verificar usando legacy verifier aislado
       si válido:
          generar hash moderno
          persistir hash moderno + version
          invalidar material legacy
          continuar login
  -> si moderno:
       verificar con hasher actual
       si RequiresRehash:
          rehash y persistir
```

El verifier legacy debe:

- quedar encapsulado;
- no usarse para nuevos usuarios;
- quedar marcado como temporal;
- tener métrica de usuarios legacy restantes;
- poder eliminarse en una fase posterior cuando el contador llegue a cero o se cumpla la política de migración.

### 10.3 Política de contraseña baseline

Hasta obtener GIR-GRINS-010, aplicar baseline moderna:

- longitud mínima configurable segura;
- objetivo recomendado: 15 si password es factor único; 12+ puede considerarse si MFA es obligatorio, sujeto a decisión formal;
- longitud máxima admitida >= 64;
- permitir espacios/passphrases;
- no limitar arbitrariamente caracteres especiales;
- no obligar composición tipo “1 mayúscula + 1 símbolo” como control principal;
- bloquear passwords comunes, comprometidos o derivados del username/email;
- validar en backend;
- exponer reglas al frontend solo para UX.

La blocklist no debe enviar la contraseña a servicios externos desde backend sin una decisión de privacidad/seguridad explícita. Preferir corpus local o integración de privacidad tipo k-anonymity si se aprueba.

### 10.4 Creación de usuario

El UserAdmin no define la contraseña permanente.

Flujo:

```text
UserAdmin crea usuario
  -> backend valida unicidad, entidad y rol
  -> estado PendingActivation
  -> genera activation token aleatorio
  -> persiste hash del token
  -> envía enlace/código de activación por canal configurado
  -> usuario establece su password
  -> usuario enrola MFA cuando aplique
  -> estado Active
  -> revocar token de activación
  -> auditar todos los pasos
```

Si no hay proveedor de correo disponible en desarrollo, usar un `IUserNotificationSink` local que capture enlaces en un buzón de pruebas seguro, nunca imprimir secretos en logs persistentes.

### 10.5 Reset de contraseña

- respuesta anti-enumeración;
- token de 256 bits o equivalente CSPRNG;
- persistir solo hash;
- expira en ventana corta configurable;
- un token nuevo invalida anteriores del mismo propósito;
- consumo atómico;
- aplicar política de password backend;
- rotar `SecurityStamp`;
- revocar sesiones existentes;
- notificar al usuario;
- auditar solicitud y resultado sin exponer token.

---

## 11. Sesiones y tokens

### 11.1 Preferencia de arquitectura SPA

Para riesgo alto, preferir **BFF + cookie `HttpOnly Secure SameSite`** si el costo de migración es aceptable.

Si el sistema conserva JWT Bearer en SPA:

- access token corto;
- refresh token rotatorio y revocable;
- refresh token nunca en `localStorage`;
- usar cookie HttpOnly para refresh si arquitectura lo permite;
- CSP estricta;
- Trusted Types;
- revocación por `SecurityStamp`/session record para operaciones críticas.

### 11.2 Revocación

Revocar sesiones en:

- reset de contraseña;
- cambio de contraseña;
- desactivación de usuario;
- cambio de rol;
- sospecha de compromiso;
- deshabilitación/reset de MFA;
- solicitud explícita “cerrar todas las sesiones”.

### 11.3 Lockout

- contar intentos por usuario y por origen.
- no revelar existencia de cuenta.
- límites configurables.
- evitar DoS fácil por lockout permanente.
- desbloqueo administrativo auditado.

---

## 12. Auditoría y observabilidad

### 12.1 Eventos mínimos

Registrar como eventos de seguridad:

- usuario creado;
- activación completada;
- usuario activado/desactivado/reactivado;
- rol asignado/cambiado;
- intento de asignación incompatible rechazado;
- cambio de password policy;
- solicitud y consumo de reset;
- cambio de password;
- MFA enrolado/deshabilitado/recovery usado;
- login exitoso/fallido/bloqueado;
- sesión revocada;
- cambio de branding;
- modificación de configuración OIDC/autenticación;
- acceso denegado a endpoint administrativo sensible.

### 12.2 Integridad de auditoría

Preferir modelo append-only a nivel de aplicación y evitar endpoints de edición/borrado.

La retención final depende de política organizacional. No inventar un periodo regulatorio si no está documentado.

### 12.3 UI de auditoría

Angular Material:

- filtros por fecha, actor, objetivo, acción y resultado;
- detalle before/after redacted;
- exportación solo si hay permiso;
- paginación server-side;
- no mostrar secretos.

---

## 13. Branding dinámico seguro

### 13.1 Se conserva el modelo dinámico

El usuario debe poder mantener identidad visual por entidad sin recompilar Angular.

### 13.2 Tokens de diseño

Implementar branding con CSS custom properties alimentadas desde configuración validada:

```css
:root {
  --brand-primary: #...;
  --brand-secondary: #...;
  --brand-surface: #...;
  --brand-background: #...;
  --brand-on-primary: #...;
}
```

Angular Material debe consumir variables/tokens compatibles en una capa controlada, evitando inyectar CSS arbitrario provisto por usuario.

### 13.3 Validación backend

Colores:

- aceptar formatos explícitos (por ejemplo `#RRGGBB` / `#RRGGBBAA` si se decide);
- rechazar `url()`, `var()` arbitrario, `expression`, CSS libre y cadenas no reconocidas;
- calcular contraste para combinaciones principales.

Assets:

- allowlist de PNG/WebP; SVG solo si existe sanitización estricta o reprocesamiento confiable;
- validar magic bytes, no solo MIME/extensión;
- tamaño máximo;
- dimensiones máximas;
- descompresión segura;
- renombrar asset con identificador interno, no usar nombre del usuario como ruta;
- servir desde origen controlado con headers correctos.

### 13.4 UX del branding

Pantalla Material con:

- preview en tiempo real;
- contraste WCAG;
- advertencias antes de guardar;
- opción “restablecer branding por defecto”;
- historial/auditoría de cambios;
- fallback seguro si el asset deja de estar disponible.

---

## 14. UX/UI objetivo para usuarios y roles

### 14.1 Lista de usuarios

`mat-table` con paginación server-side.

Columnas sugeridas:

- nombre;
- usuario/email;
- entidad;
- rol;
- estado;
- MFA;
- último acceso;
- acciones.

Filtros:

- texto;
- rol;
- estado;
- MFA;
- rango de último acceso.

### 14.2 Crear usuario

Stepper Material:

1. Identidad.
2. Rol.
3. Seguridad.
4. Revisión.

No mostrar campo de “contraseña del usuario”.

En “Seguridad” mostrar:

- MFA requerido: sí/no según rol/política;
- método de activación;
- expiración de invitación.

### 14.3 Asignar rol

Usar `mat-radio-group` o `mat-select` simple, no `multiple` para humanos.

Mostrar:

- nombre de rol;
- descripción;
- permisos de alto nivel;
- indicador “Privilegiado”;
- incompatibilidades.

Si el cambio revoca sesiones, indicarlo antes de confirmar.

### 14.4 Desactivar usuario

Modal de confirmación:

- usuario objetivo;
- impacto: “se revocarán sesiones activas”;
- motivo obligatorio para roles privilegiados;
- protección especial del último UserAdmin.

### 14.5 Password policy

Solo usuarios con permiso explícito.

Pantalla:

- longitud mínima/máxima;
- MFA requirement;
- blocklist habilitada;
- expiración si la organización la exige (no activar por default sin requisito);
- lockout;
- reset-token lifetime;
- explicación de impacto.

No permitir valores inseguros definidos por debajo de hard minimums de plataforma.

### 14.6 Accesibilidad

- labels reales y `aria-*` donde aplique;
- focus visible;
- navegación teclado;
- errores asociados al campo;
- no usar color como único indicador;
- contraste AA;
- dialogs con focus trap correcto;
- live region para resultados de operaciones sensibles.

---

# 15. Plan por fases

Las fases están diseñadas para ser ejecutadas secuencialmente. Una fase puede dividirse internamente en commits pequeños.

---

## P0 — Baseline, inventario y freeze de contratos

**Objetivo:** obtener una fotografía reproducible antes de tocar seguridad.

### Acciones

1. Registrar commit base.
2. Ejecutar validación de solución.
3. Build Release.
4. Tests backend actuales.
5. Tests frontend actuales si existen.
6. Inventariar:
   - entidades User/Role/Permission;
   - servicios de autenticación;
   - policies de autorización;
   - seeds;
   - componentes Angular de usuarios/roles/password policy/branding;
   - migraciones vigentes.
7. Confirmar que los hallazgos del audit todavía aplican al código actual.
8. Crear/actualizar gaps `GAP-IAM-*` solo con evidencia actual.

### No hacer

- no modificar reglas de negocio;
- no crear migraciones todavía;
- no “arreglar de paso” problemas no relacionados.

### Acceptance

- baseline build/test registrado;
- lista exacta de endpoints y policies IAM;
- memoria actualizada;
- fase P1 desbloqueada.

### Prompt de ejecución

```text
Ejecuta P0 del documento ACHInterbank_Future_IAM_Security_UX_Implementation_Design.md. Lee AGENTS.md y MEMORY.md primero. Obtén baseline de git/build/tests, valida si los hallazgos R10 siguen vigentes y actualiza MEMORY.md con GAP-IAM y EVID-IAM. No cambies código funcional en esta fase.
```

---

## P1 — Contrato de identidad, rol único y segregación

**Objetivo:** fijar invariantes de dominio antes de modificar endpoints.

### Acciones backend

1. Definir `UserType` si hay humanos y service accounts.
2. Definir rol funcional único para humanos.
3. Introducir validador de asignación, por ejemplo:

```text
IRoleAssignmentPolicy
  ValidateAssignment(user, requestedRole, actor)
```

4. Reglas mínimas:
   - UserAdmin incompatible con rol operativo;
   - Auditor no recibe permisos mutadores;
   - Operator no recibe permisos IAM;
   - último UserAdmin protegido.
5. Eliminar dependencias conceptuales de permisos comodín.
6. Agregar tests unitarios de matriz de incompatibilidad.

### Persistencia

Elegir una de dos estrategias:

**A. `RoleId` directo en User** — simple, más invasiva.  
**B. conservar `UserRole`** — menor migración, agregar índice/constraint para una asignación humana.

La elección se registra como `DEC-IAM-*`.

### Acceptance

- backend rechaza multirol/incompatibilidades incluso sin UI;
- ningún rol operativo puede administrar usuarios por fallback;
- tests de rol pasan.

### Prompt

```text
Ejecuta P1. Define el contrato de rol único/SoD para usuarios humanos, preserva service accounts si existen, elimina cualquier vía conceptual que permita a ACH.Operator administrar IAM, agrega tests de dominio/aplicación y registra la decisión de esquema en MEMORY.md. No cambies todavía hashing ni MFA.
```

---

## P2 — Hash moderno y eliminación de credenciales seed

**Objetivo:** eliminar el riesgo más crítico de credenciales.

### Acciones

1. Introducir servicio de hashing versionado:

```text
IPasswordHashService
  Hash(password)
  Verify(user, password) -> Valid | Invalid | ValidNeedsRehash
```

2. Nuevos passwords usan algoritmo moderno.
3. Aislar verifier SHA-256 legacy solo para migración.
4. Rehash transparente después de login válido.
5. Eliminar seed `admin/Admin123!` y cualquier credencial conocida.
6. Diseñar bootstrap seguro:
   - comando/admin provisioning;
   - variable/secret de un solo uso fuera del repo;
   - o creación por IdP.
7. Agregar tests de no-regresión y rehash.
8. Escanear repo por secretos/credenciales demo peligrosas.

### Acceptance

- nuevo usuario nunca recibe SHA-256;
- login legacy migra automáticamente;
- no existe password default conocida en código/migraciones;
- pruebas de hash y bootstrap pasan.

### Prompt

```text
Ejecuta P2. Sustituye el almacenamiento SHA-256 por hashing versionado moderno con migración progresiva; elimina toda credencial administrativa conocida de seeds; implementa bootstrap seguro sin secretos en repo. Agrega tests que prueben hash moderno, verify, legacy rehash y ausencia del seed vulnerable.
```

---

## P3 — Policies de autorización y mínimo privilegio

**Objetivo:** hacer que API y permisos reflejen la matriz de rol.

### Acciones

1. Inventariar policies `UsersCreate`, `UsersUpdate`, `UsersDeactivate`, `UsersAssignRoles`, `UsersManagePasswordRules` y equivalentes.
2. Eliminar `CanManageAch` como fallback IAM si otorga más de lo debido.
3. Crear permisos explícitos, por ejemplo:

```text
Iam.Users.Read
Iam.Users.Create
Iam.Users.Update
Iam.Users.Disable
Iam.Users.AssignRole
Iam.PasswordPolicy.Manage
Iam.Audit.Read
Branding.Manage
```

4. Aplicar policies a controladores uno por uno.
5. Tests de autorización por endpoint:
   - UserAdmin: permitido según matriz;
   - Operator: 403;
   - Auditor: lectura únicamente;
   - anónimo: 401.
6. Evitar confiar en ocultamiento de menú Angular.

### Acceptance

- ningún endpoint IAM crítico usa `[Authorize]` genérico cuando necesita permiso específico;
- operador no puede escalar por llamada API directa;
- matriz de tests 401/403/2xx pasa.

### Prompt

```text
Ejecuta P3. Endurece todas las policies IAM y elimina permisos comodín. Haz que cada endpoint de usuarios, roles, password policy, auditoría y branding tenga autorización explícita. Agrega pruebas 401/403/allow por rol. No uses la UI como control de seguridad.
```

---

## P4 — Creación, activación y recuperación de usuario

**Objetivo:** reemplazar creación con password administrada por onboarding seguro.

### Acciones

1. `POST /users` crea `PendingActivation`.
2. No aceptar password permanente en DTO de creación.
3. Generar token CSPRNG; persistir hash.
4. Introducir `IUserNotificationService`.
5. Endpoint de activación consume token una sola vez.
6. Validar password policy backend.
7. Reset con token hashado, supersede tokens anteriores.
8. Rotar security stamp y revocar sesiones al completar reset.
9. Anti-enumeración.
10. Auditoría de eventos.

### UX Angular

- formulario sin password permanente;
- confirmation page con “invitation sent”;
- activar/restablecer en flujo separado;
- errores neutros para cuentas no existentes.

### Acceptance

- admin nunca conoce password final;
- token no existe en claro en DB/log;
- token consumido no se reutiliza;
- reset revoca sesiones.

### Prompt

```text
Ejecuta P4. Rediseña alta y reset de usuarios usando tokens de activación/reset de un solo uso, hashados en persistencia. Elimina password del formulario de alta. Aplica política en backend, anti-enumeración, revocación de sesiones y auditoría. Implementa UI Angular Material correspondiente.
```

---

## P5 — MFA y seguridad de sesión

**Objetivo:** implantar segundo factor y revocación consistente.

### Decisión obligatoria al inicio

Registrar una decisión:

- `Local + TOTP` como implementación inmediata; o
- `OIDC corporativo + MFA` si infraestructura disponible; o
- estrategia híbrida con provider abstraction.

### Acciones Local/TOTP

1. Modelo de credencial MFA.
2. Enrollment.
3. Confirmación.
4. Recovery codes hashados.
5. MFA challenge post-password.
6. Requerimiento obligatorio por rol.
7. Step-up para operaciones críticas.
8. Revocar sesiones en eventos de seguridad.

### Acciones OIDC

1. Validar issuer/audience/metadata.
2. PKCE donde corresponda.
3. Mapear identidad externa a usuario local.
4. No confiar roles administrativos provenientes del IdP sin allowlist/mapeo explícito.
5. Verificar claim de MFA/acr/amr según contrato del IdP.

### Acceptance

- usuario privilegiado no entra a funciones críticas sin MFA válido;
- recovery codes one-time;
- cambio de password/rol/MFA revoca sesiones;
- no se registran secretos MFA.

### Prompt

```text
Ejecuta P5. Primero decide y documenta el modo MFA/OIDC viable con la infraestructura real. Implementa MFA y sesiones revocables con step-up para acciones críticas. No simules integración externa si no hay IdP disponible; en ese caso completa Local+TOTP y deja OIDC como adapter documentado.
```

---

## P6 — Password policy segura y administrable

**Objetivo:** convertir password policy en control de servidor seguro.

### Acciones

1. Introducir hard minimums no debilitables desde UI.
2. Exponer configuración solo a permiso `Iam.PasswordPolicy.Manage`.
3. Validar cambios de rango.
4. Aplicar la misma policy en:
   - activación;
   - cambio de password;
   - reset;
   - cualquier bootstrap local.
5. Añadir blocklist.
6. No usar máximo de símbolos como regla.
7. Mostrar feedback útil en Angular sin revelar validaciones internas sensibles.

### Acceptance

- un usuario autenticado sin permiso no modifica policy;
- API rechaza `MinLength` inseguro;
- todos los caminos de establecimiento de password usan el mismo servicio.

### Prompt

```text
Ejecuta P6. Centraliza password policy en backend con hard minimums seguros y autorización específica. Reutilízala en activación/cambio/reset/bootstrap. Elimina reglas de máximo de símbolos y agrega blocklist local/segura. Actualiza UI Material para reflejar la policy sin convertirse en autoridad.
```

---

## P7 — Auditoría IAM inmutable y UI de trazabilidad

**Objetivo:** lograr trazabilidad de quién hizo qué y cuándo.

### Acciones

1. Implementar `SecurityAuditEvent` append-only.
2. Middleware/servicio de correlation ID.
3. Instrumentar casos de uso IAM.
4. Redactar secretos.
5. Crear endpoint paginado de consulta con permiso `Iam.Audit.Read`.
6. UI Material de auditoría.
7. Tests de evento emitido y contenido redacted.

### Acceptance

- crear/desactivar/rol/password policy/MFA/branding generan evento;
- before/after cuando aplica;
- no hay secretos en JSON de auditoría;
- auditor puede leer sin mutar.

### Prompt

```text
Ejecuta P7. Implementa auditoría IAM append-only con actor/target/action/outcome/correlation/before-after redacted. Instrumenta altas, bajas, roles, policy, MFA, sesiones y branding. Agrega endpoint y UI de consulta solo lectura para Auditor/UserAdmin según policy.
```

---

## P8 — UX/UI IAM en Angular Material

**Objetivo:** completar experiencia coherente, accesible y segura.

### Acciones

1. Lista de usuarios con filtros y estados.
2. Stepper de alta.
3. Selector de rol único.
4. Badges de privilegio/MFA/estado.
5. Confirmación de desactivar/rol.
6. Reactivación.
7. Protección visual del último admin, sin depender de ella para seguridad.
8. Mensajes de backend traducidos a UX sin ocultar error real en logs/auditoría.
9. Tests Angular de formularios y guards.
10. WCAG AA para pantallas IAM.

### Acceptance

- no existe `multiple` para rol humano;
- flujo completo teclado;
- errores asociados al campo;
- permisos no visibles si no corresponden, y backend sigue siendo autoridad.

### Prompt

```text
Ejecuta P8. Completa la UX IAM con Angular Material preservando diseño actual: tabla/filtros, alta por stepper, rol único, estados/MFA, confirmaciones, reactivación y accesibilidad WCAG AA. Mantén backend como autoridad. Agrega tests del frontend.
```

---

## P9 — Hardening de branding dinámico

**Objetivo:** conservar branding dinámico sin abrir superficie XSS/content injection.

### Acciones

1. Reemplazar CSS libre por design tokens permitidos.
2. Validar colores backend.
3. Validar archivos por bytes/tamaño/dimensiones.
4. Definir política de SVG:
   - preferido: deshabilitar upload SVG y usar PNG/WebP; o
   - sanitizador estricto + pruebas adversariales.
5. Servir assets con headers correctos.
6. Preview accesible y contraste.
7. Auditoría de cambios.
8. Tests con payloads maliciosos.

### Acceptance

- branding sigue siendo dinámico;
- Angular Material sigue activo;
- payload CSS/SVG no permitido es rechazado;
- contraste advertido/verificado.

### Prompt

```text
Ejecuta P9. Endurece branding sin eliminarlo: design tokens, validación backend de colores y assets, política segura para SVG, preview Material y contraste WCAG. Agrega pruebas adversariales de MIME spoofing, tamaños y payloads de CSS/SVG.
```

---

## P10 — CSP, proxy/IP y rate limiting

**Objetivo:** endurecer el perímetro HTTP y mitigar abuso de autenticación.

### Acciones

1. Configurar Forwarded Headers solo para proxies/redes confiables.
2. No confiar en `X-Forwarded-For` arbitrario.
3. Rate limiting:
   - login por IP + cuenta normalizada;
   - reset por IP + identidad lógica;
   - MFA challenge;
   - endpoints administrativos de alto costo.
4. CSP sin `unsafe-inline` cuando la app lo soporte.
5. Trusted Types.
6. Headers modernos; remover controles obsoletos si existen.
7. Revisar almacenamiento de tokens SPA.
8. Si es viable, migrar a BFF/cookie HttpOnly; si no, documentar riesgo residual y controles compensatorios.

### Acceptance

- IP efectiva proviene solo de proxy confiable;
- rate-limit tests pasan;
- CSP permite arrancar Angular sin `unsafe-inline` o existe waiver técnico temporal documentado;
- ningún token persistente sensible queda expuesto innecesariamente a JS.

### Prompt

```text
Ejecuta P10. Configura correctamente forwarded headers/proxies, rate limits de autenticación, CSP/Trusted Types y estrategia de tokens. Prioriza BFF/cookies HttpOnly si es viable; si no, documenta el riesgo residual y refuerza JWT corto + refresh seguro + CSP. Agrega pruebas de headers y límites.
```

---

## P11 — Migración, gauntlet y release evidence

**Objetivo:** demostrar que el cambio es integrable y no rompe funcionalidad fuera de IAM.

### Acciones

1. Ejecutar validación de solución.
2. Aplicar migraciones en base limpia.
3. Probar upgrade desde snapshot compatible si existe.
4. Build Release completo.
5. Tests backend completos.
6. Tests frontend completos.
7. Tests de autorización negativos.
8. Tests de password legacy migration.
9. Tests de MFA/recovery/reset.
10. Tests de last-admin protection.
11. Tests de branding adversarial.
12. Smoke test de rutas no IAM.
13. Security checklist manual.
14. Revisar git diff, secretos y cambios no relacionados.
15. Generar documento de evidencia.

### Gauntlet recomendado

Un crítico independiente debe intentar romper:

- multirol;
- escalación de Operator a UserAdmin;
- bypass de password policy;
- reutilización de activation/reset token;
- session reuse after reset/role change;
- bypass MFA;
- last-admin removal;
- SVG/CSS injection;
- spoof de X-Forwarded-For;
- rate-limit bypass obvio;
- IDOR entre participantes/tenants.

### Estados de cierre

- `DEVELOPMENT_COMPLETE`: código + tests focales.
- `VERIFIED`: build/regresión/migraciones.
- `GAUNTLET_PASSED`: validación adversarial independiente.
- `UAT_READY`: candidato empaquetado con evidencia.
- `USER_ACCEPTED`: solo después de aceptación explícita del usuario/negocio.

No saltar de `DEVELOPMENT_COMPLETE` a `USER_ACCEPTED`.

### Prompt

```text
Ejecuta P11 como validación final del programa IAM. No cambies expectativas de tests para ocultar fallas. Valida migración limpia y upgrade, build/test completos, pruebas negativas de autorización y gauntlet adversarial. Genera evidencia y actualiza MEMORY.md sin declarar USER_ACCEPTED sin aceptación explícita.
```

---

# 16. Mapa de cambios esperado sobre el código R10

Las rutas exactas deben verificarse en P0. Basado en la revisión R10, las zonas probables son:

### Backend / Application

- servicios de autenticación;
- helpers de hash;
- DTOs de creación/reset;
- interfaces de password policy;
- casos de uso de usuarios;
- policies/requirements/handlers de autorización;
- contratos de auditoría;
- notificación de activación/reset;
- MFA.

### Persistence

- `UsersService` o equivalente;
- configuración EF de `User`, `Role`, `UserRole`;
- seeds de usuarios/roles/permisos;
- tablas de tokens/sesiones/auditoría/MFA;
- migraciones SQL Server.

### API

- `AuthController`;
- `UsersController`;
- `PasswordRulesController`;
- `BrandingController`;
- middleware de security headers;
- forwarded headers;
- rate limiter;
- DI de policies.

### Angular

- login;
- activation/reset;
- users list/form;
- user roles;
- password policy;
- audit viewer;
- MFA enrollment/challenge;
- branding editor;
- theme service;
- auth interceptor/session service.

---

# 17. Contratos API propuestos

Los nombres son orientativos; ChatGPT debe respetar el estilo actual del repositorio.

```http
POST   /api/users
GET    /api/users
GET    /api/users/{id}
PUT    /api/users/{id}
POST   /api/users/{id}/deactivate
POST   /api/users/{id}/reactivate
PUT    /api/users/{id}/role
POST   /api/users/{id}/revoke-sessions

POST   /api/auth/activate
POST   /api/auth/login
POST   /api/auth/mfa/challenge
POST   /api/auth/mfa/enroll/start
POST   /api/auth/mfa/enroll/confirm
POST   /api/auth/mfa/recovery
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
POST   /api/auth/change-password
POST   /api/auth/logout
POST   /api/auth/logout-all

GET    /api/security/password-policy
PUT    /api/security/password-policy
GET    /api/security/audit

GET    /api/branding
PUT    /api/branding
POST   /api/branding/assets
```

### Reglas de respuesta

- no distinguir “usuario no existe” en forgot-password;
- usar `409` para conflicto de estado/last-admin/rol incompatible cuando sea semánticamente apropiado;
- usar `403` para autenticado sin permiso;
- usar `401` para no autenticado/credencial inválida;
- no devolver detalles internos de hashing/token/MFA.

---

# 18. Migraciones sugeridas

El orden real depende de la estrategia de P1.

Posibles migraciones:

1. `Iam_AddPasswordHashVersionAndSecurityStamp`
2. `Iam_RemoveDefaultAdminSeed` o ajuste de seed según patrón EF.
3. `Iam_EnforceHumanSingleRole`
4. `Iam_AddOneTimeTokens`
5. `Iam_AddSecuritySessions`
6. `Iam_AddMfaCredentialsAndRecoveryCodes`
7. `Iam_AddSecurityAuditEvents`
8. `Iam_HardenBrandingAssets`

### Reglas

- no editar manualmente una migración ya aplicada en entornos compartidos;
- crear nueva migración correctiva;
- rollback documentado para cambios destructivos;
- preservar IDs de roles existentes cuando sean contratos externos;
- no borrar datos IAM históricos sin plan explícito.

---

# 19. Estrategia de pruebas

## 19.1 Unitarias

- role assignment policy;
- password policy;
- password hash verifier/versioning;
- token hashing/expiry/consumption;
- MFA validators;
- last-admin rule;
- branding validators.

## 19.2 Integración API

Matriz mínima:

| Caso | Anonymous | Operator | UserAdmin | Auditor |
|---|---:|---:|---:|---:|
| Read users | 401 | según contrato | 200 | 200/según contrato |
| Create user | 401 | 403 | 2xx | 403 |
| Assign role | 401 | 403 | 2xx | 403 |
| Password policy update | 401 | 403 | 2xx si autorizado | 403 |
| Audit read | 401 | 403 | según policy | 200 |
| Branding update | 401 | 403 salvo rol dedicado | según policy | 403 |

## 19.3 Seguridad

- brute-force simulation controlada;
- token replay;
- session replay after revocation;
- tenant/participant boundary;
- mass assignment;
- role ID tampering;
- hidden endpoint direct call;
- header spoofing;
- oversized upload;
- polyglot/invalid image;
- malicious SVG if SVG remains enabled;
- CSP smoke.

## 19.4 Frontend

- formulario role single-select;
- disable invalid action;
- error API visible y accesible;
- focus management;
- activation/reset/MFA flows;
- dynamic theme preview;
- contrast warnings.

---

# 20. Estrategia de compatibilidad y despliegue

### 20.1 Feature flags sugeridos

```text
Security:EnableLegacyPasswordVerification = true/false
Security:RequireMfaForPrivilegedUsers = true
Security:AuthenticationMode = Local|Oidc
Security:AllowSvgBrandAssets = false
Security:EnableStrictCsp = true
```

Los defaults de producción deben ser seguros.

### 20.2 Rolling migration de passwords

1. Deploy con soporte verify legacy + hash moderno.
2. Nuevos passwords = moderno.
3. Logins válidos legacy = rehash.
4. Medir remaining legacy count.
5. Notificar/reset cuentas inactivas si política lo exige.
6. Deshabilitar legacy verifier.
7. Eliminar código legacy en release posterior.

### 20.3 Rollback

Nunca hacer rollback que obligue a convertir hashes modernos a legacy.

Si una versión debe revertirse:

- la versión anterior debe tolerar columnas nuevas o el deployment debe ser forward-compatible;
- preferir expand/contract migrations;
- no revertir a un sistema que solo entienda SHA-256 una vez que hay hashes modernos.

---

# 21. Threat model mínimo

Actores:

- atacante externo sin credenciales;
- usuario válido con rol operativo;
- usuario privilegiado comprometido;
- actor interno intentando escalar permisos;
- atacante XSS;
- proxy/header spoofing;
- abuso de reset/activación;
- archivo de branding malicioso.

Assets:

- identidad y sesión;
- permisos IAM;
- credenciales;
- secretos MFA;
- configuración de entidad;
- evidencia de auditoría;
- branding mostrado a usuarios.

Trust boundaries:

- navegador ↔ API;
- API ↔ DB;
- API ↔ IdP/email si existen;
- reverse proxy ↔ API;
- upload asset ↔ almacenamiento/servidor web.

Cada fase debe verificar que no amplía innecesariamente estos límites de confianza.

---

# 22. Reglas de manejo de secretos

ChatGPT jamás debe:

- imprimir un password real;
- crear `Admin123!` o equivalente en código permanente;
- usar un secreto real para pruebas;
- guardar un OTP/recovery code en `MEMORY.md`;
- dejar activation/reset token en logs;
- committear connection strings con password;
- deshabilitar validación TLS para “hacer que funcione”.

Fixtures deben usar secretos obviamente ficticios generados durante la prueba y descartados.

---

# 23. Criterios de aceptación global

El programa puede considerarse técnicamente completo cuando:

- [ ] no hay SHA-256 directo para passwords nuevos;
- [ ] no hay credencial admin conocida en seed;
- [ ] política de password se aplica en backend;
- [ ] Operator no administra IAM;
- [ ] UserAdmin no combina rol operativo;
- [ ] último UserAdmin está protegido;
- [ ] alta de usuario no requiere password elegida por admin;
- [ ] activation/reset tokens son one-time y hashados;
- [ ] password/role/MFA changes pueden revocar sesiones;
- [ ] MFA existe para privilegiados;
- [ ] auditoría IAM cubre cambios críticos;
- [ ] forwarded headers usan trusted proxies;
- [ ] rate limiting protege login/reset/MFA;
- [ ] CSP/Trusted Types están implementados o waiver explícito temporal;
- [ ] branding dinámico permanece y se valida server-side;
- [ ] Angular Material permanece como base UI;
- [ ] pruebas negativas de autorización pasan;
- [ ] migración desde R10 está probada;
- [ ] no cambió lógica ACH/CENIT no relacionada;
- [ ] `MEMORY.md` refleja decisiones, gaps y evidencia;
- [ ] falta de GIR-GRINS-010 sigue indicada si aún no está disponible.

---

# 24. Definition of Done por pull request/commit lógico

Para cada bloque:

- cambio pequeño y focal;
- tests nuevos para el defecto/control;
- tests existentes no debilitados;
- nombres de permisos/roles coherentes;
- sin secretos;
- sin EOL churn masivo;
- migration solo si es necesaria;
- documentación actualizada si cambia contrato;
- memoria actualizada solo para decisión durable/estado real;
- `git diff` revisado antes de declarar listo.

---

# 25. Recovery protocol si ChatGPT pierde contexto

En una conversación nueva, usar este prompt:

```text
Continúa el programa IAM/Seguridad/UX de ACHInterbank.

1. Lee AGENTS.md.
2. Lee MEMORY.md y busca IDs DEC-IAM, INV-IAM, GAP-IAM, CLOSE-IAM, EVID-IAM y NEXT-IAM.
3. Lee ACHInterbank_Future_IAM_Security_UX_Implementation_Design.md.
4. Lee ACHInterbank_R10_Security_UX_Audit.md.
5. Ejecuta git status --short, git log -1 --oneline y git diff --stat.
6. Identifica active_phase y no repitas fases cerradas sin evidencia contradictoria.
7. Inspecciona el código actual antes de modificar.
8. Continúa exactamente desde next_action.
9. No declares una fase DONE sin build/tests/evidencia aplicable.
10. No cambies lógica NACHA/ciclos/causales/compensación salvo dependencia IAM demostrada.
```

---

# 26. Prompt maestro para iniciar toda la implementación

Copiar/pegar en ChatGPT con acceso al repositorio:

```text
Quiero que ejecutes el programa definido en `ACHInterbank_Future_IAM_Security_UX_Implementation_Design.md` sobre el repositorio ACHInterbank.

Reglas de ejecución:
- Lee primero `AGENTS.md`, `MEMORY.md`, el diseño y `ACHInterbank_R10_Security_UX_Audit.md`.
- Respeta Clean Architecture, EF Code First, Angular Material y branding dinámico.
- No cambies reglas NACHA-M, ciclos, causales, compensación o conectividad productiva salvo dependencia IAM estrictamente demostrada.
- Ejecuta P0 y continúa secuencialmente.
- Puedes avanzar sin pedirme confirmación entre fases locales, reversibles y completamente verificadas.
- Detente si hay contradicción normativa, cambio destructivo no reversible, secreto/infraestructura externa faltante, baseline roto o cambios locales míos que puedan perderse.
- En cada fase: inspecciona, implementa, prueba, revisa diff, actualiza `MEMORY.md` con IDs IAM y entrega Phase Completion Report.
- Nunca reduzcas cobertura o cambies expectativas para hacer pasar un defecto.
- Nunca escribas credenciales reales ni secretos en archivos, logs o memoria.
- No declares VERIFIED/GAUNTLET_PASSED/UAT_READY/USER_ACCEPTED sin la evidencia correspondiente.

Empieza ahora por P0.
```

---

# 27. Checklist de revisión humana recomendada

Aunque ChatGPT ejecute las fases, antes de producción un responsable humano debería aprobar:

- modelo final de roles y SoD;
- choice Local/TOTP vs OIDC corporativo;
- política de recuperación y canales de notificación;
- retención de auditoría;
- política de password al obtener GIR-GRINS-010;
- política de service accounts;
- política de SVG/branding;
- estrategia BFF/JWT;
- trusted proxies de infraestructura;
- procedimientos break-glass;
- plan de rollout y rollback.

---

# 28. Resultado esperado

Al finalizar, ACHInterbank debe conservar su identidad técnica y visual —Angular Material, branding dinámico, arquitectura actual y reglas ACH/CENIT— pero con una capa IAM notablemente más fuerte:

```text
User lifecycle
  + role isolation
  + secure password hashing
  + activation/reset lifecycle
  + MFA
  + revocable sessions
  + explicit authorization
  + immutable audit
  + safe dynamic branding
  + hardened browser/API boundary
  + evidence-driven release process
```

Ese es el contrato de futuro que ChatGPT debe implementar y verificar por fases.
