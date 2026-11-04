# MEMORY_STATE — Correcciones incrementales CENIT (F06)

**Fecha:** 2026-11-02. **ZIP base histórica:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip`. **Base inmediata F05:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F05_2026-11-02.zip`, SHA-256 `df34d0c4c6f2bee8c5099657cdae9272da1f861beaff42bb71879dbd80e84ed0`, CRC OK, 4.644 entradas. Raíz `ACHInterbank_ajustado/`. Auditoría I01–I10 preservada sin cambios.

## Estado e hitos

- **Fase actual F06:** caracteres NACHA-M CENIT y separación errores STA / NACHA. **Completadas como entrega correctiva:** F01–F06 (**6/8 = 75 % de hitos**, NO homologación ni cumplimiento). **Siguiente fase:** F07 integración/regresión de ambos sistemas; F08 cierre documental.
- **Implementados — pendientes de verificación:** HC-CENIT-001 a HC-CENIT-013 (**13/13**). **No implementados:** 0. **Cerrados formalmente:** 0. **Bloqueos de verificación:** 13 por ausencia de SDK .NET / integración/operador; no se confunde documentación con pruebas aprobadas.
- **Fuente modificada F06 (5 archivos originales):** productivos `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/{NachaPhysicalParser,NachaParserService,NachaInboundValidation}.cs`; pruebas `tests/Cfa.ACHInterbank.Tests/{NachaPhysicalParserTests,NachaInboundValidationSeparationTests}.cs`. **Pruebas fuente nuevas:** 25 escenarios; 0 archivos xUnit nuevos. **Archivos nuevos:** script de 24 controles estáticos y seis artefactos de documentación F06.
- **Pruebas realmente ejecutadas:** F06 24/24 PASS; F05 26/26, F04 21/21, F03 17/17, F02 13/13, F01 12/12 PASS. Parche dry-run/rollback en 5 archivos. **xUnit ejecutadas 0, aprobadas 0, fallidas ejecutadas 0.** DB/Quartz/gateway CENIT/archivos oficiales/STA real/ACH funcional no ejecutados. Detalle `REGISTRO_PRUEBAS.md`, `F06_EVIDENCIA_CONTROLES_ESTATICOS.txt`.
- **ACH Colombia:** archivos específicos no modificados, compatibilidad del alfabeto y mapeo Dxx genérico preservados por cámara; regresión funcional pendiente. **CUD:** fuera de alcance; liquidación Tesorería externa.

## Decisiones y riesgos

1. El alfabeto físico CENIT usa ASCII explícito del manual 2026 y falla para Ñ/ñ (0xD1/0xF1). Para ACH Colombia se mantiene la tabla Latin-1 previa. La selección se toma de perfil publicado o perfil conocido ordinario/Return/ROR; perfiles dinámicos sin autoridad resuelta requieren revisión F07.
2. D01–D06 **se conservan en el catálogo CENIT de STA**: Anexo B 2023; no se eliminan ni renumeran. En validación NACHA CENIT ya no se devuelven sus códigos ni descripciones: usar identificación interna `CENIT-NACHA-FATAL` para estructura, `ERR_INV_PATTERN` solo para repertorio físico cuando aplicable. Esto no representa un código de rechazo real del operador; formal por entrada continúa `NachaValidationFailure`.
3. Códigos internos ACH Colombia quedan intactos en ruta sin perfil CENIT; NO duplicar semántica STA en NACHA, NO cambiar reglas de ACH Colombia ni crear entidades/migraciones.
4. F07 debe comprobar compilación y pruebas en SDK .NET, casos reales de `ERR_*` del manual, archivos con Ñ/ñ y STA D02 con relleno NACHA inválido; correlación CENIT vs ACH; revisar resultados previos de F01–F05 y pendientes normativos DN-CEN-007/008.
5. Siguen pendientes simulación/estados (F01), NACK/ADV (F02), colas interdiarias (F03), R23/R06 (F04), ROR/R74 (F05), PT-01–PT-22; certificación externa del operador y Tesorería manual. No declarar homologación por fase.

## Entrega y reversibilidad

`F06_INFORME_CORRECCIONES.md`, `F06_MEMORY_STATE.md`, `F06_CAMBIOS.patch`, `F06_EVIDENCIA_CONTROLES_ESTATICOS.txt`, `F06_COMPARACION_SHA256.tsv`, `MANIFIESTO_F06.md`, además de `REGISTRO_CORRECCIONES.md`, `MATRIZ_HALLAZGOS_CORRECCIONES.md`, `ESTADO_FASES.md`, `REGISTRO_PRUEBAS.md`, `PLAN_INCREMENTAL_CORRECCIONES.md`. **ZIP F06:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F06_2026-11-02.zip`. Su SHA-256 definitivo se registra en archivo lateral `.zip.sha256`, ya que el hash del propio ZIP no puede incorporarse a su contenido sin circularidad. Restaurar F05 o aplicar `F06_CAMBIOS.patch` inverso y recuperar documentos previos.
