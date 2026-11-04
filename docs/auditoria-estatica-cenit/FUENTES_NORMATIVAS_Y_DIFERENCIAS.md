# Fuentes y diferencias de versión — corte 2026-11-01

## Jerarquía práctica por objeto de la regla

| Fuente | Versión verificada en el archivo | Ubicación portable | Uso específico / límite |
|---|---|---|---|
| DSP-152 reglamento, 27-feb-2025 | Portada/hojas de `ceos_dsp_152_MAY_27_2022.md` describen 27-feb-2025, modifican régimen 2023 y Anexo 2 de 2024; **nombre histórico engañoso** | `docs/normativa/md/ceos_dsp_152_MAY_27_2022.md` | Definiciones, CUD, STA vs compensación, modificatorias; no se trata como edición 2022 por su nombre |
| DSP-152 Anexo 2, 27-feb-2025 | Hojas Cap. 1, 2025; 5 ciclos; 3.1–3.3, 4, 5, 12 | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | Horas, elegibilidad, plazos, colas, algoritmo 4/5. Referencia operativa principal de I02–I09 |
| Anexo A devoluciones, 28-nov-2023 | Fecha interna 2023, R23 y tablas R60–R74 | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | Causales específicas del servicio de compensación; se requiere comparar vigencia junto a 2025 |
| Anexo B rechazos STA, 28-nov-2023 | Fecha interna 2023, D01–D06 | `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md` | **Solo** Servicio de Transferencia de Archivos STA, no causa de Return In NACHA |
| Manual Usuario CENIT-WEB, 13-jun-2025 | Fecha interna 2025, capítulos 3.1–3.6 | `docs/normativa/md/Archivos de CENIT/Manual de Usuario CENIT.md` y PDF adjunto | Guía funcional por sesión; subordinar al Anexo 2 en reglas reglamentarias |
| Manual especificaciones NACHA-M CENIT, 07-may-2026 | Fecha interna 2026; 106 caracteres, registros 1/5/6/7/8/9, adendas/ADV | `docs/normativa/md/Archivos de CENIT/Manual de Especificaciones Formato NACHA-M CENIT.md` y PDF adjunto | Especificación técnica aportada; validar ajustes/puesta en producción antes de proclamar vigencia externa confirmada |
| Manual ACH Colombia v35, abr-2026 | Versión 35 | `docs/normativa/md/ACH-Colombia-V35.md` **si existe en base**; adicional PDF adjunto | Solo contraste de separación; **no** normativa CENIT |
| Nuevos Ciclos de Proceso y Horarios | Documento adjunto ACH Colombia con 7 ciclos en tabla y frase contradictoria «cinco (7)» | PDF adjunto | No aplicar a las 5 sesiones CENIT |
| WSDL WSCFAACH / WSAxon | Contratos entregados, no incluyen fecha regulatoria CENIT | `fuentes_adjuntas/*.wsdl.xml` | Evidencia de integración CFA/SOAP que requiere mapeo; no equiparar al operador CENIT |
| IAM Security UX propuesta futura | Documento de propuesta, **no** norma CENIT | `fuentes_adjuntas/IAM-SECURITY-UX-PROPUESTA-FUTURA.md` | Contexto técnico, fuera de la auditoría normativa CENIT salvo controles transversales |

