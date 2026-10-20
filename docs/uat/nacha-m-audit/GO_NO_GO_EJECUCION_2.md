# GO / NO-GO — Ejecución 2 NACHA-M

Fecha: 2026-07-16  
Decisión técnica preliminar; no sustituye certificación de cámara ni aprobación humana.

## ACH Colombia

**Decisión: NO-GO**

### Correcciones confirmadas offline

- Perfil ACHCOL separado con layout de 106 posiciones de T1, T5, T6, variantes T7, T8 y T9.
- Fecha/hora T1 de ocho/cuatro posiciones desde un snapshot coherente.
- `ReferenceCode` separado de ciclo; `CycleName` no se serializa en T1.
- Fechas T5 de ocho; lote local 1..n por archivo y coincidencia T5/T8.
- Monto T6 de 18; indicador y Trace en offsets oficiales.
- T6/T7 intercalados y sufijo T7 validado contra T6 asociado.
- Totales T8/T9 de 18; conteos, hash y bloques reconciliados.
- Reglas `CfgFieldRule` ejecutadas en el renderer oficial y overflow fail-closed.
- Parser por bytes con longitud exacta, sin BOM/EOL/residuos y round-trip sintético.
- Redacción de errores/logs/trazas en las superficies modificadas.
- Golden y negativas completamente sintéticos.

### Reglas no demostradas — bloqueantes para LIVE

1. Naming contractual exacto e interfaz aplicable a CFA, incluida la relación entre ZZZ y FileId.
2. Encoding contractual exacto y repertorio aprobado extremo a extremo.
3. Vigencia aplicable a CFA y resolución del conflicto: archivo/portada V32 frente a encabezados internos V31 en fichas técnicas, especialmente T7.
4. Semántica de `ReferenceCode` cuando no está blanco.
5. Regla exacta de fecha de compensación y obligatoriedad de fecha descriptiva para todos los flujos.
6. Cobertura normativa completa de prenotificaciones, variantes de adenda y múltiples addendas por entrada.
7. Unicidad/idempotencia persistente de Trace y consecutivo externo bajo concurrencia.
8. Persistencia de fecha operacional `America/Bogota`, SQL Server y PostgreSQL, prevista para Ejecución 3.
9. Duplicate gate, naming y prevención de reenvío validados de extremo a extremo.
10. Vector externo oficial, certificación/homologación de ACH Colombia y pruebas de aceptación de cámara.
11. Aprobación humana de Operaciones, Compliance, Seguridad y dueño normativo.
12. Revisión completa de históricos, retención y exposición de datos fuera de las superficies modificadas.

Ninguna prueba o semejanza con archivos de terceros elimina estos bloqueantes. La implementación reduce el riesgo mediante reglas ejecutables, trazabilidad, fail-closed y pruebas reproducibles; no garantiza ausencia de sanciones.

## CENIT

**Decisión: NO-GO / NOT HOMOLOGATED / BLOCKED FOR LIVE**

### Control implementado

- La generación LIVE falla de forma cerrada si CENIT es placeholder/no homologado o carece de soporte normativo aprobado.
- El error contiene código/RuleId y contexto técnico, sin datos financieros o personales.
- El perfil CENIT permanece separado y disponible sólo para desarrollo controlado cuando la configuración lo permite.
- El gate no bloquea el perfil ACHCOL oficial.

### Reglas no demostradas — bloqueantes para LIVE

- Layout oficial completo.
- Naming oficial.
- Encoding oficial.
- Matriz normativa completa y RuleIds críticos.
- Variantes T7 y controles cruzados.
- Reglas de lotes, ciclo, Trace, hash, totales y fecha operacional.
- Manual STA/especificación técnica aplicable a CFA.
- Homologación explícita y aprobación humana.

No se creó golden CENIT válido ni se copió el layout ACHCOL. El fixture CENIT representa únicamente el rechazo por no homologación.

## Condiciones para reconsiderar ACHCOL

1. Resolver documentalmente todos los puntos no demostrados.
2. Ejecutar Ejecución 3 para persistencia/consecutivos/fecha operacional/concurrencia sin mezclar lote interno y consecutivo externo.
3. Ejecutar Ejecución 4: suite offline completa, SQL Server/PostgreSQL controlados, propiedades, seguridad y Compliance Gate.
4. Obtener certificación/homologación externa y aprobación humana antes de LIVE.

## Condiciones para reconsiderar CENIT

1. Obtener manuales oficiales vigentes y contrato aplicable a CFA.
2. Construir matriz CENIT completa sin inferencias desde ACHCOL o archivos de terceros.
3. Implementar perfil independiente, pruebas sintéticas, persistencia y gate de homologación explícita.
4. Obtener homologación externa y aprobación humana.
