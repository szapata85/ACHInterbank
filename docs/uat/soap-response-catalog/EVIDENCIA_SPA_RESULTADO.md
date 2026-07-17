# Evidencia SPA del resultado del core

Fecha: 2026-07-16.

El panel **RESULTADO DEL PROCESAMIENTO EN EL CORE** fue validado con el resultado persistido de la única ejecución local:

- Servicio: `Proc_Contrapartidas`.
- Resultado: Exitoso.
- Código: `R96`.
- Descripción: “Débito aplicado correctamente”.

La selección de la fila se verificó sobre la misma transacción creada en el escenario. AG Grid 32 requirió habilitar explícitamente `enableClickSelection`; no se agregó lógica basada en `R96` a Angular.

La vista depende de `BusinessStatus` y no expone XML, JSON técnico, request ni response. Después de reiniciar la API, el panel volvió a renderizar el mismo resultado desde la persistencia.

Evidencia automatizada:

- Angular: 395/395.
- Playwright smoke sin SOAP: aprobado.
- Reanudación Playwright read-only del escenario LIVE: aprobada antes y después del reinicio.

Las capturas permanecen en artefactos temporales de prueba y no incorporan datos sensibles al repositorio.
