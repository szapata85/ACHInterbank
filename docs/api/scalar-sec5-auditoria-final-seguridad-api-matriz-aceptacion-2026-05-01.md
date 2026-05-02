# Scalar-SEC-5 — Auditoría final de seguridad API y matriz de aceptación (2026-05-01)

## 1. Resumen ejecutivo
Con base en la evidencia consolidada de SEC-5A, se documenta el cierre del alcance evaluado para autorización explícita y metadata OpenAPI/Scalar de seguridad. El resultado del alcance auditado es satisfactorio, con riesgos residuales identificados y plan de continuidad.

## 2. Alcance
- Controladores y endpoints evaluados en la línea Scalar-SEC (P0, P1, P2).
- Endurecimiento de autorización explícita con `Authorize`/`Policy`.
- Validación de metadata `security` y esquema `Bearer/JWT` en OpenAPI runtime.
- Verificación de endpoints públicos justificados.
- Validación técnica por pruebas específicas, suite completa backend y build final.

## 3. Fuera de alcance
- Pruebas de penetración.
- Validación de infraestructura productiva (WAF, TLS, reverse proxy, hardening de red).
- Cierre de gestión de secretos/OpenBao en operación real.
- UAT de seguridad y cumplimiento externo.

## 4. Línea de tiempo SEC-1 a SEC-4B
- **SEC-1 / SEC-1A:** auditoría inicial y evidencia de autorización explícita.
- **SEC-2:** hardening P0 (Transactions, AchTraceability, AchReturns).
- **SEC-3 / SEC-3A:** metadata OpenAPI de seguridad y validación runtime (`Bearer/JWT`).
- **SEC-4 / SEC-4A:** hardening ampliado P1/P2 y rescate de evidencias.
- **SEC-4B:** cierre puntual de rutas pendientes sin security (`PUT /api/users/branding`, `POST /Nacha/header`) y justificación de `GET /api/users/branding`.

## 5. Evidencia técnica consolidada desde SEC-5A
- OpenAPI runtime final: 213 operaciones.
- 207 operaciones con security.
- 6 operaciones sin security, justificadas.
- CSV finales de operaciones, sin security, AllowAnonymous y escritura.
- Pruebas específicas: 26/26.
- Suite completa backend: 418/418.
- Build final exitoso.

## 6. Estado de hardening P0
P0 endurecido y validado con autorización explícita y policies de lectura/gestión según acción. Sin hallazgos abiertos en P0 para este alcance.

## 7. Estado de hardening P1/P2
P1/P2 endurecido para endpoints operativos y de configuración; excepciones públicas formalmente justificadas en autenticación/token y branding de login.

## 8. Estado de metadata OpenAPI/Scalar
Se confirma metadata `security` en OpenAPI para endpoints protegidos y esquema `Bearer` en `components.securitySchemes`.

## 9. Estado de endpoints AllowAnonymous
`AllowAnonymous` queda limitado al conjunto público esperado de autenticación/recuperación/token y lectura pública de branding.

## 10. Estado de endpoints de escritura
Las escrituras operativas bancarias se encuentran protegidas. Las únicas escrituras sin security corresponden a endpoints públicos de autenticación/token.

## 11. Matriz de aceptación
| Criterio | Evidencia | Resultado | Estado |
|---|---|---|---|
| Build inicial exitoso | SEC-5A | Exitoso | Cumple |
| Build final exitoso | SEC-5A | Exitoso | Cumple |
| Suite completa backend exitosa | SEC-5A | 418/418 | Cumple |
| Pruebas específicas de autorización exitosas | SEC-5A | 26/26 | Cumple |
| OpenAPI real generado | SEC-5A | `/tmp/openapi-sec5a1.json` | Cumple |
| Bearer/JWT presente | CSV/OpenAPI SEC-5A | `SECURITY_SCHEMES=['Bearer']` | Cumple |
| 213 operaciones OpenAPI | CSV general SEC-5A | 213 | Cumple |
| 207 operaciones con security | CSV general SEC-5A | 207 | Cumple |
| 6 operaciones sin security justificadas | CSV sin security SEC-5A | 6 | Cumple |
| P0 con security | SEC-2/SEC-3A/SEC-5A | Validado | Cumple |
| P1/P2 con security | SEC-4A/SEC-4B/SEC-5A | Validado | Cumple |
| Endpoints de escritura operativa con security | CSV escritura SEC-5A | Validado | Cumple |
| AllowAnonymous limitado a endpoints públicos | CSV AllowAnonymous SEC-5A | Validado | Cumple |
| CSV finales generados | SEC-5A | 4 CSV finales | Cumple |
| No se cambiaron rutas ni contratos en SEC-5 | Acta SEC-5A | Declarado y mantenido | Cumple |
| No se declara producción lista | SEC-5A/SEC-5 | Declarado | Cumple |

