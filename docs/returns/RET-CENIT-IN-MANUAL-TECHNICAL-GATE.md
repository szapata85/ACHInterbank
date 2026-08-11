# Gate Manual Técnico CENIT Return In

Alcance: datos del contrato `RAW CENIT -> Return normalizado interno` que deben contrastarse exclusivamente contra el Manual de Especificaciones Técnicas CENIT vigente. La política operativa, correlación, persistencia, estados e idempotencia posteriores al parse no sustituyen esta homologación.

| Dato pendiente | Por qué se necesita | Código afectado | Riesgo de inferirlo | Estado |
| -------------- | ------------------- | --------------- | ------------------- | ------ |
| Registro exacto de devolución | Identificar sin ambigüedad el evento transaccional entrante | `NachaParserService`, perfiles NACHA-M CENIT | Clasificar otro registro como devolución | PENDIENTE_MANUAL |
| Tipo exacto de addenda | Seleccionar el registro complementario autorizado | parser, clasificador y frontera de normalización | Confundir addendas o descartar evidencia | PENDIENTE_MANUAL |
| Estructura de addenda | Extraer únicamente los campos oficiales | parser y mapper raw-normalizado | Interpretar datos no equivalentes | PENDIENTE_MANUAL |
| Referencia a la transacción original | Alimentar la correlación interna determinística | mapper raw-normalizado y `IncomingNachaTransactionLinker` | Vincular una transacción incorrecta | PENDIENTE_MANUAL |
| Número de secuencia original | Preservar la referencia oficial cuando aplique | parser, mapper y evidencia de linking | Duplicados o correlación falsa | PENDIENTE_MANUAL |
| Routing / DFI | Interpretar las entidades participantes según el contrato oficial | parser, mapper y clasificación de cámara | Cámara o participante incorrectos | PENDIENTE_MANUAL |
| Código de transacción | Distinguir débito, crédito, prenotificación y devolución raw | parser y clasificador funcional | Aplicar una política a un tipo equivocado | PENDIENTE_MANUAL |
| Campos preservados del original | Construir el Return normalizado con trazabilidad suficiente | mapper raw-normalizado | Pérdida o alteración de identidad transaccional | PENDIENTE_MANUAL |
| Controles del archivo y registros | Validar integridad antes del post-parse | parser y validadores NACHA-M | Aceptar archivos truncados o inconsistentes | PENDIENTE_MANUAL |
| Longitudes | Delimitar registros y campos oficialmente | perfiles y parser NACHA-M CENIT | Lectura desplazada o silenciosamente corrupta | PENDIENTE_MANUAL |
| Posiciones | Mapear cada dato raw al campo normalizado | perfiles y parser NACHA-M CENIT | Causal, referencia o valor incorrectos | PENDIENTE_MANUAL |
| Naming externo | Resolver y validar nombres de archivos CENIT oficiales | ingesta y resolución de perfil/cámara | Aceptar o rechazar archivos incorrectamente | PENDIENTE_MANUAL |

No se infirieron `OriginalTrace`, `OriginalTraceRef`, addenda, DFI, offsets, longitudes, posiciones, controles ni naming externo. La ruta raw CENIT permanece bloqueada con `DEPENDENCIA_MANUAL_TECNICO_CENIT` hasta completar este gate.
