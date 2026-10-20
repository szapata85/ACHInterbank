# Enrutamiento de GitHub Actions

Los workflows se seleccionan por evento y rutas modificadas. El mensaje de commit y el título de un pull request no se usan como criterios de selección. El título visible de una ejecución de `pull_request` puede ser el título del PR.

| Workflow | Responsabilidad | Ejecución automática |
| --- | --- | --- |
| `dotnet-ci` | Compilación y pruebas generales .NET | Código, pruebas, solución o configuración .NET |
| `angular-ci` | Compilación, pruebas y E2E propios del SPA | SPA y Compose PostgreSQL usado por su runtime |
| `scheduler-cluster-e2e` | Quartz, tareas, calendario operativo y `/scheduler/tasks` en PostgreSQL y SQL Server | Rutas específicas del scheduler, Quartz y su clúster |
| `clearing-houses-multidb` | Administración de cámaras y estrategias Payment Rail en PostgreSQL y SQL Server | Rutas específicas de cámaras, Payment Rail y sus migraciones |
| `financial-integrity-multidb` | Integridad de persistencia financiera en PostgreSQL y SQL Server | Entidades, configuración, migraciones y pruebas financieras |
| `reprocess-dispatcher-certification` | Reproceso de respuestas y su clúster Quartz | Rutas de respuestas, reproceso, migraciones y pruebas JOB 4 |
| `postgres-integration-tests` | Diagnóstico manual de integración PostgreSQL | Solo `workflow_dispatch` |

## Regla de ramas por alcance

Cada prompt funcional inicia en una rama nueva desde una base remota actualizada. No se reutiliza una rama con cambios no integrados de otro dominio.

```powershell
git switch <rama-base>
git pull --ff-only
git switch -c feature/<alcance-puntual>
git push -u origin feature/<alcance-puntual>
```

Antes de publicar, se revisan los commits y archivos que formarán el rango:

```powershell
git log --oneline origin/<rama>..HEAD
git diff --name-status origin/<rama>..HEAD
```

Un pull request que acumula dominios independientes debe aislarse en una rama nueva creada desde su base remota; no se reescribe, elimina ni fuerza la historia de la rama existente.
