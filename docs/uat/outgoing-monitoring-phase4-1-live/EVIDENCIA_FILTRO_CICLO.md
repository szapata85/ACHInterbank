# Evidencia — filtro de ciclo

- Control: `mat-select`, sin entrada arbitraria.
- Fuente: catálogo real `GET /api/ach-cycles`.
- Valor enviado: identificador estable del ciclo.
- Texto visible: nombre de ciclo, cámara compensadora y hora de corte.
- Dependencia: al cambiar cámara se cancela la solicitud anterior, se consulta el catálogo compatible y se limpia una selección incompatible.
- Estados verificados: cargando, todos los ciclos, vacío, error y reintento.
- Limpieza: restaura `Todos los ciclos` y no conserva un identificador oculto.

Pruebas: 11/11 pruebas focalizadas del componente; 685/685 pruebas Angular; cuatro escenarios Playwright reales en escritorio, tableta, móvil y permisos.