## 12. Matriz de endpoints sin security justificados
| Método | Ruta | Tipo | Justificación | Riesgo residual | Estado |
|---|---|---|---|---|---|
| POST | /Auth/login | Autenticación | Endpoint público de autenticación | Exposición de superficie de login | Aceptado con controles |
| POST | /Auth/forgot-password | Recuperación | Endpoint público de recuperación | Abuso de recuperación | Aceptado con controles |
| POST | /Auth/reset-password | Recuperación | Endpoint público de recuperación | Abuso de restablecimiento | Aceptado con controles |
| GET | /api/users/branding | Branding público | Consulta de branding para pantalla de login | Exposición de configuración visual | Aceptado con control funcional |
| POST | /Oauths/GenerateToken | Token | Endpoint público/preautenticado de token | Abuso de emisión | Aceptado con controles |
| POST | /Oauths/GenerateTokenAsync | Token | Endpoint público/preautenticado de token | Abuso de emisión | Aceptado con controles |

## 13. Matriz de riesgos residuales
| Riesgo | Descripción | Severidad | Mitigación actual | Acción posterior |
|---|---|---|---|---|
| Endpoints de autenticación expuestos por diseño | Login/recovery/token públicos | Alta | Policies y validaciones de aplicación | Pentest + hardening de rate limit/captcha |
| Endpoint branding público | Lectura pública de branding | Media | Alcance limitado de datos | Revalidar contenido expuesto en UAT seguridad |
| Uso de políticas genéricas CanReadAch/CanManageAch | Granularidad por dominio puede ser insuficiente | Alta | Matriz de policies vigente | Diseñar permisos finos por dominio |
| Falta de permisos finos por dominio | No todo el dominio usa permissions especializados | Alta | Controles actuales por controller/acción | Programa de refinamiento RBAC/ABAC |
| Falta de pruebas de penetración | No ejecutadas en este cierre | Alta | Pruebas funcionales y unitarias | Ejecutar pentest interno/externo |
| Falta de UAT de seguridad | UAT seguridad pendiente | Alta | Evidencia técnica de QA | Plan de UAT con negocio y seguridad |
| Gestión de secretos/OpenBao fuera del cierre | Validación operativa no incluida | Alta | Documentación previa | Auditoría y pruebas operativas de secretos |
| Infraestructura, TLS, WAF y reverse proxy fuera del cierre | No cubierto por esta línea | Alta | Configuración base existente | Revisión DevSecOps integral |
| Validación externa/compliance pendiente | Falta homologación regulatoria completa | Alta | Evidencia técnica parcial | Evaluación compliance externa |

## 14. Matriz de controles implementados
| Control | Implementación | Evidencia |
|---|---|---|
| Autorización explícita por controller/acción | `Authorize` + `Policy` | SEC-2 a SEC-4B |
| Seguridad OpenAPI por operación | Transformer de metadata `security` | SEC-3/SEC-3A |
| Esquema Bearer/JWT en OpenAPI | Document transformer | SEC-3/SEC-3A |
| Validación de endpoints públicos | CSV sin security + AllowAnonymous | SEC-5A |
| Validación de escrituras | CSV escritura con security | SEC-5A |
| Prevención de regresión | Pruebas unitarias de uniformidad | SEC-4B/SEC-5A |

## 15. Qué se declara cerrado
Se declara cerrado, para el alcance evaluado de esta línea Scalar-SEC, el frente de autorización explícita y metadata OpenAPI/Scalar de seguridad para los controladores revisados, con evidencia de:

- hardening P0;
- hardening P1/P2;
- metadata Bearer/JWT en OpenAPI;
- endpoints de escritura operativa protegidos;
- endpoints públicos justificados;
- pruebas específicas exitosas;
- suite completa backend exitosa;
- OpenAPI real y CSV finales generados.

## 16. Qué NO se declara cerrado
- No se declara producción lista.
- No se declara cierre de pruebas de penetración.
- No se declara cierre de gestión de secretos.
- No se declara cierre de OpenBao.
- No se declara cierre de infraestructura.
- No se declara cierre de WAF, TLS ni reverse proxy.
- No se declara cierre de SIEM/SOC.
- No se declara cierre de UAT de seguridad.
- No se declara cierre de compliance externo.
- No se declara que CanReadAch/CanManageAch sean permisos finos suficientes para todos los dominios.
- No se cambiaron rutas ni contratos en SEC-5.

## 17. Requisitos antes de producción
1. UAT de seguridad.
2. Pruebas de penetración.
3. Validación de roles reales.
4. Validación de claims/permissions del proveedor de identidad.
5. Revisión de permisos finos.
6. Validación de secretos/OpenBao.
7. Validación TLS/reverse proxy/WAF.
8. Revisión de logs/auditoría/SIEM.
9. Pruebas de expiración/refresh token.
10. Aprobación de arquitectura, seguridad, operación y cumplimiento.

## 18. Recomendaciones de siguientes fases
- Ejecutar SEC-5B-2 con matrices de aceptación firmadas por Seguridad/Arquitectura/Operación.
- Priorizar plan de permisos finos por dominio.
- Incorporar validación de seguridad dinámica (DAST/pentest) al pipeline de liberación.

## 19. Veredicto
**SEC-5B-1: CERRADO.**

**SEC-5 completo: cerrado para el alcance técnico evaluado de autorización explícita y metadata OpenAPI/Scalar, con riesgos residuales controlados y acciones posteriores obligatorias.**
