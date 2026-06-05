# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: spa-routes-smoke.spec.ts >> SPA route smoke >> Route_ShouldRender_transactions-create
- Location: e2e/spa-routes-smoke.spec.ts:138:9

# Error details

```
Error: expect(received).toEqual(expected) // deep equality

- Expected  - 1
+ Received  + 3

- Array []
+ Array [
+   "ERROR Et",
+ ]
```

# Page snapshot

```yaml
- generic [ref=e4]:
  - navigation "Menú principal" [ref=e5]:
    - generic [ref=e6]:
      - generic [ref=e7]: ACH
      - generic [ref=e8]:
        - paragraph [ref=e9]: ACH Interbank
        - generic [ref=e10]: Portal backoffice
    - navigation [ref=e11]:
      - link "Panel principal" [ref=e14] [cursor=pointer]:
        - /url: /dashboard
        - generic [ref=e15]: Panel principal
      - generic [ref=e16]:
        - generic [ref=e17]:
          - link "Usuarios" [ref=e18] [cursor=pointer]:
            - /url: /users
            - generic [ref=e19]: Usuarios
          - button "Alternar submenú de Usuarios" [ref=e20] [cursor=pointer]:
            - generic [ref=e21]: expand_more
        - group "Submenú de Usuarios":
          - link "Identidad y colores":
            - /url: /users/branding
            - generic: Identidad y colores
          - link "Reglas de contraseña":
            - /url: /users/password-rules
            - generic: Reglas de contraseña
          - link "Bloqueo de acceso":
            - /url: /users/login-lockout
            - generic: Bloqueo de acceso
      - generic [ref=e22]:
        - generic [ref=e23]:
          - link "Integraciones" [ref=e24] [cursor=pointer]:
            - /url: /integraciones
            - generic [ref=e25]: Integraciones
          - button "Alternar submenú de Integraciones" [ref=e26] [cursor=pointer]:
            - generic [ref=e27]: expand_more
        - group "Submenú de Integraciones":
          - link "Integraciones SOAP":
            - /url: /soap-integrations
            - generic: Integraciones SOAP
      - generic [ref=e28]:
        - generic [ref=e29]:
          - link "CENIT" [ref=e30] [cursor=pointer]:
            - /url: /cenit
            - generic [ref=e31]: CENIT
          - button "Alternar submenú de CENIT" [ref=e32] [cursor=pointer]:
            - generic [ref=e33]: expand_more
        - group "Submenú de CENIT":
          - 'link "Regulatorio: Devoluciones"':
            - /url: /cenit/regulatorio/causales-devolucion
            - generic: "Regulatorio: Devoluciones"
          - 'link "Regulatorio: Rechazos"':
            - /url: /cenit/regulatorio/causales-rechazo
            - generic: "Regulatorio: Rechazos"
          - 'link "Regulatorio: Políticas"':
            - /url: /cenit/regulatorio/politicas-transaccion
            - generic: "Regulatorio: Políticas"
          - 'link "Operación: Ciclos"':
            - /url: /cenit/operacion/ciclos
            - generic: "Operación: Ciclos"
          - 'link "Operación: Cola"':
            - /url: /cenit/operacion/cola
            - generic: "Operación: Cola"
          - 'link "Operación: Neteo"':
            - /url: /cenit/operacion/neteo
            - generic: "Operación: Neteo"
          - 'link "Operación: Optimización"':
            - /url: /cenit/operacion/optimizacion
            - generic: "Operación: Optimización"
          - 'link "Operación: Devoluciones"':
            - /url: /cenit/operacion/devoluciones
            - generic: "Operación: Devoluciones"
          - 'link "Operación: Trazabilidad"':
            - /url: /cenit/operacion/trazabilidad
            - generic: "Operación: Trazabilidad"
      - generic [ref=e34]:
        - generic [ref=e35]:
          - link "Configuración NACHA-M" [ref=e36] [cursor=pointer]:
            - /url: /nacha-config-admin/perfiles
            - generic [ref=e37]: Configuración NACHA-M
          - button "Alternar submenú de Configuración NACHA-M" [ref=e38] [cursor=pointer]:
            - generic [ref=e39]: expand_more
        - group "Submenú de Configuración NACHA-M":
          - link "Perfiles oficiales":
            - /url: /nacha-config-admin/perfiles
            - generic: Perfiles oficiales
          - link "Registros oficiales":
            - /url: /nacha-config-admin/records
            - generic: Registros oficiales
          - link "Variantes y campos":
            - /url: /nacha-config-admin/variants-fields
            - generic: Variantes y campos
      - generic [ref=e40]:
        - generic [ref=e41]:
          - link "Catálogos" [ref=e42] [cursor=pointer]:
            - /url: /catalogs
            - generic [ref=e43]: Catálogos
          - button "Alternar submenú de Catálogos" [ref=e44] [cursor=pointer]:
            - generic [ref=e45]: expand_more
        - group "Submenú de Catálogos":
          - link "Conceptos de lote":
            - /url: /catalogs/company-entry-descriptions
            - generic: list
            - generic: Conceptos de lote
          - link "Tipos de documento":
            - /url: /catalogs/document-types
            - generic: badge
            - generic: Tipos de documento
          - link "Tipos de género":
            - /url: /catalogs/gender-types
            - generic: diversity_3
            - generic: Tipos de género
          - link "Tipos de persona":
            - /url: /catalogs/person-types
            - generic: apartment
            - generic: Tipos de persona
          - link "Tipos de teléfono":
            - /url: /catalogs/phone-types
            - generic: call
            - generic: Tipos de teléfono
          - link "Tipos de correo":
            - /url: /catalogs/email-types
            - generic: mail
            - generic: Tipos de correo
          - link "Tipos de dirección":
            - /url: /catalogs/address-types
            - generic: location_on
            - generic: Tipos de dirección
          - link "Códigos de transacción ACH":
            - /url: /catalogs/transaction-codes
            - generic: numbers
            - generic: Códigos de transacción ACH
      - generic [ref=e46]:
        - generic [ref=e47]:
          - link "Transacciones" [ref=e48] [cursor=pointer]:
            - /url: /transactions
            - generic [ref=e49]: Transacciones
          - button "Alternar submenú de Transacciones" [expanded] [ref=e50] [cursor=pointer]:
            - generic [ref=e51]: expand_more
        - group "Submenú de Transacciones" [ref=e52]:
          - link "Listado" [ref=e53] [cursor=pointer]:
            - /url: /transactions/list
            - generic [ref=e54]: Listado
          - link "Crear transacción" [ref=e55] [cursor=pointer]:
            - /url: /transactions/create
            - generic [ref=e56]: Crear transacción
          - link "Carga masiva" [ref=e57] [cursor=pointer]:
            - /url: /transactions/bulk-create
            - generic [ref=e58]: Carga masiva
          - link "Carga masiva por archivo" [ref=e59] [cursor=pointer]:
            - /url: /transactions/bulk-ingestion/upload
            - generic [ref=e60]: Carga masiva por archivo
          - link "Seguimiento lotes" [ref=e61] [cursor=pointer]:
            - /url: /transactions/bulk-ingestion/tracking
            - generic [ref=e62]: Seguimiento lotes
          - link "Config. ciclos" [ref=e63] [cursor=pointer]:
            - /url: /transactions/cycle-configs
            - generic [ref=e64]: Config. ciclos
          - link "Reglas por cámara" [ref=e65] [cursor=pointer]:
            - /url: /transactions/clearing-house-rules
            - generic [ref=e66]: Reglas por cámara
          - link "Cargar NACHA-M" [ref=e67] [cursor=pointer]:
            - /url: /transactions/nacha-upload
            - generic [ref=e68]: Cargar NACHA-M
          - link "Devoluciones ACH" [ref=e69] [cursor=pointer]:
            - /url: /transactions/returns
            - generic [ref=e70]: Devoluciones ACH
      - generic [ref=e71]:
        - generic [ref=e72]:
          - link "Navegación" [ref=e73] [cursor=pointer]:
            - /url: /navigation
            - generic [ref=e74]: Navegación
          - button "Alternar submenú de Navegación" [ref=e75] [cursor=pointer]:
            - generic [ref=e76]: expand_more
        - group "Submenú de Navegación":
          - link "Menús":
            - /url: /navigation/menu-items
            - generic: Menús
      - generic [ref=e77]:
        - generic [ref=e78]:
          - link "Seguridad NACHA" [ref=e79] [cursor=pointer]:
            - /url: /nacha-security/dashboard
            - generic [ref=e80]: Seguridad NACHA
          - button "Alternar submenú de Seguridad NACHA" [ref=e81] [cursor=pointer]:
            - generic [ref=e82]: expand_more
        - group "Submenú de Seguridad NACHA":
          - link "Dashboard seguridad":
            - /url: /nacha-security/dashboard
            - generic: Dashboard seguridad
          - link "Certificados":
            - /url: /nacha-security/certificates
            - generic: Certificados
          - link "Sobre digital":
            - /url: /nacha-security/sobre-digital
            - generic: Sobre digital
          - link "Generar NACHA-M":
            - /url: /nacha-security/nacha/generate
            - generic: description
            - generic: Generar NACHA-M
          - link "Generar NACHA-M cifrado":
            - /url: /nacha-security/nacha/generate-encrypted
            - generic: encrypted
            - generic: Generar NACHA-M cifrado
          - link "Cifrado manual":
            - /url: /nacha-security/digital-envelope/manual-encrypt
            - generic: lock
            - generic: Cifrado manual
          - link "Descifrado manual":
            - /url: /nacha-security/digital-envelope/manual-decrypt
            - generic: lock_open
            - generic: Descifrado manual
          - link "Auditoría operaciones":
            - /url: /nacha-security/digital-envelope/audit
            - generic: fact_check
            - generic: Auditoría operaciones
          - link "Interoperabilidad":
            - /url: /nacha-security/digital-envelope/interoperability
            - generic: hub
            - generic: Interoperabilidad
      - generic [ref=e83]:
        - generic [ref=e84]:
          - link "Programador" [ref=e85] [cursor=pointer]:
            - /url: /scheduler
            - generic [ref=e86]: Programador
          - button "Alternar submenú de Programador" [ref=e87] [cursor=pointer]:
            - generic [ref=e88]: expand_more
        - group "Submenú de Programador":
          - link "Tareas programadas":
            - /url: /scheduler/tasks
            - generic: Tareas programadas
      - generic [ref=e89]:
        - generic [ref=e90]:
          - link "Logs" [ref=e91] [cursor=pointer]:
            - /url: /audit-logs
            - generic [ref=e92]: Logs
          - button "Alternar submenú de Logs" [ref=e93] [cursor=pointer]:
            - generic [ref=e94]: expand_more
        - group "Submenú de Logs":
          - link "Logs de auditoría":
            - /url: /audit-logs
            - generic: Logs de auditoría
          - link "Logs de autenticaciones":
            - /url: /auth-logs
            - generic: Logs de autenticaciones
          - link "Logs de navegación":
            - /url: /navigation-logs
            - generic: Logs de navegación
      - generic [ref=e95]:
        - generic [ref=e96]:
          - link "UAT / Simuladores" [ref=e97] [cursor=pointer]:
            - /url: /uat
            - generic [ref=e98]: UAT / Simuladores
          - button "Alternar submenú de UAT / Simuladores" [ref=e99] [cursor=pointer]:
            - generic [ref=e100]: expand_more
        - group "Submenú de UAT / Simuladores":
          - link "Simulador NACHA-M Entrada":
            - /url: /uat/nacha-inbound-simulator
            - generic: Simulador NACHA-M Entrada
      - generic [ref=e101]:
        - generic [ref=e102]:
          - link "Respuestas ACH" [ref=e103] [cursor=pointer]:
            - /url: /ach-responses
            - generic [ref=e104]: Respuestas ACH
          - button "Alternar submenú de Respuestas ACH" [ref=e105] [cursor=pointer]:
            - generic [ref=e106]: expand_more
        - group "Submenú de Respuestas ACH":
          - link "Bandeja":
            - /url: /ach-responses
            - generic: assignment
            - generic: Bandeja
          - link "Revisión manual":
            - /url: /ach-responses/manual-review
            - generic: rule
            - generic: Revisión manual
          - link "Homologaciones":
            - /url: /ach-responses/status-mappings
            - generic: sync_alt
            - generic: Homologaciones
          - link "Dashboard operativo":
            - /url: /ach-responses/dashboard
            - generic: dashboard
            - generic: Dashboard operativo
      - link "Reportes" [ref=e109] [cursor=pointer]:
        - /url: /reports
        - generic [ref=e110]: Reportes
      - link "Command Center inbound NACHA" [ref=e113] [cursor=pointer]:
        - /url: /incoming-nacha-command-center
        - generic [ref=e114]: Command Center inbound NACHA
      - link "Ciclos ACH" [ref=e117] [cursor=pointer]:
        - /url: /ach-cycles
        - generic [ref=e118]: Ciclos ACH
      - link "Capability Registry" [ref=e121] [cursor=pointer]:
        - /url: /payment-rail-capability-registry
        - generic [ref=e122]: Capability Registry
      - link "Clientes" [ref=e125] [cursor=pointer]:
        - /url: /customers
        - generic [ref=e126]: Clientes
      - link "Alias" [ref=e129] [cursor=pointer]:
        - /url: /aliases
        - generic [ref=e130]: Alias
      - link "Dashboard operativo" [ref=e133] [cursor=pointer]:
        - /url: /ach
        - generic [ref=e134]: Dashboard operativo
      - generic [ref=e135]:
        - generic [ref=e136]:
          - link "SOAP UAT Console" [ref=e137] [cursor=pointer]:
            - /url: /ach/nacha/soap-uat-console
            - generic [ref=e138]: fact_check
            - generic [ref=e139]: SOAP UAT Console
          - button "Alternar submenú de SOAP UAT Console" [ref=e140] [cursor=pointer]:
            - generic [ref=e141]: expand_more
        - group "Submenú de SOAP UAT Console":
          - link "SOAP UAT Console":
            - /url: /ach/nacha/soap-uat-console
            - generic: fact_check
            - generic: SOAP UAT Console
      - generic [ref=e142]:
        - generic [ref=e143]:
          - link "Conciliacion ACH" [ref=e144] [cursor=pointer]:
            - /url: /ach/reconciliation
            - generic [ref=e145]: fact_check
            - generic [ref=e146]: Conciliacion ACH
          - button "Alternar submenú de Conciliacion ACH" [ref=e147] [cursor=pointer]:
            - generic [ref=e148]: expand_more
        - group "Submenú de Conciliacion ACH":
          - link "Conciliacion ACH":
            - /url: /ach/reconciliation
            - generic: fact_check
            - generic: Conciliacion ACH
      - generic [ref=e149]:
        - generic [ref=e150]:
          - link "Catálogos" [ref=e151] [cursor=pointer]:
            - /url: /catalogs
            - generic [ref=e152]: list_alt
            - generic [ref=e153]: Catálogos
          - button "Alternar submenú de Catálogos" [ref=e154] [cursor=pointer]:
            - generic [ref=e155]: expand_more
        - group "Submenú de Catálogos":
          - link "Conceptos de lote":
            - /url: /catalogs/company-entry-descriptions
            - generic: list
            - generic: Conceptos de lote
          - link "Tipos de documento":
            - /url: /catalogs/document-types
            - generic: badge
            - generic: Tipos de documento
          - link "Tipos de género":
            - /url: /catalogs/gender-types
            - generic: diversity_3
            - generic: Tipos de género
          - link "Tipos de persona":
            - /url: /catalogs/person-types
            - generic: apartment
            - generic: Tipos de persona
          - link "Tipos de teléfono":
            - /url: /catalogs/phone-types
            - generic: call
            - generic: Tipos de teléfono
          - link "Tipos de correo":
            - /url: /catalogs/email-types
            - generic: mail
            - generic: Tipos de correo
          - link "Tipos de dirección":
            - /url: /catalogs/address-types
            - generic: location_on
            - generic: Tipos de dirección
          - link "Códigos de transacción ACH":
            - /url: /catalogs/transaction-codes
            - generic: numbers
            - generic: Códigos de transacción ACH
    - generic [ref=e156]:
      - paragraph [ref=e157]: Perfil
      - generic [ref=e158]:
        - generic [ref=e159]: U
        - generic [ref=e160]:
          - generic [ref=e161]: Usuario SPA Smoke
          - generic [ref=e162]: Admin, ACH.Operator
      - button "Cerrar sesión" [ref=e163] [cursor=pointer]
  - generic [ref=e164]:
    - banner [ref=e165]:
      - generic [ref=e167]:
        - heading "Crear transacción" [level=1] [ref=e168]
        - navigation "Breadcrumbs" [ref=e169]:
          - link "Transacciones" [ref=e170] [cursor=pointer]:
            - /url: /transactions
          - generic [ref=e171]: /
          - generic [ref=e172]: Crear transacción
      - generic [ref=e173]:
        - generic [ref=e174]:
          - generic [ref=e175]: Usuario SPA Smoke
          - generic [ref=e176]: Admin, ACH.Operator
        - button "Salir" [ref=e177] [cursor=pointer]
    - main [ref=e178]:
      - region "Crear transacción ACH" [ref=e180]:
        - generic [ref=e181]:
          - heading "Crear transacción ACH" [level=2] [ref=e182]
          - paragraph [ref=e183]: Complete los datos de la transacción
        - generic [ref=e184]:
          - generic [ref=e185]:
            - group "Datos de la transacción" [ref=e186]:
              - generic [ref=e187]: Datos de la transacción
              - generic [ref=e188]:
                - generic [ref=e189]:
                  - generic [ref=e190]: Monto *
                  - 'textbox "Monto * Ingrese el monto en pesos colombianos. Valor a enviar: $ 0" [ref=e191]':
                    - /placeholder: 0,00
                  - generic [ref=e192]: Ingrese el monto en pesos colombianos.
                  - generic [ref=e193]: "Valor a enviar: $ 0"
                - generic [ref=e194]:
                  - generic [ref=e195]: ID operación cliente *
                  - textbox "ID operación cliente * Identificador operativo/idempotencia. Debe ser único por instrucción." [ref=e196]
                  - generic [ref=e197]: Identificador operativo/idempotencia. Debe ser único por instrucción.
                - generic [ref=e198]:
                  - generic [ref=e199]: Referencia legado (opcional)
                  - textbox "Referencia legado (opcional) Solo para coexistencia histórica. El contexto funcional viaja en addenda." [ref=e200]
                  - generic [ref=e201]: Solo para coexistencia histórica. El contexto funcional viaja en addenda.
                - generic [ref=e202]:
                  - generic [ref=e203]: Tipo *
                  - generic [ref=e205]:
                    - generic [ref=e206]:
                      - textbox "Tipo * Limpiar Crédito Débito Reverso" [ref=e207]:
                        - /placeholder: Buscar tipo
                      - button "Limpiar" [ref=e208] [cursor=pointer]
                    - generic [ref=e209]:
                      - button "Crédito" [ref=e210] [cursor=pointer]:
                        - generic [ref=e211]: Crédito
                      - button "Débito" [ref=e212] [cursor=pointer]:
                        - generic [ref=e213]: Débito
                      - button "Reverso" [ref=e214] [cursor=pointer]:
                        - generic [ref=e215]: Reverso
                - generic [ref=e216]:
                  - generic [ref=e217]: Tipo de cuenta *
                  - generic [ref=e219]:
                    - generic [ref=e220]:
                      - textbox "Tipo de cuenta * Limpiar Cuenta corriente Cuenta de ahorros Depósitos electrónicos" [ref=e221]:
                        - /placeholder: Buscar tipo de cuenta
                      - button "Limpiar" [ref=e222] [cursor=pointer]
                    - generic [ref=e223]:
                      - button "Cuenta corriente" [ref=e224] [cursor=pointer]:
                        - generic [ref=e225]: Cuenta corriente
                      - button "Cuenta de ahorros" [ref=e226] [cursor=pointer]:
                        - generic [ref=e227]: Cuenta de ahorros
                      - button "Depósitos electrónicos" [ref=e228] [cursor=pointer]:
                        - generic [ref=e229]: Depósitos electrónicos
                - generic [ref=e230]:
                  - generic [ref=e231]: Prenotificación
                  - checkbox "Prenotificación Se enviará con monto cero." [ref=e232]
                  - generic [ref=e233]: Se enviará con monto cero.
            - group "Datos del originador" [ref=e234]:
              - generic [ref=e235]: Datos del originador
              - generic [ref=e236]:
                - generic [ref=e237]:
                  - generic [ref=e238]: Cliente originador (opcional)
                  - generic [ref=e240]:
                    - generic [ref=e241]:
                      - textbox "Cliente originador (opcional) Limpiar Sin resultados para la búsqueda. Seleccionarlo autocompleta identificación, nombre y cuenta origen. Si no existe, crea la transacción y el cliente se registrará silenciosamente." [ref=e242]:
                        - /placeholder: Buscar cliente originador
                      - button "Limpiar" [disabled] [ref=e243]
                    - paragraph [ref=e245]: Sin resultados para la búsqueda.
                  - generic [ref=e246]: Seleccionarlo autocompleta identificación, nombre y cuenta origen.
                  - generic [ref=e247]: Si no existe, crea la transacción y el cliente se registrará silenciosamente.
                - generic [ref=e248]:
                  - generic [ref=e249]: Cuenta origen *
                  - textbox "Cuenta origen * Digite la cuenta origen (6 a 18 dígitos)." [ref=e250]
                  - generic [ref=e251]: Digite la cuenta origen (6 a 18 dígitos).
                - generic [ref=e252]:
                  - generic [ref=e253]: Tipo persona originador *
                  - generic [ref=e255]:
                    - generic [ref=e256]:
                      - textbox "Tipo persona originador * Limpiar Persona jurídica (PJ) Persona natural (PN)" [ref=e257]:
                        - /placeholder: Buscar tipo de persona
                      - button "Limpiar" [ref=e258] [cursor=pointer]
                    - generic [ref=e259]:
                      - button "Persona jurídica (PJ)" [ref=e260] [cursor=pointer]:
                        - generic [ref=e261]: Persona jurídica (PJ)
                      - button "Persona natural (PN)" [ref=e262] [cursor=pointer]:
                        - generic [ref=e263]: Persona natural (PN)
                - generic [ref=e264]:
                  - generic [ref=e265]: Identificación usuario originador *
                  - textbox "Identificación usuario originador * Este dato se usa para alta silenciosa/actualización del cliente originador." [ref=e266]
                  - generic [ref=e267]: Este dato se usa para alta silenciosa/actualización del cliente originador.
                - generic [ref=e268]:
                  - generic [ref=e269]: Nombre usuario originador *
                  - textbox "Nombre usuario originador * Si seleccionas cliente se autocompleta, pero puedes ajustarlo." [ref=e270]
                  - generic [ref=e271]: Si seleccionas cliente se autocompleta, pero puedes ajustarlo.
            - group "Datos del receptor" [ref=e272]:
              - generic [ref=e273]: Datos del receptor
              - generic [ref=e274]:
                - generic [ref=e275]:
                  - generic [ref=e276]: Institución destino *
                  - generic [ref=e278]:
                    - generic [ref=e279]:
                      - textbox "Institución destino * Limpiar Sin resultados para la búsqueda. Seleccione una institución habilitada." [ref=e280]:
                        - /placeholder: Buscar institución
                      - button "Limpiar" [disabled] [ref=e281]
                    - paragraph [ref=e283]: Sin resultados para la búsqueda.
                  - generic [ref=e284]: Seleccione una institución habilitada.
                - generic [ref=e285]:
                  - generic [ref=e286]: Cuenta destino *
                  - generic [ref=e288]:
                    - generic [ref=e289]:
                      - textbox "Cuenta destino * Limpiar Cargando opciones... Solo se muestran cuentas con prenotificación aprobada (Activa). Primero selecciona/digita la cuenta origen para cargar las cuentas destino permitidas. No hay cuentas activas para la institución seleccionada. Para registrar receptores usa Terceros." [ref=e290]:
                        - /placeholder: Buscar cuenta destino
                      - button "Limpiar" [ref=e291] [cursor=pointer]
                    - paragraph [ref=e292]: Cargando opciones...
                  - generic [ref=e293]: Solo se muestran cuentas con prenotificación aprobada (Activa).
                  - generic [ref=e294]: Primero selecciona/digita la cuenta origen para cargar las cuentas destino permitidas.
                  - generic [ref=e295]: No hay cuentas activas para la institución seleccionada.
                  - generic [ref=e296]: Para registrar receptores usa Terceros.
                - generic [ref=e297]:
                  - generic [ref=e298]: Tipo persona receptor *
                  - generic [ref=e300]:
                    - generic [ref=e301]:
                      - textbox "Tipo persona receptor * Limpiar Persona natural (PN) Persona jurídica (PJ)" [ref=e302]:
                        - /placeholder: Buscar tipo de persona
                      - button "Limpiar" [ref=e303] [cursor=pointer]
                    - generic [ref=e304]:
                      - button "Persona natural (PN)" [ref=e305] [cursor=pointer]:
                        - generic [ref=e306]: Persona natural (PN)
                      - button "Persona jurídica (PJ)" [ref=e307] [cursor=pointer]:
                        - generic [ref=e308]: Persona jurídica (PJ)
                - generic [ref=e309]:
                  - generic [ref=e310]: Identificación del receptor
                  - textbox "Identificación del receptor Obligatoria si se solicita validación de identidad." [ref=e311]
                  - generic [ref=e312]: Obligatoria si se solicita validación de identidad.
                - generic [ref=e313]:
                  - generic [ref=e314]: Nombre del receptor
                  - textbox "Nombre del receptor Se usa para crear/actualizar el cliente receptor en alta silenciosa." [ref=e315]
                  - generic [ref=e316]: Se usa para crear/actualizar el cliente receptor en alta silenciosa.
                - generic [ref=e317]:
                  - generic [ref=e318]: Validar identidad
                  - checkbox "Validar identidad Marca V para validar identidad." [ref=e319]
                  - generic [ref=e320]: Marca V para validar identidad.
            - generic [ref=e321]:
              - generic [ref=e322]: Descripción de la entrada *
              - generic [ref=e324]:
                - generic [ref=e325]:
                  - textbox "Descripción de la entrada * Limpiar Sin resultados para la búsqueda. Seleccione un concepto del catálogo." [ref=e326]:
                    - /placeholder: Buscar concepto
                  - button "Limpiar" [disabled] [ref=e327]
                - paragraph [ref=e329]: Sin resultados para la búsqueda.
              - generic [ref=e330]: Seleccione un concepto del catálogo.
          - generic [ref=e331]:
            - generic [ref=e332]:
              - heading "Adendas" [level=3] [ref=e333]
              - button "Agregar addenda" [ref=e334] [cursor=pointer]
            - generic [ref=e335]:
              - generic [ref=e336]:
                - text: Código tipo registro adenda *
                - generic [ref=e338]:
                  - generic [ref=e339]:
                    - textbox "Código tipo registro adenda * Limpiar 05 - Información adicional" [ref=e340]:
                      - /placeholder: Buscar tipo de adenda
                    - button "Limpiar" [ref=e341] [cursor=pointer]
                  - button "05 - Información adicional" [ref=e343] [cursor=pointer]:
                    - generic [ref=e344]: 05 - Información adicional
              - generic [ref=e345]:
                - text: Información *
                - textbox "Información *" [ref=e346]
              - button "Eliminar addenda" [ref=e347] [cursor=pointer]: Eliminar
          - generic [ref=e348]:
            - button "Registrar transacción" [disabled] [ref=e349]
            - button "Cancelar" [ref=e350] [cursor=pointer]
```

