"""Lectura de appsettings.json con comentarios // en líneas completas.

A diferencia de eliminar // arbitrariamente, nunca modifica URLs ni contenido
entrecomillado: solo elimina líneas cuyo primer texto es //.
"""
import json
import re
from pathlib import Path


def load_jsonc(path: str | Path):
    source = Path(path).read_text(encoding="utf-8-sig")
    clean = re.sub(r"(?m)^[ \t]*//[^\r\n]*(?:\r?\n|$)", "", source)
    return json.loads(clean)
