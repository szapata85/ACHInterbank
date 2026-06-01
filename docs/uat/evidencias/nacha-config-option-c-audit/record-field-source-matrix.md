# Fase 6A - Matriz registros/campos/fuentes

| Registro | Campo/funcion | Fuente actual | Legacy | Profile | Hardcoded | Calculado | Fallback | Separado por camara | Riesgo | Accion Fase 6B |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 File Header | Layout posiciones/longitud | `NachaRecordLayout` o `CfgLayoutVariant` si modo/flags | Si | Parcial | No | No | Si | Parcial | LEGACY_DEPENDENCY | Usar solo `CfgLayoutVariant` oficial |
| 1 File Header | PriorityCode, record size, blocking factor, format | `FileHeaderRecord.From` | No | Parcial | Si | No | Si | No garantizado | HARDCODED_TO_REMOVE o documentar constante oficial | Parametrizar constantes como `CfgLayoutField`/source constante |
| 1 File Header | FileIdModifier | Header/ciclo y normalizacion posterior | Parcial | Parcial | Parcial | Parcial | Si | Parcial | MISSING_TRACE | Trazar fuente y normalizacion |
| 5 Batch Header | Layout | Legacy o profile parcial | Si | Parcial | No | No | Si | Parcial | LEGACY_DEPENDENCY | Usar `CfgProfileRecord` y `CfgLayoutVariant` |
| 5 Batch Header | Company/originator/status/SEC | Objetos dominio + constantes | Parcial | Parcial | Si | Parcial | Si | Parcial | MISSING_VALIDATION | Parametrizar campos no calculados y reglas |
| 6 Entry Detail | Datos transaccion/cuenta/monto | `AchTransaction` y renderer | Si | Parcial | No | Parcial | Si | Parcial | READY_FOR_TABLE_DRIVEN parcial | Resolver por `CfgFieldSourceDefinition` |
| 6 Entry Detail | ReceivingDFI length | `NachaRecordField` legacy | Si | No efectivo | No | No | Si | No | LEGACY_DEPENDENCY | Eliminar lectura legacy |
| 6 Entry Detail | Check digit | Codigo | No | No | No | Si | No | Si aplica | CALCULATED_OK | Mantener calculo en codigo y trazar |
| 7 Addenda | Construccion type 7 | Estrategia type7 + fallback | Si | Parcial | Parcial | Parcial | Si | Parcial por policy | LEGACY_DEPENDENCY | Hacer table-driven oficial por camara |
| 8 Batch Control | Totales/hash/count | Builder/control record | Parcial | Parcial | No | Si | Si | Parcial | CALCULATED_OK con trace faltante | Mantener calculos, render por profile |
| 9 File Control | BatchCount/BlockCount/totales | Builder/control record | Parcial | Parcial | No | Si | Si | Parcial | CALCULATED_OK con trace faltante | Mantener calculos, render por profile |

Clasificacion general: registros 1/5/6/7/8/9 estan parciales, no listos para declararse 100% table-driven oficial.

