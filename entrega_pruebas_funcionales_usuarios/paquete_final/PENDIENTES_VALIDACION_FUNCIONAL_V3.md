# Pendientes de validación funcional V3

## Objetivo

Registrar los pendientes que no deben olvidarse antes del cierre final del manual funcional ACH Interbank.

## Pendientes

| ID | Captura esperada | Tema | Ruta asociada | Estado | Motivo | Evidencia requerida | Responsable sugerido | Criterio de cierre |
|---|---|---|---|---|---|---|---|---|
| 50 | `50_onboarding_silencioso_si_aparece.png` | Onboarding silencioso en creacion de transaccion | `/transactions/create` | Requiere validacion funcional en flujo de transaccion | El onboarding silencioso ocurre durante el registro de una transaccion y no como pantalla independiente. | Caso de prueba de creacion de transaccion donde se confirme la asociacion o registro automatico. | Operador ACH / Usuario funcional de pruebas | Transaccion de prueba creada exitosamente con evidencia de que no se requirio alta manual previa y que la informacion quedo asociada correctamente. |
| 53 | `53_nacha_security_certificate_versions_historial.png` | Versiones e historial de certificados | `/nacha-security/certificates/:id/versions` | Requiere validacion | No existe un certificado navegable confirmado para documentarlo como flujo estable. | Captura real del historial visible. | Administrador de certificados digitales | Captura real con historial navegable visible. |
| 54 | `54_nacha_security_certificates_estados_vigencia_si_visible.png` | Estados y vigencia de certificados | `/nacha-security/certificates` | Requiere validacion | No hay certificados cargados para mostrar estados o fechas de vigencia. | Captura real con estados o fechas visibles. | Administrador de certificados digitales | Captura real con estados o fechas de vigencia visibles. |
| 57 | `57_nacha_generate_base.png` | Generacion NACHA-M base | `/nacha-security/nacha/generate` | Requiere evidencia funcional | La evidencia de cierre puede ser archivo generado, nombre visible, registro de exportacion o validacion funcional; no depende exclusivamente de printscreen. | Archivo NACHA-M base generado, registro de exportacion o salida visible. | Administrador de certificados digitales | Confirmar archivo base generado correctamente por camara. |
| 58 | `58_nacha_generate_encrypted.png` | Generacion NACHA-M cifrada | `/nacha-security/nacha/generate-encrypted` | Requiere evidencia funcional | La evidencia de cierre puede ser archivo generado, nombre visible, registro de exportacion o validacion funcional; no depende exclusivamente de printscreen. | Archivo cifrado o salida final generada. | Administrador de certificados digitales | Confirmar que la salida cifrada se genero desde el archivo base correcto. |
| 64 | `64_naming_archivo_base_por_camara_si_visible.png` | Naming archivo base por camara | `/nacha-security/nacha/generate` | Requiere evidencia funcional | La evidencia de cierre puede ser archivo generado, nombre visible, registro de exportacion o validacion funcional; no depende exclusivamente de printscreen. | Nombre de archivo base generado o visible. | Administrador de certificados digitales | Confirmar naming por camara. Para ACH Colombia `RRRRTTT.ZZZ.1`. Para CENIT, regla validada desde parametrizacion o evidencia generada. |
| 65 | `65_naming_archivo_final_env_si_visible.png` | Naming archivo final .env | `/nacha-security/nacha/generate-encrypted` | Requiere evidencia funcional | La evidencia de cierre puede ser archivo generado, nombre visible, registro de exportacion o validacion funcional; no depende exclusivamente de printscreen. | Archivo final exportable o nombre final visible. | Administrador de certificados digitales | Confirmar extension final `.env` solo si la aplicacion la genera o muestra. |

## Regla de cierre

Un pendiente solo puede cerrarse cuando exista al menos una de estas evidencias:

* Captura real de pantalla.
* Archivo generado visible.
* Nombre de archivo visible en la aplicacion.
* Registro de certificado de prueba visible.
* Resultado funcional verificable en el entorno local.

## Elementos que NO permiten cierre

No cerrar pendientes con:

* Suposiciones.
* Texto redactado sin evidencia.
* Pantallas vacias.
* Datos inventados.
* Naming no visible.
* Certificados productivos.
* Capturas con errores visuales o menu lateral dañado.