**Consulta pública puntual (01-nov-2026):** [DSP-152 oficial 27-feb-2025](https://banrep.gov.co/es/circular-externa-operativa-servicios-dsp-152-asunto-1-reglamento-sistema-compensacion-electronica-4), [Manual operativo CENIT](https://banrep.gov.co/es/manual-operativo-sistema-compensacion-electronica-nacional-interbancaria-cenit), [ficha 2023 DSP-152](https://banrep.gov.co/es/circular-externa-operativa-servicios-dsp-152-asunto-1-reglamento-sistema-compensacion-electronica-2). La consulta encontró 2025; no equivale a declaración oficial de no existir una versión más nueva en WSEBRA. El NACHA 2026 se soporta en el documento suministrado, no en una publicación pública comprobada en I01.

## Diferencias y registro de decisión documental

| ID | Evidencia | Clasificación | Tratamiento sin inventar norma |
|---|---|---|---|
| DN-CEN-001 | Archivo `ceos_dsp_152_MAY_27_2022.md` tiene nombre 2022 pero texto inicial de modificación fechado 2025 y hojas de texto heredadas 2023 | **Metadatos/procedencia** (fecha de archivo no fiable), abierta | Usar fecha y artículo **de cada hoja**, cotejar versión oficial 2025. No inferir que todas sus páginas son de 2022 ni de 2025 |
| DN-CEN-002 | Anexo 2 2025 frente a Anexo A/B con fecha interna 2023 | **Diferencia de fecha no contradictoria demostrada**; vigencia documental a verificar | Anexo 2 remite expresamente a Anexo A; aplicar A/B al ámbito correcto, verificar futuras sustituciones |
| DN-CEN-003 | NACHA-M 07-may-2026 más reciente que Anexo 2 2025, con alcance técnico diferente | **Complementariedad temporal; vigencia externa pendiente** | Usar edición 2026 aportada para trazabilidad de formato, sin suponer cambio tácito a plazos/ciclos |
| DN-CEN-004 | Anexo A ROR tabla 2 R60–R69 y continuación R70–R74 | **Alcance de tabla/documento**, no contradicción | No limitar el inventario a R69; evaluar condiciones por causal y servicio en I07 |
| DN-CEN-005 | Ficha de ACH Colombia 7 ciclos y CENIT 5 ciclos | **Servicios distintos, no contradicción** | No trasplantar horarios/causales |
| DN-CEN-006 | WSDL SOAP CFA y WSAxon, sin contrato de compensación CENIT/CUD | **Ámbito distinto / evidencia faltante** | No deducir homologación CENIT ni liquidación externa de estos WSDL |
| DN-CEN-007 | Anexo 2 (27-feb-2025) §3.2 limita devoluciones de ciclo anterior en C5, mientras §4.8 permite devolver prenotificaciones «máximo en el último ciclo de Devoluciones» del día | **Tensión de alcance dentro del mismo documento; interpretación pendiente**, abierta desde I06 | No resolver por intuición. Verificar con circular aclaratoria vigente si prenotificación original C1–C3 puede devolverse en C5; **no** crear rechazo definitivo ni hallazgo de código hasta contar con criterio autorizado |

**Prioridad de fuentes:** norma DSP-152 aplicable sobre guías de UI; manual técnico NACHA-M en formato; anexos A y B por servicio. Cuando una discrepancia afecte recepción, corte, fecha valor, saldo o causal, marcar `No demostrable` y solicitar fuente vigente autorizada; nunca elegir por intuición. No se estableció una **contradicción sustantiva irresoluble confirmada** entre las fuentes principales durante I01.


## Nota I07: contraste por causal sin cambiar jerarquía

La **tabla 2 Anexo A (28-11-2023) contiene R60–R74**, incluida R74 limitada a crédito monetario devuelto por **R32** con adenda original campo 3 presente. `RegulatoryCatalogSeeder.cs` (I07, HC-008) no conserva esa restricción por razón de retorno padre. La falla es de catálogo **consumido por servicio ROR**, no una contradicción documental.

El **§4.2 del Anexo 2 (27-02-2025)** cuenta quince días desde recepción de entrada y fija trámite máximo en día hábil bancario siguiente a notificación. El método CENIT usa `ReceiverRejectionDeadlineDate` aportada por DTO y `OriginalValueDate` como ancla de 15 días: **diferencia entre norma y prueba del cálculo**, registrada HC-009; la fecha real de recepción puede no coincidir con fecha valor, pendiente de evidencia. No se introdujo variante normativa no suministrada. Anexo B sigue siendo **STA** exclusivamente. DN-CEN-001…007 permanecen abiertas/según clasificación previa.

## Nota documental I08 (no sustitución normativa)

| ID | Comparación de fuente / implementación | Clasificación | Tratamiento |
|---|---|---|---|
| DN-CEN-008 | Manual NACHA-M 07-05-2026 §6.1 prescribe `RRRRTTT.ZZZ.1` **para envíos de participantes**; el parser auxiliar de **archivos entrantes** y `CenitOfficialFileNameParser` reconocen patrones con fecha (`RRRRTTT.ZZZ.YYYYMMDD.N`) y/o RET. | **Diferencia de dirección/canal aún no resuelta**, no contradicción normativa demostrada por sí sola. | No imponer patrón outbound a todo archivo recibido. Exigir muestras y contrato reales de salida/entrada CENIT antes de clasificar incumplimiento adicional. |

El manual CENIT §4.1 lista caracteres permitidos y no incluye `Ñ/ñ` (HC-CENIT-010); §7.3.3 identifica ADV 280/ADV (HC-CENIT-012). El Anexo B D01–D06 corresponde al **STA** y no al parser NACHA-M del Servicio de Compensación y Liquidación (HC-CENIT-011). Ninguna de estas tres discrepancias es una contradicción entre fuentes: son diferencias código–fuente normativa. La verificación pública de vigencia posterior a las ediciones adjuntas sigue pendiente; no se inventó publicación sustituta.


## Decisión de alcance I09 — CUD manual, no conflicto normativo

- El Reglamento DSP-152 del **27-feb-2025**, **arts.18–19**, asigna al **Banco de la República** la compensación multilateral y liquidación contra las Cuentas de Depósito. El Anexo 2 §5.2 asigna la aplicación del algoritmo al Sistema CENIT. **No se deduce obligación de que ACH Interbank implemente integración CUD**. La inexistencia de API no es un hallazgo.
- El usuario confirma que Tesorería verifica CUD manualmente. Se registra como **decisión de alcance**, no como una corrección o interpretación que altere la circular. La plataforma gestiona resultados/archivos y conciliación operativa; los movimientos y comprobaciones bancarios permanecen externos.
- HC-CENIT-003/005/006 se reinterpretan como **efectos internos indebidos de una simulación** que sí es invocada por Quartz. Si el módulo deja de decidir estados reales, no debe exigirse que replique el algoritmo CENIT. Los hallazgos **no se cierran** sin intervención y pruebas.
- La regla incondicional ACH Colombia v35 que aparece en un servicio genérico de conciliación queda bajo **observación parcial**, no como conflicto entre normas CENIT ni incumplimiento demostrado: no se localizó consumidor productivo de ese servicio en I09.
- **Se preservan DN-CEN-001…008**; la decisión CUD no añade artificialmente otra discrepancia documental.


## Verificación documental de cierre I10 (02-nov-2026)

Se contrastaron las referencias públicas del Banco de la República [reglamentación CENIT](https://www.banrep.gov.co/es/normatividad/sistemas-pago/reglamentacion-ach-cenit), [DSP-152 27-feb-2025](https://banrep.gov.co/es/circular-externa-operativa-servicios-dsp-152-asunto-1-reglamento-sistema-compensacion-electronica-4), [PDF Reglamento](https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/ceos_dsp-152_Asunto_1_feb_27_2025.pdf) y [PDF Anexo 2](https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/manual_dsp_cenit.pdf). Se sigue utilizando 2025 como referencia aportada/publicada; esta revisión no demuestra que WSEBRA o comunicaciones a participantes carezcan de otras modificaciones. El manual NACHA-M **07-may-2026** es una edición **aportada**, aún sin prueba de activación externa. **DN-CEN-001…008 permanecen clasificadas como en el registro**, sin resolver tensiones por inferencia.

No hay contradicción por dejar CUD fuera del código: DSP-152 arts.18–19 atribuyen neteo y liquidación al Banco; Tesorería mantiene la verificación manual institucional.
