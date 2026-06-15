# COBERTURA FUNCIONAL MANUAL V3

| Tema | Actor principal | Ruta SPA revisada | Pantalla encontrada | Captura requerida | Nombre captura | Estado | ObservaciÃ³n |
|---|---|---|---|---|---|---|---|
| Terceros de prenotificaciÃ³n | Operador ACH | `/customer-third-parties` | Lista y bÃºsqueda de terceros | SÃ­ | `28_customer_third_parties_listado_busqueda.png` | Agregar captura | Cubre consulta, filtros y seguimiento. |
| CreaciÃ³n de terceros, si existe | Operador ACH | `/customer-third-parties` | AcciÃ³n interna por confirmar | SÃ­ | `29_customer_third_parties_creacion_si_existe.png` | Requiere validaciÃ³n | Validar si existe botÃ³n, modal o acciÃ³n interna. |
| Clientes | Operador ACH | `/customers` | Listado de clientes | SÃ­ | `30_customers_listado.png` | Agregar captura | Cubre consulta de clientes. |
| CreaciÃ³n de clientes | Operador ACH | `/customers/new` | Formulario de alta | SÃ­ | `31_customers_nuevo.png` | Agregar captura | Validar formulario de creaciÃ³n. |
| Entidades financieras | Administrador de catÃ¡logos / parametrizaciÃ³n | `/catalogs/financial-institutions` | Mantenimiento de entidades financieras | SÃ­ | `32_financial_institutions_mantenimiento_digito_verificacion.png` | Agregar captura | Incluye el dÃ­gito visible si la pantalla lo muestra. |
| PriorizaciÃ³n por cÃ¡mara | Administrador funcional ACH | `/catalogs/clearing-house-preferences` | Preferencias por cÃ¡mara | SÃ­ | `33_clearing_house_preferences_prioridades_camara.png` | Agregar captura | Validar prioridades ACH Colombia y CENIT. |
| Tipos de documento | Administrador de catÃ¡logos / parametrizaciÃ³n | `/catalogs/document-types` | CatÃ¡logo de tipos de documento | SÃ­ | `34_catalog_document_types.png` | Agregar captura | CatÃ¡logo maestro. |
| Sexo / gÃ©nero | Administrador de catÃ¡logos / parametrizaciÃ³n | `/catalogs/gender-types` | CatÃ¡logo de gÃ©nero | SÃ­ | `35_catalog_gender_types.png` | Agregar captura | CatÃ¡logo maestro. |
| Tipos de persona | Administrador de catÃ¡logos / parametrizaciÃ³n | `/catalogs/person-types` | CatÃ¡logo de persona | SÃ­ | `36_catalog_person_types.png` | Agregar captura | CatÃ¡logo maestro. |
| CÃ³digos de transacciÃ³n ACH | Administrador de catÃ¡logos / parametrizaciÃ³n | `/catalogs/transaction-codes` | CatÃ¡logo de transacciones | SÃ­ | `37_catalog_transaction_codes.png` | Agregar captura | CatÃ¡logo maestro. |
| Conceptos de lote | Administrador de catÃ¡logos / parametrizaciÃ³n | `/catalogs/company-entry-descriptions` | Conceptos de lote | SÃ­ | `38_catalog_company_entry_descriptions.png` | Agregar captura | CatÃ¡logo maestro. |
| Reglas por cÃ¡mara | Administrador funcional ACH | `/transactions/clearing-house-rules` | Reglas por cÃ¡mara | SÃ­ | `39_transactions_clearing_house_rules.png` | Agregar captura | Cubrir reglas funcionales visibles. |
| ConfiguraciÃ³n de ciclos | Administrador funcional ACH | `/transactions/cycle-configs` | ConfiguraciÃ³n de ciclos | SÃ­ | `40_transactions_cycle_configs.png` | Agregar captura | Validar vigencia y control de cambios. |
| Causales de devoluciÃ³n CENIT | Administrador funcional ACH | `/cenit/regulatorio/causales-devolucion` | Causales de devoluciÃ³n | SÃ­ | `41_cenit_causales_devolucion.png` | Agregar captura | CatÃ¡logo regulatorio CENIT. |
| Causales de rechazo CENIT | Administrador funcional ACH | `/cenit/regulatorio/causales-rechazo` | Causales de rechazo | SÃ­ | `42_cenit_causales_rechazo.png` | Agregar captura | CatÃ¡logo regulatorio CENIT. |
| PolÃ­ticas de transacciÃ³n CENIT | Administrador funcional ACH | `/cenit/regulatorio/politicas-transaccion` | PolÃ­ticas de transacciÃ³n | SÃ­ | `43_cenit_politicas_transaccion.png` | Agregar captura | PolÃ­ticas visibles en pantalla. |
| PolÃ­ticas de prenotificaciÃ³n CENIT | Administrador funcional ACH | `/cenit/regulatorio/politicas-prenotificacion` | PolÃ­ticas de prenotificaciÃ³n | SÃ­ | `44_cenit_politicas_prenotificacion.png` | Agregar captura | PolÃ­ticas visibles en pantalla. |
| Perfiles NACHA-M | Administrador funcional ACH | `/nacha-config-admin/perfiles` | Listado de perfiles oficiales | SÃ­ | `45_nacha_config_perfiles.png` | Agregar captura | Vista principal de perfiles. |
| Detalle de perfil NACHA-M, si existe | Administrador funcional ACH | `/nacha-config-admin/perfiles/:id` | Detalle navegable de perfil | SÃ­ | `46_nacha_config_perfil_detalle_si_existe.png` | Requiere validaciÃ³n | Solo si existe un registro navegable. |
| Dashboard operativo NACHA-M | Revisor / validador operativo | `/ach/nacha/operational-dashboard` | Panel operativo | SÃ­ | `47_nacha_operational_dashboard.png` | Agregar captura | Consulta operativa read-only. |
| ConciliaciÃ³n ACH | Revisor / validador operativo | `/ach/reconciliation` | Consola de conciliaciÃ³n | SÃ­ | `48_ach_reconciliation.png` | Agregar captura | Revisar diferencias y trazabilidad. |
| Reporte de rechazos | Revisor / validador operativo | `/reports/rejections` | Reporte de rechazos | SÃ­ | `49_reports_rejections.png` | Agregar captura | Soporta validaciÃ³n operativa. |
| Onboarding silencioso, si aparece | Usuario funcional de pruebas | `/customers/new` | Ayuda, prellenado o flujo automÃ¡tico | SÃ­ | `50_onboarding_silencioso_si_aparece.png` | Requiere validaciÃ³n | Solo si la interfaz lo muestra de forma visible. |
| Dashboard seguridad NACHA-M | Administrador de certificados digitales | `/nacha-security/dashboard` | Panel de seguridad NACHA | SÃ­ | `51_nacha_security_dashboard.png` | Agregar captura | Consola base de seguridad. |
| Consulta / gobierno de certificados | Administrador de certificados digitales | `/nacha-security/certificates` | Gobierno de certificados | SÃ­ | `52_nacha_security_certificates_gobierno.png` | Agregar captura | Vista principal de certificados. |
| Versiones e historial de certificados | Administrador de certificados digitales | `/nacha-security/certificates/:id/versions` | Versionado de certificado | SÃ­ | `53_nacha_security_certificate_versions_historial.png` | Agregar captura | Evidenciar historial. |
| Estados y vigencia de certificados | Administrador de certificados digitales | `/nacha-security/certificates` | Estados y fechas visibles | SÃ­ | `54_nacha_security_certificates_estados_vigencia_si_visible.png` | Agregar captura | Registrar solo estados y fechas mostrados. |
| RotaciÃ³n / reemplazo de certificados, si existe | Administrador de certificados digitales | `/nacha-security/certificates` | AcciÃ³n visible de gestiÃ³n | SÃ­ | `55_nacha_security_certificates_rotacion_reemplazo_si_existe.png` | Requiere validaciÃ³n | Solo si existe activar, revocar, reemplazar o versionar. |
| AuditorÃ­a de certificados / sobre digital | Revisor / validador operativo | `/nacha-security/digital-envelope/audit` | AuditorÃ­a operacional | SÃ­ | `56_nacha_security_audit_sobre_digital.png` | Agregar captura | Trazabilidad y eventos visibles. |
| GeneraciÃ³n NACHA-M base | Administrador de certificados digitales | `/nacha-security/nacha/generate` | GeneraciÃ³n base | SÃ­ | `57_nacha_generate_base.png` | Agregar captura | Validar nombre mostrado si aparece. |
| GeneraciÃ³n NACHA-M cifrada | Administrador de certificados digitales | `/nacha-security/nacha/generate-encrypted` | GeneraciÃ³n cifrada | SÃ­ | `58_nacha_generate_encrypted.png` | Agregar captura | Validar salida cifrada si aparece. |
| Cifrado manual con sobre digital | Administrador de certificados digitales | `/nacha-security/digital-envelope/manual-encrypt` | Cifrado manual | SÃ­ | `59_nacha_manual_encrypt_sobre_digital.png` | Agregar captura | Flujo manual visible. |
| Descifrado manual con sobre digital | Administrador de certificados digitales | `/nacha-security/digital-envelope/manual-decrypt` | Descifrado manual | SÃ­ | `60_nacha_manual_decrypt_sobre_digital.png` | Agregar captura | Flujo manual visible. |
| Herramienta sobre digital | Administrador de certificados digitales | `/nacha-security/sobre-digital` | Herramienta integral | SÃ­ | `61_sobre_digital_tool.png` | Agregar captura | Consola operativa de sobre digital. |
| Interoperabilidad / vector oficial | Soporte tÃ©cnico funcional | `/nacha-security/digital-envelope/interoperability` | Interoperabilidad | SÃ­ | `62_interoperabilidad_vector_oficial.png` | Agregar captura | Solo lo que muestre la pantalla. |
| ExportaciÃ³n NACHA-M desde ciclo | Operador ACH | `/ach-cycles/nacha/export` | ExportaciÃ³n desde ciclo | SÃ­ | `63_ach_cycles_nacha_export.png` | Agregar captura | Evidenciar salida exportable. |
| Naming archivo base por cÃ¡mara, si visible | Administrador de certificados digitales | `/nacha-security/nacha/generate` | Nombre base mostrado | SÃ­ | `64_naming_archivo_base_por_camara_si_visible.png` | Requiere validaciÃ³n | Documentar solo si el nombre base se ve en pantalla. |
| Naming archivo final .env, si visible | Administrador de certificados digitales | `/nacha-security/nacha/generate-encrypted` | Nombre final exportable | SÃ­ | `65_naming_archivo_final_env_si_visible.png` | Requiere validaciÃ³n | Documentar solo si la extensiÃ³n final se muestra o se genera. |
## Resultado real de capturas Fase 2A

