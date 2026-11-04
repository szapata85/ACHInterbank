# I05-ACH — Generación controlada desde candidatas aprobadas

**Corte:** 2 de noviembre de 2026 (hora Colombia). **Cámara:** ACH Colombia (`ACHCOL`) únicamente. **Base acumulativa:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I04_ACHCOLOMBIA_2026-11-02.zip`. **Resultado:** código y pruebas incorporados; ejecución real y homologación pendientes. **Producción: NO-GO.**

## Objetivo y alcance real

I05 conecta la candidata **APROBADA_SIN_ENVIO** de I04 con `IAchReturnsService.GenerateReturnsFileAsync`, existente en el proyecto. La operación requiere un clic y confirmación explícitos del usuario autorizado. La pantalla actual «Devoluciones» permite descargar el `.RET` con el nombre oficial devuelto por el generador; no se agrega una pantalla nueva.

**No se habilita la generación autónoma programada, ni transmisión, ni acuse, ni compensación ni liquidación de cámara.** Las configuraciones I01 `CON_APROBACION` y `AUTOMATICO` permiten el flujo interno de preparación/aprobación, pero `ProcessingConnected=false` sigue indicando que el procesamiento autónomo no está conectado. CENIT no forma parte de esta intervención.

## Flujo implementado

1. Usuario con política `P0.ReturnsGenerateFile` selecciona **Generar archivo** sobre una candidata previamente aprobada, y confirma.
2. `POST api/ach-colombia/devoluciones-automaticas/candidatas/{candidateId}/generar-archivo` recibe **solo el identificador**; no acepta que el navegador altere transacción original, cámara, causal ni ciclo.
3. El backend exige `ACHCOL`, estado `APROBADA_SIN_ENVIO`, modo habilitado, ciclo original coincidente, ausencia de archivo previo, causal del catálogo ACH vigente y evidencia I02 de origen `CORE_FUNCIONAL`.
4. Recalcula la ventana mediante `AchColombiaReturnPreparationWindow.Evaluate` y exige exactamente el ciclo originalmente aprobado. Mantiene fuera del flujo R10 sin evidencia adicional y créditos tardíos.
5. Reutiliza el generador normativo NACHA-M; el generador original administra validaciones adicionales, reserva de secuencia y unicidad por transacción.
6. Comprueba `AchReturnsGenerated`, ciclo, causal, archivo y contenido retornado. Luego cambia la candidata a `ARCHIVO_GENERADO_SIN_ENVIO` y registra la operación con el mecanismo de auditoría existente. Se utiliza una transacción EF relacional exterior, ejecutada bajo la estrategia de reintentos configurada, para confirmar juntos registro y estado; debe probarse con el proveedor real.
7. El endpoint responde `GenerateReturnsFileResponse` como JSON; `Content` de .NET llega en base64. La SPA descarga el archivo `.RET` usando **el nombre devuelto por el servicio**, no uno inventado. No invoca transportador.

**Distinción importante:** el generador ya existente aplica su transición interna `ReturnedByEpr` a la transacción original al generar un archivo. Esto NO demuestra transmisión ni liquidación; deben revisarse los efectos de esa transición en UAT antes de cualquier habilitación productiva.

## Archivos tocados y decisión de diseño

- Contratos: `IAchColombiaReturnReviewService.cs`, `AchColombiaReturnReviewModels.cs`.
- Servicio I04 extendido: `AchColombiaReturnReviewService.cs`, reutilizando el método de validación de aprobación y el mismo `IAchReturnsService`.
- API con permiso existente: `AchColombiaReturnReviewController.cs`.
- SPA Angular Material existente: `ach-colombia-return-review.component.{ts,html}`, `ach-colombia-return-review-api.service.ts`.
- Pruebas: `AchColombiaApprovedReturnGenerationI05Tests.cs`, especificaciones Jasmine ampliadas y comprobador estático `automatic_returns_i05_ach_static_check.py`.
- Memoria, evidencias, manifiesto, estado incremental y README de I05.

No se añadió tabla, migración, cola, tarea programada, ruta de SPA, perfil de permiso ni un segundo generador. CENIT y sus políticas/servicios permanecen sin cambios.

## Pruebas y honestidad de resultados

| Validación | Resultado |
|---|---|
| I05 estática (33 reglas) | **PASS_STATIC 33/33** |
| Regresión I02 | **PASS_STATIC 18/18** |
| Regresión I03 | **PASS_STATIC 25/25** |
| Script histórico I04 sobre código I05 | **33/35**: dos aserciones `sin generador NACHA` y `SPA no invoca creación archivo` ya no aplican por el objetivo explícito I05 |
| Parseo TypeScript archivos afectados | Sin errores de sintaxis; NO equivale a compilación |
| Grafo de solución | 7/7 proyectos |
| xUnit/compilación .NET, pruebas Angular completas, SQL Server/PostgreSQL, concurrencia distribuida, verificación en navegador | **NO EJECUTADAS por falta de entorno** |
| Homologación ACH Colombia y CENIT | **PENDIENTE** |

Las pruebas incluidas cubren éxito con aprobación y archivo persistido, cámara CENIT, modos desactivados, candidatas sin aprobar/descartadas, devolución manual existente, cambio de ciclo, R10, error del generador, ausencia de fila persistida, segundo intento y confirmación SPA.

## Riesgos y condiciones de desbloqueo

- Verificar con .NET y base real que el servicio de generación sea transaccional al interactuar con el `DbContext` compartido, la política de reintentos EF, la auditoría del generador y la restricción única de `AchReturnsGenerated`.
- Ejecutar la migración SQL Server I01/I03 y resolver **paridad PostgreSQL pendiente** antes de certificar ese proveedor.
- Probar cambio de configuración entre aprobación/generación, vencimiento de ciclo, concurrencia multiinstancia con la generación manual, nombre NACHA-M y contenido de registro 106 caracteres.
- Probar descarga real. Si la llamada se confirmó en servidor y el navegador falla al descargar, **no repetir la generación**: consultar el artefacto/auditoría y el procedimiento de recuperación; el endpoint no implementa recuperación de archivos en I05.
- Revisar el comportamiento de `ReturnedByEpr` de la transacción original: un archivo creado **no** es aceptación de cámara.
- Confirmar horarios operativos de cinco/siete ciclos con fuentes vigentes antes de activar automatismos.
- En modo `AUTOMATICO` **no** existe todavía un proceso que genere o transmita sin intervención humana.

## Punto de parada

**I05-ACH: incorporación en fuente cerrada, sin pruebas de ejecución y NO-GO.** Próximo incremento recomendado **I06-ACH**: verificación y conexión controlada de ejecución automática y/o transporte solo con evidencias y política de habilitación, nunca activarlo productivamente sin pruebas, homologación y conciliación. **CENIT conserva ruta totalmente independiente.**
