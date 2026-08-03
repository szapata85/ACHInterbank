# Evidencia de readiness y resolver

## Readiness

Para `Proc_Contrapartidas` se consideran criticos `OFCTA`, `OFDD`, `OFMONDEB`, `OFMONCRE` y `OFIDTX`. Se valida tanto la regla publicada efectiva como las fuentes persistidas de la transaccion.

Resultados cubiertos:

- placeholder monetario: `Failed`, `IsReady=false`;
- placeholder de direccion: `Failed`, `IsReady=false`;
- referencia placeholder: `Failed`, `IsReady=false`;
- fuente obligatoria ausente: `Failed`, sin fallback;
- mapping valido: construible, sin errores ni fallback;
- IP tecnica local: unica advertencia no financiera permitida.

Un mapping publicado invalido no activa mapping alterno ni se degrada silenciosamente a warning.

## Resolver

El resolver exige un unico mapping publicado activo, una regla activa por parametro obligatorio y una fuente resoluble. Las conversiones numericas usan cultura invariante y fallan con un error controlado cuando el valor no es valido. No existen conversiones implicitas de ausencia a `0`.

El fixture canonico resolvio:

- referencia: `TX-REF-001` desde la transaccion;
- monto: valor decimal persistido;
- direccion: `D` desde el tipo debito;
- identificadores: fuentes persistidas de transaccion, lote y camara.

## Pruebas focalizadas

El filtro conjunto de readiness, resolver y bootstrappers termino con 73 aprobadas, 0 fallidas y 0 omitidas. Las expectativas originales de seguridad se conservaron.
