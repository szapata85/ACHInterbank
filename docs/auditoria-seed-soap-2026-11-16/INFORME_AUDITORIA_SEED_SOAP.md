# Auditoría técnica del seed de ACH Interbank y contratos SOAP

**Corte del código inspeccionado:** copia entregada en `ACHInterbank.zip` (no es una conexión con tu repositorio o SQL Server en vivo).  
**Fuente de contratos:** WSDL/XSD en `WSCFAACH.zip` y `WSAxonRespuestaTransacciones.zip`.  
**Tipo de validación:** lectura de código, comparación estática y preparación de pruebas. **No se pudieron ejecutar .NET, SQL Server ni llamadas SOAP en este entorno.**

## Resumen ejecutivo

- Se revisaron **70 clases implementadoras de `IDbSeeder`** en el código original; el parche propuesto conserva **68** y retira dos seeders de navegación/permisos de `OperationalRestrictions`. No se alteraron migraciones.
- Hay una dependencia rota en `NavigationInformationArchitectureSeeder`: exige la ruta `/operational-restrictions` que el proyecto ya no debe crear, lo que ocasionó `500` en `/Maintenance/seed`. Se sustituye por **inactivación idempotente** de rutas heredadas; no se vuelve a crear el menú.
- Se inactiva la ruta de menú SOAP antigua `/soap-integrations`, incluso si hay duplicados; la ruta Angular válida encontrada en el ZIP es `/integraciones/conexion`. No se eliminan los endpoints `/api/users/soap-integrations` (servicio de configuración).
- El catálogo técnico presenta los campos esperados por el contrato: `Proc_Contrapartidas` **22**, `Proc_Transacciones` **27 (25 entradas + 2 salidas)**, `RegistrarRespuestaTransaccion` de WSAXON **7**. La paridad de nombres/tipos/orden contra los WSDL/XSD adjuntos pasó la comparación estática; **no certifica el origen/semántica bancaria de valores**.
- El seed de mappings podía reemplazar o archivar versiones **publicadas manualmente**. Se propone preservar las del operador y archivar únicamente las publicadas por el seeder si compiten. Se agregaron pruebas de regresión.
- El seed de settings SOAP reescribía `Endpoint`, `Enabled`, `OperatingMode` y timeout guardados en SQL Server. Se propone conservar esos cuatro valores al resembrar; solo sobreescribir expresamente con `SoapIntegrationBootstrap:OverwriteExistingSettings=true`. Los argumentos técnicos y SOAPAction se reconciliarán con el contrato.
- Los campos/fuentes añadidos manualmente en el catálogo no deben desactivarse por un seed posterior. Se ajusta su reconciliación para preservar esos campos y limitar modificaciones de parámetros al `baseline-v1` (sin alterar versiones importadas).
- Se instrumenta el inicializador con mensajes `Seed starting`, `Seed completed` y `Seed failed`, cada uno con tipo y orden, para identificar el seeder que provoca futuros errores.

## 1. Estado de cada SOAP

| Operación | Contrato técnico cotejado | Fuente de datos de mapping | Estado prudente |
|---|---|---|---|
| `WSCFAACH.Proc_Contrapartidas` | 22 parámetros | Hay mappings de referencia, pero **no homologación** de `OFEMP`, `OFIDARCH`, `OFIDLOT`, `OFST`, `OFDIRECCIONIP` según `DECISIONES_I02_CONTRAPARTIDAS.md` | Mantener **DryRun** y no habilitar Live hasta aprobación funcional |
| `WSCFAACH.Proc_Transacciones` | 25 parámetros de entrada y 2 de salida | `NCTAORIG` no tiene origen homologado; la documentación señala además `IDORIG` e `INFPAG` pendientes | Mantener **DryRun** y validar mapeo con el banco |
| `WSAXON.RegistrarRespuestaTransaccion` | 7 parámetros: `idCanal`, `nombreCanal`, `idTransaccion`, `idEstado`, `causal`, `idTransaccionAxon`, `descripcionCausal` | Seeder de respuesta diferencial tiene reglas para los siete; evitar archivar mappings ajustados y publicados por el operador | Comprobar trazabilidad/estados y probar en ambiente controlado |

**Advertencia:** una versión marcada `Published` no equivale a una homologación bancaria. Las rutas monetarias deben seguir **cerradas a Live** si faltan campos obligatorios, equivalencias, validación de entrada o autorización de operación.

## 2. Ajustes de código del parche

1. `DbInitializer.cs`: elimina llamada redundante a `IntegrationCatalogBootstrapper` (ya invocado por `IntegrationMappingBootstrapper`), registra pasos y excepciones por seeder y asegura restauración de `AuditEnabled`.
2. `NavigationInformationArchitectureSeeder.cs`: no exige `/operational-restrictions`; desactiva ese menú si existe; desactiva las filas del antiguo `/soap-integrations`, mantiene `/integraciones/conexion`.
3. Retira `OperationalRestrictionsMenuSeeder.cs` y `CfaOperationalTransactionRestrictionsPermissionSeeder.cs`, actualizando pruebas de sus dependencias; **no** toca el motor bancario `CfaOperationalTransactionRestrictionResolver`, `CfaOperationalTransactionRestrictionSeeder` ni migraciones históricas.
4. `IntegrationCatalogBootstrapper.cs`: preserva fuentes de catálogo añadidas por operadores y versiones de contrato no-baseline; conserva `Required` como regla funcional de obligatoriedad, mientras `MinOccurs` refleja la estructura XSD (`0`). Para entradas obligatorias se mantiene rechazo de nulos, aunque el XSD los permita estructuralmente.
5. `IntegrationMappingBootstrapper.cs`: conserva publicaciones de operador, evita multiplicar publicaciones del seed y elimina constantes sintéticas tipo `SEED` de parámetros obligatorios en la referencia de `Proc_Contrapartidas`.
6. `SoapIntegrationSettingsBootstrapper.cs`: mantiene las opciones operativas persistidas (si existen), actualiza argumentos y SOAPAction canónicos, y permite una reparación explícita activando `OverwriteExistingSettings`.
7. Se agregan o adaptan pruebas sobre navegación, idempotencia, propiedad de mappings, supervivencia de fuentes manuales y protección de configuraciones.