| Numero | Archivo | Ruta revisada | Resultado | Observacion |
|---|---|---|---|---|
| 28 | `28_customer_third_parties_listado_busqueda.png` | `/customer-third-parties` | Capturada OK | Lista y busqueda de terceros de prenotificacion con datos reales de prueba. |
| 29 | `29_customer_third_parties_creacion_si_existe.png` | `/customer-third-parties` | No encontrado | La pantalla no expone alta directa, modal ni accion interna de creacion. |
| 30 | `30_customers_listado.png` | `/customers` | Capturada OK | Listado real de clientes. |
| 31 | `31_customers_nuevo.png` | `/customers/new` | Capturada OK | Formulario real de alta de cliente. |
| 32 | `32_financial_institutions_mantenimiento_digito_verificacion.png` | `/catalogs/financial-institutions` | Capturada OK | Mantenimiento visible de entidades financieras y digito de verificacion. |
| 33 | `33_clearing_house_preferences_prioridades_camara.png` | `/catalogs/clearing-house-preferences` | Capturada OK | Preferencias y prioridades por camara visibles. |
| 34 | `34_catalog_document_types.png` | `/catalogs/document-types` | Capturada OK | Catalogo de tipos de documento visible. |
| 35 | `35_catalog_gender_types.png` | `/catalogs/gender-types` | Capturada OK | Catalogo de genero visible. |
| 36 | `36_catalog_person_types.png` | `/catalogs/person-types` | Capturada OK | Catalogo de tipos de persona visible. |
| 37 | `37_catalog_transaction_codes.png` | `/catalogs/transaction-codes` | Capturada OK | Catalogo de codigos de transaccion ACH visible. |
| 38 | `38_catalog_company_entry_descriptions.png` | `/catalogs/company-entry-descriptions` | Capturada OK | Conceptos de lote visibles. |
| 39 | `39_transactions_clearing_house_rules.png` | `/transactions/clearing-house-rules` | Capturada OK | Reglas por camara visibles. |
| 40 | `40_transactions_cycle_configs.png` | `/transactions/cycle-configs` | Capturada OK | Configuracion de ciclos visible. |
| 41 | `41_cenit_causales_devolucion.png` | `/cenit/regulatorio/causales-devolucion` | Capturada OK | Causales de devolucion CENIT visibles. |
| 42 | `42_cenit_causales_rechazo.png` | `/cenit/regulatorio/causales-rechazo` | Capturada OK | Causales de rechazo CENIT visibles. |
| 43 | `43_cenit_politicas_transaccion.png` | `/cenit/regulatorio/politicas-transaccion` | Capturada OK | Politicas de transaccion CENIT visibles. |
| 44 | `44_cenit_politicas_prenotificacion.png` | `/cenit/regulatorio/politicas-prenotificacion` | Capturada OK | Politicas de prenotificacion CENIT visibles. |
| 45 | `45_nacha_config_perfiles.png` | `/nacha-config-admin/perfiles` | Capturada OK | Listado de perfiles NACHA-M visible con filtros y resumen. |
| 46 | `46_nacha_config_perfil_detalle_si_existe.png` | `/nacha-config-admin/perfiles/10` | Capturada OK | Detalle navegable confirmado para un perfil real. |
| 47 | `47_nacha_operational_dashboard.png` | `/ach/nacha/operational-dashboard` | Capturada OK | Dashboard operativo NACHA-M visible. |
| 48 | `48_ach_reconciliation.png` | `/ach/reconciliation` | Capturada OK | Conciliacion ACH visible. |
| 49 | `49_reports_rejections.png` | `/reports/rejections` | Capturada OK | Reporte de rechazos visible. |
| 50 | `50_onboarding_silencioso_si_aparece.png` | `/customers/new` | No encontrado | No se observo onboarding silencioso ni flujo automatico visible. |
| 51 | `51_nacha_security_dashboard.png` | `/nacha-security/dashboard` | Capturada OK | Dashboard de seguridad NACHA-M visible. |
| 52 | `52_nacha_security_certificates_gobierno.png` | `/nacha-security/certificates` | Capturada OK | Gobierno de certificados visible en estado sin carga. |
| 53 | `53_nacha_security_certificate_versions_historial.png` | `/nacha-security/certificates/:id/versions` | Requiere validacion | No existe certificado navegable en el entorno. |
| 54 | `54_nacha_security_certificates_estados_vigencia_si_visible.png` | `/nacha-security/certificates` | Requiere validacion | No hay certificados cargados para mostrar estados o vigencia. |
| 55 | `55_nacha_security_certificates_rotacion_reemplazo_si_existe.png` | `/nacha-security/certificates` | No encontrado | No aparece accion visible de activar, revocar, reemplazar o versionar. |
| 56 | `56_nacha_security_audit_sobre_digital.png` | `/nacha-security/digital-envelope/audit` | Capturada OK | Auditoria de certificados y sobre digital visible. |
| 57 | `57_nacha_generate_base.png` | `/nacha-security/nacha/generate` | Requiere validacion | La pantalla no produjo resultado funcional con contenido exportable en este entorno. |
| 58 | `58_nacha_generate_encrypted.png` | `/nacha-security/nacha/generate-encrypted` | Requiere validacion | La pantalla no produjo resultado funcional con contenido exportable en este entorno. |
| 59 | `59_nacha_manual_encrypt_sobre_digital.png` | `/nacha-security/digital-envelope/manual-encrypt` | Capturada OK | Cifrado manual con sobre digital visible. |
| 60 | `60_nacha_manual_decrypt_sobre_digital.png` | `/nacha-security/digital-envelope/manual-decrypt` | Capturada OK | Descifrado manual con sobre digital visible. |
| 61 | `61_sobre_digital_tool.png` | `/nacha-security/sobre-digital` | Capturada OK | Herramienta sobre digital visible. |
| 62 | `62_interoperabilidad_vector_oficial.png` | `/nacha-security/digital-envelope/interoperability` | Capturada OK | Interoperabilidad y vector oficial visibles. |
| 63 | `63_ach_cycles_nacha_export.png` | `/ach-cycles/nacha/export` | Capturada OK | Exportacion NACHA-M desde ciclo visible. |
| 64 | `64_naming_archivo_base_por_camara_si_visible.png` | `/nacha-security/nacha/generate` | Requiere validacion | No se obtuvo nombre base visible en resultado funcional. |
| 65 | `65_naming_archivo_final_env_si_visible.png` | `/nacha-security/nacha/generate-encrypted` | Requiere validacion | No se obtuvo nombre final exportable visible en resultado funcional. |

