# Auditoría integral de producción — ACHInterbank

**Fecha de auditoría:** 2026-04-26 (UTC)  
**Alcance:** inventario, diagnóstico y readiness productivo (sin cambios funcionales).

---

## 1) Evidencia recolectada en repositorio

> Actualización P0 (2026-04-26): análisis raíz detallado de los 18 fallos backend en `docs/audits/p0-cierre-analisis-raiz-18-fallos-backend-2026-04-26.md`.

Se ejecutó el inventario solicitado y se guardó evidencia cruda:

- `git status --short` → `docs/audits/evidence/git-status-short.txt`
- `git log --oneline -30` → `docs/audits/evidence/git-log-oneline-30.txt`
- `find . -maxdepth 3 -type f | sort` → `docs/audits/evidence/find-root-maxdepth3.txt`
- `find docs -maxdepth 5 -type f | sort` → `docs/audits/evidence/find-docs-maxdepth5.txt`
- `find src -maxdepth 5 -type f | sort` → `docs/audits/evidence/find-src-maxdepth5.txt`
- `find tests -maxdepth 5 -type f | sort` → `docs/audits/evidence/find-tests-maxdepth5.txt`
- `find web -maxdepth 6 -type f | sort` → `docs/audits/evidence/find-web-maxdepth6.txt`
- `find .github -maxdepth 5 -type f | sort` → `docs/audits/evidence/find-github-maxdepth5.txt`
- `find scripts -maxdepth 5 -type f | sort` → `docs/audits/evidence/find-scripts-maxdepth5.txt`

Evidencia de build/test ejecutada en esta auditoría:

- Build release: `docs/audits/evidence/dotnet-build-release.txt`
- Test release: `docs/audits/evidence/dotnet-test-release.txt`

---

## 2) Inventario ejecutivo (qué existe realmente)

### 2.1 Documentación funcional y técnica

**Implementado + documentado + evidenciado:**
- Existe volumen alto de documentación técnica/operativa en `docs/` (arquitectura, runbooks, UAT, operaciones, ADRs y auditorías previas).  
- Hay documentación explícita para seguridad NACHA/digital envelope/OpenBao, command center inbound, resiliencia y capability registry.

**Riesgo de calidad documental:**
- El `README.md` principal está parcialmente en plantilla genérica de GitLab y no refleja un estado productivo integral con criterios de operación/soporte/SLO claros.

### 2.2 Backend C# / arquitectura

**Implementado + evidenciado:**
- Solución multi-capa con proyectos `Api`, `Application`, `Domain`, `Persistence`, `External`, y tests dedicados.
- API con 52 controladores y pipeline con middleware de excepciones, WAF custom, rate limiting, logging, JWT y headers de seguridad.
- Integración de autenticación/autorización JWT + políticas finas por permisos.
- Integración OpenBao/KeyVault para resolución de secreto de certificados (según modo de almacenamiento).
- EF Core DbContext extenso con múltiples agregados ACH/CENIT/NACHA-M, bitácora y entidades de seguridad.

**Implementado pero no cerrado / riesgo:**
- Sí se evidencian migraciones EF Core para PostgreSQL bajo `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres` (incluyendo `AchDbContextModelSnapshot`). Riesgo pendiente: no se observó en este inventario una estrategia equivalente explícita para otros motores declarados (p. ej. SQL Server), por lo que la paridad multi-motor queda **PARCIALMENTE EVIDENCIADA**.
- Build Release compila en esta corrida sin warnings ni errores; mantener vigilancia de deuda técnica en validaciones futuras.

### 2.3 Frontend Angular

**Implementado + evidenciado:**
- SPA Angular moderna (v21), Karma y ChromeHeadless sin sandbox con Puppeteer, AG Grid presente en dependencias.
- Artefactos Docker y configuración Angular/Karma en repo.

**No verificado en esta corrida:**
- `npm ci`, `npm run build` y `npm test` **no ejecutados** en esta auditoría puntual (se requiere tiempo/entorno adicional y ejecución dedicada).

### 2.4 Pruebas

**Evidenciado:**
- Suite xUnit amplia: 394 tests detectados/ejecutados en corrida local de auditoría.
- Resultado real en esta auditoría: **376 passed / 18 failed**.
- Fallos concentrados en constraints SQLite (FK/unique), diferencias de expectativas en reportes y test de gobernanza CENIT.