## 3. Lo que no debe eliminarse automáticamente

- Persisten referencias de dominio, controladores, UI y pruebas de la **funcionalidad transaccional** `OperationalRestrictions` en el ZIP. El parche la retira del *seed de menú/permisos*, pero **no equivale a eliminación total del módulo ni de sus datos históricos**. Borrarla completamente requiere análisis del impacto en validaciones de pagos y una migración reversible.
- No se modificó ninguna de las migraciones EF Core existentes. El error histórico `PendingModelChangesWarning` se debe verificar de manera independiente, antes de aplicar cambios de esquema.
- No se crean, modifican ni eliminan datos directamente en SQL Server desde este paquete. El script `Auditar_Seed_ACHInterbank.sql` es **solo lectura**.
- No se han certificado conexiones reales, respuestas del core ni homologación bancaria. Las homologaciones más recientes existentes **solo en tu equipo o BD y no en el ZIP** requieren comparación con el sistema actual.

## 4. Seguridad detectada

**ALTA:** El archivo `src/Cfa.ACHInterbank.Api/appsettings.Development.json` del ZIP contiene una **clave JWT de firma en texto claro**. No se incluye ese archivo en el parche para no redistribuirla. Si ese valor se utiliza en cualquier entorno accesible, **rotar la clave y moverla a variables de entorno/secret manager**, evitando incluirla en repositorios y logs. Inspeccionar también cadena de conexión, certificados y endpoints internos.

El ZIP original de desarrollo configura `ProcContrapartidas`, `ProcTransacciones` y sus tres endpoints SOAP en modo `Live`. En el archivo local de desarrollo, cambiar explícitamente (con copia de seguridad):

- `ProcContrapartidas.Mode = "DryRun"`
- `ProcTransacciones.Mode = "DryRun"`
- `SoapIntegrationBootstrap.ProcContrapartidas.OperatingMode = "DryRun"`
- `SoapIntegrationBootstrap.ProcTransacciones.OperatingMode = "DryRun"`
- `SoapIntegrationBootstrap.RegistrarRespuestaTransaccion.OperatingMode = "DryRun"`

**Nota:** El parche **preserva** settings ya guardados en SQL Server. Por lo tanto, cambiar el JSON **no convierte automáticamente** un setting persistido en `Live` a `DryRun`. Revisar y deshabilitar Live de manera explícita usando el mecanismo administrativo autorizado antes de repetir `/Maintenance/seed`.

## 5. Aplicación y validación recomendada

1. **Guardar respaldo** de la BD `ACHInterbank` y del repositorio. Detener llamadas automáticas externas durante la prueba; no borrar volúmenes.
2. En una rama Git, descomprimir el ZIP de correcciones. Desde PowerShell, **previsualizar** el parche (no cambia archivos):
   ```powershell
   .\apply-seed-patch.ps1 -ProjectRoot 'C:\Users\CHECHO\Documents\proyectos\Interbank\ACHInterbank_final'
   ```
3. Si todos los hashes de archivos coinciden con la copia original del ZIP, aplicar (crea respaldo de los archivos reemplazados):
   ```powershell
   .\apply-seed-patch.ps1 -ProjectRoot 'C:\Users\CHECHO\Documents\proyectos\Interbank\ACHInterbank_final' -Apply
   ```
   Si los hashes no coinciden, **no forzar**: el proyecto local tiene cambios posteriores; revisar `git diff` y hacer combinación manual.
4. Ajustar `DryRun` local sin copiar o compartir claves. En Visual Studio, ejecutar `scripts\Probar_Seed_ACHInterbank.ps1` para compilar, probar los seeds y comprobar discrepancia de modelo EF. Esta verificación **requiere dotnet 10**.
5. En SSMS, correr `scripts\Auditar_Seed_ACHInterbank.sql`, que revisa historial EF, rutas, versiones, mappings publicados, reglas requeridas sin fuente y configuración SOAP **sin imprimir secretos**.
6. Solo si la compilación, migraciones y seguridad operativa están correctas, reconstruir **sin seed automático**:
   ```bat
   docker-ach.cmd up --no-seed --no-auto-migrations
   docker-ach.cmd status
   ```
   Luego ejecutar una única vez `/Maintenance/seed` en entorno controlado y revisar el log `Seed failed` si surge una excepción.
7. Validar `/integraciones/conexion` e inactividad de `/soap-integrations`. No habilitar Live hasta confirmar todas las equivalencias.

## 6. Limitaciones de la entrega

La revisión es **estática sobre el ZIP adjunto**, no sobre tu copia de Visual Studio ni tu SQL Server. No contamos con SDK .NET, Docker ni tu base de datos en este entorno; por ello **no afirmar que los tests pasan o que el seed ya corrió sin errores**. Las pruebas y consulta SQL adjuntas permiten reproducir el diagnóstico en tu Windows. El instalador se detiene ante cualquier diferencia de hash para evitar sobrescribir mappings recién desarrollados en tu máquina.
