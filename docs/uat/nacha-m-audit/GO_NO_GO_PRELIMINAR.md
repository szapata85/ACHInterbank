# GO / NO-GO preliminar NACHA-M

Fecha: 2026-07-16. Alcance: generación de archivos NACHA-M. Esta decisión es técnica preliminar, no aprobación productiva, contractual ni regulatoria.

## Decisión ACH Colombia

**NO-GO**

### Reglas críticas cumplidas

- Longitud física de 106 en la muestra.
- Sin terminadores de línea ni tabs.
- Total de registros múltiplo de 10 y padding al final.
- Conteos, bloque count y EntryHash recalculables.
- Cálculos internos de débitos/créditos consistentes cuando se leen con el perfil actual.
- Perfiles técnicamente separados y ruta official fail-fast disponible.

Estas reglas no compensan las incumplidas.

### Reglas críticas incumplidas

- T1: fecha 6 vs 8 y desplazamiento de hora, FileId, size, block, format, nombres, reference y reservado.
- T5: fechas 6 vs 8 y desplazamiento de status, entidad y lote.
- Lote: política diaria global en vez de iniciar 1 dentro de cada archivo.
- T6: monto 10 vs 18; identificación, nombre, indicador y Trace desplazados.
- T6/T7: registros agrupados, indicador inválido y asociación/sufijo no verificables.
- T8/T9: totales de 12 vs 18, lote/reservados en offsets incorrectos.
- Truncamiento silencioso disponible en ramas legacy/HYBRID.
- Parser no consume el archivo mediante motor por bytes/perfil.
- Trazas/logs no minimizan datos sensibles por campo.
- Archivos reales rastreados por Git.

### Reglas no demostradas

- Vigencia contractual exacta del MAN-004 V32 para CFA.
- Uso permitido de `CycleName` como `ReferenceCode`.
- Alcance/dirección del patrón `.OUT` del archivo comparativo.
- Encoding monobyte oficial de salida.
- Homologación externa y vector de certificación.
- Aprobación humana de Compliance, Seguridad y Operaciones.

### Riesgos residuales

Incluso después de corregir, permanecerán riesgos de cambio normativo, operación, datos, concurrencia, configuración y transporte. Deben gestionarse con control de versiones, homologación, monitoreo, reconciliación y rollback. No se garantiza ausencia de sanciones.

### Condiciones mínimas para reconsiderar GO

1. Resolver vigencia/alcance documental.
2. Corregir todos los RuleIds críticos de la matriz ACHCOL.
3. Implementar lote por archivo y fecha Bogotá.
4. Corregir T6/T7 y controles cruzados.
5. Remediar privacidad Git/trace/logs.
6. Pasar suite normativa con golden sintético aprobado.
7. Probar SQL Server/PostgreSQL, concurrencia, idempotencia y rollback.
8. Homologación de cámara/interfaz.
9. Firma humana de cuatro ojos.

## Decisión CENIT

**NO-GO**

### Reglas críticas cumplidas

- Existe un perfil separado en el modelo.
- El manual operativo confirma uso del formato NACHAM y operación por ciclos.
- El archivo comparativo es físicamente consistente y sus controles se recalculan bajo una forma tipo ACH.

La última evidencia es comparativa y no demuestra cumplimiento CENIT.

### Reglas críticas incumplidas

- Perfil marcado `PUBLICADO` aun cuando su propia descripción declara fuente placeholder pendiente de homologación.
- Reutilización de las posiciones erróneas del perfil ACHCOL.
- Brechas comunes de T6/T7, trace, lotes, privacidad, parser, encoding y zona horaria.
- Naming implementado sin una fuente técnica local que lo soporte.

### Reglas no demostradas — bloqueantes para LIVE

- Longitud de registro, block factor, padding, encoding y terminadores CENIT.
- Todos los campos y offsets T1/5/6/7/8/9.
- Fechas, ciclo y representación de `ReferenceCode`.
- Inicio/reinicio/unicidad del lote y match T5/T8.
- Códigos de transacción, SEC, indicador de adenda y Trace Number.
- Conteos, hash y totales.
- Nombre externo y relación con STA.
- Vigencia/versión de anexos A/B y aplicabilidad por flujo.
- Aprobación/homologación para CFA.

### Documentos faltantes

- Manual/ficha técnica NACHA-M CENIT vigente aplicable a CFA.
- Manual de Especificaciones del Formato para STA citado por el Anexo 2.
- Contrato de naming y transporte con CFA.
- Calendario/ventanas vigentes con evidencia de aprobación operativa.

### Condiciones mínimas para reconsiderar GO

1. Obtener y catalogar fuentes oficiales con versión, vigencia, hash y numerales.
2. Crear matriz CENIT campo a campo sin copiar ACHCOL.
3. Despublicar/bloquear el placeholder y crear un perfil nuevo versionado.
4. Implementar y probar todas las reglas críticas.
5. Remediar privacidad y Git.
6. Golden sintético CENIT aprobado y vector externo.
7. Pruebas SQL Server/PostgreSQL y UAT por ciclo.
8. Aprobación humana formal.

## Pruebas faltantes para ambas cámaras

- Byte-level oracle por RuleId.
- Posiciones exactas de todos los campos.
- T6/T7 intercalado y relación de sufijos.
- Límites de montos y contadores.
- Caracteres permitidos/encoding.
- Cambio de día America/Bogota.
- Varios archivos el mismo día.
- Reintentos, rollback y concurrencia real de ambos providers.
- No reconstrucción de datos desde trace/log/API.
- Publicación con fuente normativa y doble aprobación.

## Estado global

El sistema permanece **NO-GO** para generación LIVE de ambas cámaras. Esta decisión no cambia por compilación, número de tests existentes ni semejanza con archivos de terceros.

