# Integridad APN-I11 — alcance interno

El manifiesto `APN_I11_MANIFIESTO_DIFERENCIAS.json` establece los archivos nuevos, modificados y conservados con respecto al ZIP acumulativo APN-I10. El empaquetador verifica ZIP CRC y SHA-256 por entrada contra el árbol extraído y compara todas las entradas originales no modificadas con el ZIP base.

El reporte con el hash SHA-256 del **ZIP completo** se distribuye como `APN_I11_INTEGRIDAD.md` al lado del ZIP. No se incrusta ese hash dentro del mismo archivo, porque crearía una autorreferencia que invalidaría el resultado.

Esta integridad de contenido no demuestra compilación .NET, pruebas de concurrencia SQL ni aprobación UAT.
