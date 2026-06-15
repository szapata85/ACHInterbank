# Pendientes de validación funcional V3

## Objetivo

Registrar los pendientes que no deben olvidarse antes del cierre final del manual funcional ACH Interbank.

## Pendientes

| ID | Captura esperada | Tema | Ruta asociada | Estado | Motivo | Evidencia requerida | Responsable sugerido | Criterio de cierre |
|---|---|---|---|---|---|---|---|---|
| 53 | `53_nacha_security_certificate_versions_historial.png` | Versiones e historial de certificados | `/nacha-security/certificates/:id/versions` | Requiere validacion | No existe un certificado navegable confirmado para documentarlo como flujo estable. | Captura real del historial visible. | Administrador de certificados digitales | Captura real con historial navegable visible. |
| 54 | `54_nacha_security_certificates_estados_vigencia_si_visible.png` | Estados y vigencia de certificados | `/nacha-security/certificates` | Requiere validacion | No hay certificados cargados para mostrar estados o fechas de vigencia. | Captura real con estados o fechas visibles. | Administrador de certificados digitales | Captura real con estados o fechas de vigencia visibles. |
| 57 | `57_nacha_generate_base.png` | Generacion NACHA-M base | `/nacha-security/nacha/generate` | Requiere validacion | No se obtuvo resultado funcional utilizable en el entorno. | Captura real de la salida visible. | Administrador de certificados digitales | Captura real de la salida base visible. |
| 58 | `58_nacha_generate_encrypted.png` | Generacion NACHA-M cifrada | `/nacha-security/nacha/generate-encrypted` | Requiere validacion | No se obtuvo resultado funcional utilizable en el entorno. | Captura real de la salida cifrada visible. | Administrador de certificados digitales | Captura real de la salida cifrada visible. |
| 64 | `64_naming_archivo_base_por_camara_si_visible.png` | Naming archivo base por camara | `/nacha-security/nacha/generate` | Requiere validacion | No se obtuvo un nombre base visible para documentar. | Captura real del nombre base visible. | Administrador de certificados digitales | Nombre base visible en pantalla. |
| 65 | `65_naming_archivo_final_env_si_visible.png` | Naming archivo final .env | `/nacha-security/nacha/generate-encrypted` | Requiere validacion | No se obtuvo un nombre final exportable visible para documentar. | Captura real del nombre final visible. | Administrador de certificados digitales | Nombre final o extension visible en pantalla o exportacion. |

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