# Test source

```ts
  86  |   { id: 'transactions-nacha-upload', path: '/transactions/nacha-upload', title: /.+/ },
  87  |   { id: 'customer-third-parties', path: '/customer-third-parties', title: /.+/ },
  88  |   { id: 'transactions-returns', path: '/transactions/returns', title: /.+/ },
  89  |   { id: 'transactions-clearing-house-rules', path: '/transactions/clearing-house-rules', title: /.+/ },
  90  |   { id: 'ach-root', path: '/ach', title: /.+/ },
  91  |   { id: 'uat-root', path: '/uat', title: /.+/ },
  92  |   { id: 'uat-nacha-inbound-simulator', path: '/uat/nacha-inbound-simulator', title: /.+/ },
  93  |   { id: 'nacha-security-root', path: '/nacha-security', title: /.+/ },
  94  |   { id: 'nacha-security-dashboard', path: '/nacha-security/dashboard', title: /.+/ },
  95  |   { id: 'nacha-security-certificates', path: '/nacha-security/certificates', title: /.+/ },
  96  |   { id: 'nacha-security-sobre-digital', path: '/nacha-security/sobre-digital', title: /.+/ }
  97  | ];
  98  | 
  99  | const omittedRoutes = [
  100 |   { path: '/ach-cycles/nacha/layouts', reason: 'Legacy route controlled by not-found only.' },
  101 |   { path: '/ach-cycles/nacha/definitions', reason: 'Legacy route controlled by not-found only.' },
  102 |   { path: '/nacha-layouts', reason: 'Legacy API route controlled by not-found only.' },
  103 |   { path: '/nacha-record-definitions', reason: 'Legacy API route controlled by not-found only.' }
  104 | ];
  105 | 
  106 | test.use({ ignoreHTTPSErrors: true });
  107 | 
  108 | test.describe('SPA route smoke', () => {
  109 |   test.beforeEach(async ({ page }) => {
  110 |     await seedAuthenticatedSession(page);
  111 |     await mockAuthRefresh(page);
  112 |     await mockNavigation(page);
  113 |     await mockBackend(page);
  114 |   });
  115 | 
  116 |   test.afterEach(async ({ page }, testInfo) => {
  117 |     if (testInfo.status === testInfo.expectedStatus) {
  118 |       return;
  119 |     }
  120 | 
  121 |     await page.screenshot({
  122 |       path: testInfo.outputPath(`${slugify(testInfo.title)}.png`),
  123 |       fullPage: true
  124 |     });
  125 |   });
  126 | 
  127 |   test('RouteCoverage_ShouldReportIncludedAndOmittedRoutes', async ({ page }, testInfo) => {
  128 |     await testInfo.attach('spa-routes-coverage.json', {
  129 |       body: JSON.stringify({ included: routes.map((route) => route.path), omitted: omittedRoutes }, null, 2),
  130 |       contentType: 'application/json'
  131 |     });
  132 | 
  133 |     await expect(routes.length).toBeGreaterThan(0);
  134 |     await expect(omittedRoutes.length).toBeGreaterThan(0);
  135 |   });
  136 | 
  137 |   for (const route of routes) {
  138 |     test(`Route_ShouldRender_${route.id}`, async ({ page }, testInfo) => {
  139 |       const consoleErrors: string[] = [];
  140 |       const criticalRequestFailures: string[] = [];
  141 |       const htmlAssetResponses: string[] = [];
  142 | 
  143 |       page.on('console', (message) => {
  144 |         if (message.type() !== 'error') {
  145 |           return;
  146 |         }
  147 | 
  148 |         const text = message.text();
  149 |         if (!isBenignConsoleError(text)) {
  150 |           consoleErrors.push(text);
  151 |         }
  152 |       });
  153 | 
  154 |       page.on('requestfailed', (request) => {
  155 |         const url = request.url();
  156 |         if (isCriticalAssetOrApi(url)) {
  157 |           criticalRequestFailures.push(`${request.method()} ${url} ${request.failure()?.errorText ?? ''}`.trim());
  158 |         }
  159 |       });
  160 | 
  161 |       page.on('response', async (response) => {
  162 |         const url = response.url();
  163 |         if (!isAssetRequest(url)) {
  164 |           return;
  165 |         }
  166 | 
  167 |         const contentType = response.headers()['content-type'] ?? '';
  168 |         if (contentType.includes('text/html')) {
  169 |           htmlAssetResponses.push(`${response.status()} ${url} ${contentType}`);
  170 |         }
  171 |       });
  172 | 
  173 |       await page.goto(route.path);
  174 | 
  175 |       await expect(page.locator('body')).not.toHaveText(/ChunkLoadError|Application error|UnhandledPromiseRejection/i);
  176 |       const bodyLength = await page.locator('body').evaluate((node) => (node.textContent ?? '').trim().length);
  177 |       expect(bodyLength).toBeGreaterThan(0);
  178 | 
  179 |       if (consoleErrors.length || criticalRequestFailures.length || htmlAssetResponses.length) {
  180 |         await testInfo.attach(`${route.id}-observability.json`, {
  181 |           body: JSON.stringify({ consoleErrors, criticalRequestFailures, htmlAssetResponses }, null, 2),
  182 |           contentType: 'application/json'
  183 |         });
  184 |       }
  185 | 
> 186 |       expect(consoleErrors).toEqual([]);
      |                             ^ Error: expect(received).toEqual(expected) // deep equality
  187 |       expect(criticalRequestFailures).toEqual([]);
  188 |       expect(htmlAssetResponses).toEqual([]);
  189 |     });
  190 |   }
  191 | 
  192 |   test('LegacyRoutes_ShouldEndInNotFound', async ({ page }) => {
  193 |     for (const legacyRoute of legacyRoutes) {
  194 |       await page.goto(legacyRoute);
  195 |       await expect(page).toHaveURL(/\/not-found$/);
  196 |       await expect(page.getByText('404', { exact: true })).toBeVisible();
  197 |     }
  198 |   });
  199 | });
  200 | 
  201 | async function mockNavigation(page: Page): Promise<void> {
  202 |   await page.route(navigationEndpoint, async route => {
  203 |     await route.fulfill({
  204 |       status: 200,
  205 |       contentType: 'application/json',
  206 |       body: JSON.stringify([
  207 |         { id: 1, label: 'Panel principal', route: '/dashboard' },
  208 |         { id: 2, label: 'Usuarios', route: '/users', children: [
  209 |           { id: 21, label: 'Identidad y colores', route: '/users/branding' },
  210 |           { id: 22, label: 'Reglas de contraseña', route: '/users/password-rules' },
  211 |           { id: 23, label: 'Bloqueo de acceso', route: '/users/login-lockout' }
  212 |         ]},
  213 |         { id: 3, label: 'Integraciones', route: '/integraciones', children: [
  214 |           { id: 31, label: 'Integraciones SOAP', route: '/soap-integrations' }
  215 |         ]},
  216 |         { id: 4, label: 'CENIT', route: '/cenit', children: [
  217 |           { id: 41, label: 'Regulatorio: Devoluciones', route: '/cenit/regulatorio/causales-devolucion' },
  218 |           { id: 42, label: 'Regulatorio: Rechazos', route: '/cenit/regulatorio/causales-rechazo' },
  219 |           { id: 43, label: 'Regulatorio: Políticas', route: '/cenit/regulatorio/politicas-transaccion' },
  220 |           { id: 44, label: 'Operación: Ciclos', route: '/cenit/operacion/ciclos' },
  221 |           { id: 45, label: 'Operación: Cola', route: '/cenit/operacion/cola' },
  222 |           { id: 46, label: 'Operación: Neteo', route: '/cenit/operacion/neteo' },
  223 |           { id: 47, label: 'Operación: Optimización', route: '/cenit/operacion/optimizacion' },
  224 |           { id: 48, label: 'Operación: Devoluciones', route: '/cenit/operacion/devoluciones' },
  225 |           { id: 49, label: 'Operación: Trazabilidad', route: '/cenit/operacion/trazabilidad' }
  226 |         ]},
  227 |         { id: 5, label: 'Configuración NACHA-M', route: '/nacha-config-admin/perfiles', children: [
  228 |           { id: 51, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' },
  229 |           { id: 52, label: 'Registros oficiales', route: '/nacha-config-admin/records' },
  230 |           { id: 53, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields' }
  231 |         ]},
  232 |         { id: 6, label: 'Catálogos', route: '/catalogs' },
  233 |         { id: 7, label: 'Transacciones', route: '/transactions', children: [
  234 |           { id: 71, label: 'Listado', route: '/transactions/list' },
  235 |           { id: 72, label: 'Crear transacción', route: '/transactions/create' },
  236 |           { id: 73, label: 'Carga masiva', route: '/transactions/bulk-create' },
  237 |           { id: 74, label: 'Carga masiva por archivo', route: '/transactions/bulk-ingestion/upload' },
  238 |           { id: 75, label: 'Seguimiento lotes', route: '/transactions/bulk-ingestion/tracking' },
  239 |           { id: 76, label: 'Config. ciclos', route: '/transactions/cycle-configs' },
  240 |           { id: 77, label: 'Reglas por cámara', route: '/transactions/clearing-house-rules' },
  241 |           { id: 78, label: 'Cargar NACHA-M', route: '/transactions/nacha-upload' },
  242 |           { id: 79, label: 'Devoluciones ACH', route: '/transactions/returns' }
  243 |         ]},
  244 |         { id: 8, label: 'Navegación', route: '/navigation', children: [
  245 |           { id: 81, label: 'Menús', route: '/navigation/menu-items' }
  246 |         ]},
  247 |         { id: 9, label: 'Seguridad NACHA', route: '/nacha-security/dashboard', children: [
  248 |           { id: 91, label: 'Dashboard seguridad', route: '/nacha-security/dashboard' },
  249 |           { id: 92, label: 'Certificados', route: '/nacha-security/certificates' },
  250 |           { id: 93, label: 'Sobre digital', route: '/nacha-security/sobre-digital' }
  251 |         ]},
  252 |         { id: 10, label: 'Programador', route: '/scheduler', children: [
  253 |           { id: 101, label: 'Tareas programadas', route: '/scheduler/tasks' }
  254 |         ]},
  255 |         { id: 11, label: 'Logs', route: '/audit-logs', children: [
  256 |           { id: 111, label: 'Logs de auditoría', route: '/audit-logs' },
  257 |           { id: 112, label: 'Logs de autenticaciones', route: '/auth-logs' },
  258 |           { id: 113, label: 'Logs de navegación', route: '/navigation-logs' }
  259 |         ]},
  260 |         { id: 12, label: 'UAT / Simuladores', route: '/uat', children: [
  261 |           { id: 121, label: 'Simulador NACHA-M Entrada', route: '/uat/nacha-inbound-simulator' }
  262 |         ]},
  263 |         { id: 13, label: 'Respuestas ACH', route: '/ach-responses' },
  264 |         { id: 14, label: 'Reportes', route: '/reports' },
  265 |         { id: 15, label: 'Command Center inbound NACHA', route: '/incoming-nacha-command-center' },
  266 |         { id: 16, label: 'Ciclos ACH', route: '/ach-cycles' },
  267 |         { id: 17, label: 'Capability Registry', route: '/payment-rail-capability-registry' },
  268 |         { id: 18, label: 'Clientes', route: '/customers' },
  269 |         { id: 19, label: 'Alias', route: '/aliases' },
  270 |         { id: 20, label: 'Dashboard operativo', route: '/ach' }
  271 |       ])
  272 |     });
  273 |   });
  274 | }
  275 | 
  276 | async function mockAuthRefresh(page: Page): Promise<void> {
  277 |   const token = createUnsignedJwt({
  278 |     unique_name: 'spa.smoke',
  279 |     name: 'Usuario SPA Smoke',
  280 |     uid: 'spa-smoke',
  281 |     role: ['Admin', 'ACH.Operator'],
  282 |     permission: [
  283 |       'CanReadAch',
  284 |       'CanManageAch',
  285 |       'CanReadCatalogs',
  286 |       'CanManageUsers',
```