**Interpretación:**
- El proyecto está funcionalmente vivo, pero la suite no está verde completa en este entorno, por lo que readiness productivo no puede declararse cerrado.

### 2.5 CI/CD y despliegue

**Evidenciado:**
- Existe workflow de GitHub para integración PostgreSQL con restore/build/EF update/tests.
- Dockerfiles para API y SPA.

**Brechas:**
- Solo se observó un workflow en `.github/workflows` (cobertura de pipeline de calidad/seguridad/despliegue integral **NO EVIDENCIADO EN REPOSITORIO**).

### 2.6 Normativa/regulatorio

**Evidenciado:**
- Documentos normativos ACH/CENIT en `docs/normativa` (PDF y markdown).
- Matrices/auditorías internas relacionadas con naming, envelope y trazabilidad.

**No evidenciado:**
- Manual operativo regulatorio integral firmado/aprobado para producción bancaria **NO EVIDENCIADO EN REPOSITORIO**.
- Evidencia de cumplimiento formal externo/certificación regulatoria vigente (actas firmadas, dictámenes de auditor externo, certificación oficial de cámara) **NO EVIDENCIADO EN REPOSITORIO**.
- Especificación oficial NACHA-M completa y versionada como fuente maestra interna (más allá de documentos técnicos internos) **NO EVIDENCIADO EN REPOSITORIO**.

---

## 3) Matriz de estado por dimensión

## 3.1 Implementado vs probado vs documentado vs operable/productivo

| Dimensión | Implementado | Probado en esta auditoría | Documentado | Evidenciado en repo | Operable | Productivo |
|---|---|---|---|---|---|---|
| API backend (.NET 10) | Sí | Parcial (build OK, tests con fallos) | Sí | Sí | Parcial | No cerrado |
| Persistencia EF Core | Sí | Parcial | Sí | Sí | Parcial | No cerrado |
| Migraciones versionadas EF | Sí (PostgreSQL) / Parcial (multi-motor) | No | Parcial | Parcial | Riesgo medio-alto | No cerrado |
| Seguridad JWT + permisos | Sí | Parcial (tests no 100% verdes) | Sí | Sí | Parcial | No cerrado |
| OpenBao/Vault integration | Sí | No validado end-to-end en esta corrida | Sí | Sí | Dependiente de entorno | No cerrado |
| Frontend Angular | Sí | No ejecutado en esta corrida | Sí (parcial) | Sí | Parcial | No cerrado |
| Observabilidad operacional | Sí (runbooks/docs + código) | No validado operativo en vivo | Sí | Sí | Parcial | No cerrado |
| UAT read-only | Sí (docs/evidencias históricas) | No reejecutado aquí | Sí | Sí | Parcial | No cerrado |
| CI/CD | Parcial | N/A | Parcial | Sí | Parcial | No cerrado |
| Normativa y cumplimiento formal | Parcial | N/A | Parcial | Parcial | Riesgo | No |

---

## 4) Riesgos principales

### P0 (bloqueantes de salida)
1. **Suite backend no verde completa (18 fallos en 394).**
2. **Paridad de migraciones/estrategia de evolución de esquema para motores alternos (además de PostgreSQL) no cerrada.**
3. **Cierre formal de cumplimiento regulatorio/operativo para producción bancaria no evidenciado con artefactos formales.**
4. **Criterios de go-live/rollback/responsables operativos consolidados en un único paquete de release no evidenciados de forma integral.**

### P1 (alto)
1. Riesgo de regresión de calidad al no contar con gates de calidad más amplios en CI/CD (además de build/tests base).  
2. Dependencias de entorno críticas (OpenBao token, DB provider, secretos JWT) con riesgo de deriva entre ambientes.  
3. Pipeline CI/CD visible limitado para garantías de calidad continua (security scan, SCA, SAST/DAST, release gates).

### P2 (medio)
1. README raíz desalineado (documentación de entrada no ejecutiva).  
2. Falta de consolidación de catálogo de evidencias “implementado vs probado vs operativo” en un tablero único.

### P3 (mejora)
1. Estandarizar scorecard de readiness por release (checklist automatizada).  
2. Endurecer métricas de calidad y objetivos SLO/SLA explícitos por módulo.

