# Decisiones de experiencia de usuario — I06

## Principio

Una sola tarea por pantalla, con contexto suficiente y sin jerga innecesaria. Se conserva el estilo Material y los tokens existentes, sin dependencias, rutas o submódulos nuevos.

## Jerarquía visual

1. Acceso identificable «Responder prenotificaciones enviadas» desde el selector existente.
2. Filtro pequeño de cámara y referencia; lista de una operación seleccionable por vez. Se muestran incluso bloqueadas, con su motivo de elegibilidad.
3. Dos opciones excluyentes con lenguaje claro: «Devolución» y «Sin devolución».
4. Únicamente los campos del resultado elegido: causal y ciclo cuando hay devolución; hora de evaluación opcional cuando no la hay.
5. Vista previa antes de botón irreversible, con modal de confirmación específico.
6. Resultado final y siguiente acción útil: descarga + NachaUpload, o resolución tácita sin fichero.

## Seguridad comunicada, sin alarmismo

- La simulación no transmite archivos a cámaras reales ni importará automáticamente los de devolución.
- Hora virtual no habilita «Aplicar vencimiento real»; hay que borrar el campo virtual y validar con reloj real.
- La UI no sustituye la validación de backend. Los estados autorizables se calculan en los servicios I02–I05.
- La selección no permite paquetes de múltiples bancos o ciclos. Es intencional (simplicidad y trazabilidad).

## Responsividad y accesibilidad

- Dos columnas de alternativas en escritorio; una en móvil.
- Los elementos seleccionables tienen `aria-pressed` y foco visible, las respuestas tienen regiones `role=status`/`role=alert`.
- Estados loading, error y vacío explícitos; sin uso exclusivo del color.
- Los estilos respetan tokens de superficie/borde/texto existentes y no añaden colores fijos ni tipografías externas.

## Restricción de evidencia

No se generaron capturas nuevas de la SPA modificada: Angular no pudo instalarse desde caché y no fue posible lanzar navegador. Estas decisiones se basan en código y reglas de diseño, no son validación visual en runtime.
