# Scalar-SEC-PERM-2A — Registro de policies finas, pruebas y evidencia (2026-05-01)

## 1) Resumen ejecutivo
Se completó el cierre técnico pendiente de PERM-2A registrando policies de autorización para **todos** los permisos definidos en `FineGrainedPermissions.AllPermissions`, sin migrar controladores ni alterar contratos/rutas. Se agregaron pruebas automáticas para validar catálogo y registro efectivo de policies.

## 2) Contexto PERM-2 parcial
El estado previo tenía constantes finas y `AllPermissions`, pero faltaba registrar policies en `AuthorizationOptions`, agregar pruebas dedicadas y consolidar evidencia de ejecución.

## 3) Constantes existentes validadas
Se validó la existencia del catálogo y de `AllPermissions`, incluyendo permisos críticos como:
- `Transactions.Read`
- `Nacha.Upload`
- `NachaSecurity.ManualDecrypt`
- `Certificates.Activate`
- `CommandCenter.MarkFailedFinal`
- `Users.AssignRoles`
- `Maintenance.Seed`

## 4) Registro de policies finas implementado
Se implementó iteración sobre `FineGrainedPermissions.AllPermissions` dentro de `AddAuthorization`, registrando policy por permiso con el mismo nombre.

## 5) Claim type usado
Se usó el claim type existente del proyecto: `"permission"`.

## 6) Compatibilidad con CanReadAch y CanManageAch
Se mantuvieron intactas las policies legacy `CanReadAch` y `CanManageAch` y su registro explícito.

## 7) Pruebas creadas
Se agregó `tests/Cfa.ACHInterbank.Tests/Authorization/FineGrainedPermissionsRegistrationTests.cs` para validar:
- catálogo no vacío;
- ausencia de duplicados;
- presencia de permisos críticos;
- registro de policies legacy (`CanReadAch`, `CanManageAch`);
- registro de todas las policies finas de `AllPermissions`.

## 8) Resultado build inicial
Comando: `dotnet build ACHInterbank.sln -c Release`.
Resultado: exitoso.

## 9) Resultado build post-cambios
Comando: `dotnet build ACHInterbank.sln -c Release`.
Resultado: exitoso.

## 10) Resultado pruebas específicas
Comando:
`dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~FineGrainedPermissionsRegistrationTests|FullyQualifiedName~Authorization|FullyQualifiedName~Security" -v minimal`

Resultado: exitoso (`29/29` aprobadas para el filtro).

## 11) Resultado suite completa
Comando:
`dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release -v minimal`

Resultado: exitoso (`421/421` aprobadas).

## 12) Resultado build final
Comando: `dotnet build ACHInterbank.sln -c Release`.
Resultado: exitoso.

## 13) Validación de no migración de controllers
Comando: `git diff -- src/Cfa.ACHInterbank.Api/Controllers`
Resultado: sin cambios.

## 14) Riesgos restantes
- Aún no se ha hecho la migración controlada de endpoints a policies finas (se abordará en PERM-3).
- Se mantiene deuda funcional de gobernanza de asignación real de permisos por rol en ambientes.

## 15) Qué NO se implementó
No se implementó migración de controladores/endpoints a permisos finos en este alcance.

## Qué NO se implementó en Scalar-SEC-PERM-2A
- No se migraron controladores.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se eliminaron CanReadAch ni CanManageAch.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.

## 16) Veredicto
**PERM-2A: CERRADO** a nivel de registro de policies finas + pruebas + evidencia.