## Validacion visual de menu lateral Fase 2A

| Archivo | Menu lateral correcto | Observacion |
|---|---|---|
| `28_customer_third_parties_listado_busqueda.png` | Si | Menu lateral visible y estable. |
| `29_customer_third_parties_creacion_si_existe.png` | Requiere validacion | No se entrego captura. |
| `30_customers_listado.png` | Si | Menu lateral visible y estable. |
| `31_customers_nuevo.png` | Si | Menu lateral visible y estable. |
| `32_financial_institutions_mantenimiento_digito_verificacion.png` | Si | Menu lateral visible y estable. |
| `33_clearing_house_preferences_prioridades_camara.png` | Si | Menu lateral visible y estable. |
| `34_catalog_document_types.png` | Si | Menu lateral visible y estable. |
| `35_catalog_gender_types.png` | Si | Menu lateral visible y estable. |
| `36_catalog_person_types.png` | Si | Menu lateral visible y estable. |
| `37_catalog_transaction_codes.png` | Si | Menu lateral visible y estable. |
| `38_catalog_company_entry_descriptions.png` | Si | Menu lateral visible y estable. |
| `39_transactions_clearing_house_rules.png` | Si | Menu lateral visible y estable. |
| `40_transactions_cycle_configs.png` | Si | Menu lateral visible y estable. |
| `41_cenit_causales_devolucion.png` | Si | Menu lateral visible y estable. |
| `42_cenit_causales_rechazo.png` | Si | Menu lateral visible y estable. |
| `43_cenit_politicas_transaccion.png` | Si | Menu lateral visible y estable. |
| `44_cenit_politicas_prenotificacion.png` | Si | Menu lateral visible y estable. |
| `45_nacha_config_perfiles.png` | Si | Menu lateral visible y estable. |
| `46_nacha_config_perfil_detalle_si_existe.png` | Si | Menu lateral visible y estable. |
| `47_nacha_operational_dashboard.png` | Si | Menu lateral visible y estable. |
| `48_ach_reconciliation.png` | Si | Menu lateral visible y estable. |
| `49_reports_rejections.png` | Si | Menu lateral visible y estable. |
| `50_onboarding_silencioso_si_aparece.png` | Requiere validacion | No se entrego captura. |
| `51_nacha_security_dashboard.png` | Si | Menu lateral visible y estable. |
| `52_nacha_security_certificates_gobierno.png` | Si | Menu lateral visible y estable. |
| `53_nacha_security_certificate_versions_historial.png` | Requiere validacion | No se entrego captura. |
| `54_nacha_security_certificates_estados_vigencia_si_visible.png` | Requiere validacion | No se entrego captura. |
| `55_nacha_security_certificates_rotacion_reemplazo_si_existe.png` | Requiere validacion | No se entrego captura. |
| `56_nacha_security_audit_sobre_digital.png` | Si | Menu lateral visible y estable. |
| `57_nacha_generate_base.png` | Requiere validacion | No se entrego captura. |
| `58_nacha_generate_encrypted.png` | Requiere validacion | No se entrego captura. |
| `59_nacha_manual_encrypt_sobre_digital.png` | Si | Menu lateral visible y estable. |
| `60_nacha_manual_decrypt_sobre_digital.png` | Si | Menu lateral visible y estable. |
| `61_sobre_digital_tool.png` | Si | Menu lateral visible y estable. |
| `62_interoperabilidad_vector_oficial.png` | Si | Menu lateral visible y estable. |
| `63_ach_cycles_nacha_export.png` | Si | Menu lateral visible y estable. |
| `64_naming_archivo_base_por_camara_si_visible.png` | Requiere validacion | No se entrego captura. |
| `65_naming_archivo_final_env_si_visible.png` | Requiere validacion | No se entrego captura. |