---

## 5) Qué está implementado pero no cerrado

- Pipeline técnico backend y módulos ACH/CENIT/NACHA-M con amplia superficie funcional.
- Integración de seguridad y gestión de certificados con modos múltiples.
- Command center inbound, capability registry read-only, wrappers multi-riel y shadow compare (evidenciados en historial de commits y documentación operativa).

**No cerrado para producción** por pruebas no completamente verdes, dependencias de entorno y falta de cierre formal normativo-operativo integral.

---

## 6) Qué está documentado pero no implementado (o no evidenciado técnicamente)

- Cualquier aseveración de cumplimiento regulatorio final o certificación externa no respaldada por artefacto técnico/verificable en repo.
- Migraciones EF versionadas: evidenciadas para PostgreSQL; mantener control de paridad y estrategia formal para motores alternos declarados por arquitectura.

---

## 7) Qué está implementado pero no documentado (o insuficientemente documentado)

- Estado real de calidad de pruebas en el entorno actual (fallos concretos de suite) no estaba reflejado como “estado actual” consolidado previo a esta auditoría.
- Riesgos de deuda técnica y regresiones de calidad no aparecen consolidados en una política de release visible.

---

## 8) Dependencias de entorno (alto impacto)

1. SDK .NET 10.0.203 requerido.
2. PostgreSQL para pruebas de integración y/o escenarios reales.
3. Secretos y variables de JWT.
4. OpenBao/Vault habilitado y token válido para casos de referencia externa.
5. ChromeHeadless/Puppeteer para pruebas SPA.

---

## 9) Criterio de readiness productivo (aplicado al estado actual)

Criterio mínimo exigido (10/10): implementada, configurada, probada, documentada, auditable, segura, observable, desplegable, rollback, responsables operativos.

**Veredicto actual:** **NO LISTO PARA PRODUCCIÓN (estado integral)**.

Razón: aunque existe implementación y documentación extensa, faltan cierres obligatorios en pruebas verdes integrales, evidencia formal normativa/operativa y paquete de release-governance consolidado.

---

## 10) Plan por fases para cierre a producción

### Fase 0 (P0) — Estabilización de salida
- Dejar suite backend en verde (0 fallos) en baseline oficial (SQLite y PostgreSQL según corresponda).
- Formalizar estrategia multi-motor de migraciones EF (PostgreSQL/SQL Server), con validación reproducible por entorno y gobernanza explícita.
- Emitir paquete único de go/no-go con responsables, rollback probado y sign-off técnico/operativo/compliance.

### Fase 1 (P1) — Hardening de calidad y seguridad
- Endurecer reglas de calidad estática (nullability/analyzers) con fail-fast en CI para prevenir regresiones.
- Endurecer CI/CD con quality gates (tests, cobertura mínima, SAST/SCA/secrets scan).
- Ejecutar pruebas de resiliencia y fallback en entorno cercano a producción con evidencias versionadas.

### Fase 2 (P2) — Operación y auditoría continua
- Consolidar runbooks de incidentes, guardias, RTO/RPO, matrices de escalamiento.
- Publicar dashboard de readiness por release (estado funcional, técnico, normativo, operativo).

### Fase 3 (P3) — Optimización
- KPI de calidad sostenida (lead time, MTTR, fail rate).
- Automatizar evidencias regulatorias periódicas y evidencia de trazabilidad punta a punta.

---

## 11) Impedimentos de salida a producción (gate blockers)

1. Suite de pruebas backend no estable al 100%.
2. Brecha de evidencia formal normativa/compliance para certificación de salida bancaria.
3. Brecha de consolidación de governance operacional de release (rollback + ownership + runbook de crisis en un paquete formal único).
4. Cierre incompleto de estrategia multi-motor (PostgreSQL/SQL Server) respecto a migraciones, validación y operación de cambios de esquema.

---

## 12) Conclusión ejecutiva

ACHInterbank muestra un avance técnico considerable y arquitectura madura en múltiples frentes, pero **la evidencia actual no permite declarar readiness productivo integral** bajo estándar bancario estricto. El proyecto se encuentra en **pre-producción avanzada con brechas de cierre P0/P1**. La recomendación es ejecutar un ciclo corto de estabilización y certificación de salida antes de cualquier go-live.
