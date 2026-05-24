# Fase 6A - Mapa de dependencia legacy

| Elemento legacy | Uso actual | Camara | Vigencia/version/status | Riesgo | Accion Fase 6B |
|---|---|---|---|---|---|
| `NachaRecordDefinition` | `NachaFileBuilder` carga definiciones y secuencia de registros | No | No | Define registros oficiales sin separacion ACH/CENIT | Reemplazar por `CfgProfileRecord` en modo oficial |
| `NachaRecordLayout` | `layoutCache` y renderer legacy/generico | No | No | Layout no normativo por camara | Migrar posiciones/longitudes a `CfgLayoutVariant` |
| `NachaRecordField` | Render de campos y longitud de `ReceivingDFI` en registro 6 | No | No | Dependencia funcional directa de legacy | Migrar a `CfgLayoutField`; eliminar lectura de longitud desde legacy |
| `NachaRecordLayoutAppService` | CRUD legacy | No | No | Sigue operativo para parametrizacion ambigua | Convertir a solo lectura/deprecado o redirigir |
| `NachaRecordDefinitionAppService` | CRUD legacy | No | No | Permite cambiar secuencia fuera del modelo oficial | Convertir a solo lectura/deprecado o redirigir |
| `/ach-cycles/nacha/layouts` | Pantalla visible de layouts | No | No | Usuario ve dos lugares para configurar NACHA-M | Marcar legacy/deprecar en menu |
| `/ach-cycles/nacha/definitions` | Pantalla visible de definiciones | No | No | Ambiguedad operativa | Marcar legacy/deprecar en menu |

La dependencia legacy no es solo visual. Es funcional en el builder y en la seleccion de registros/campos. Opcion C requiere cortar esta dependencia en modo oficial y dejar legacy solo como referencia historica o herramienta de migracion.

