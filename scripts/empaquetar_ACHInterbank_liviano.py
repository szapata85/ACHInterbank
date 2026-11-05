#!/usr/bin/env python3
"""Empaqueta únicamente las fuentes de ACHInterbank, sin cachés ni salidas de build.

Uso, desde cualquier directorio:
  python scripts/empaquetar_ACHInterbank_liviano.py --source RUTA_PROYECTO --output archivo.zip
No modifica ni elimina archivos del directorio fuente.
"""
import argparse
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

EXCLUDED_DIRS = {
    '.angular', '.vs', '.git', 'bin', 'obj', 'dist', 'node_modules',
    'test-results', 'playwright-report', 'blob-report', 'coverage',
    '__pycache__', '.pytest_cache', '.next', '.turbo', '.cache',
}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=Path('.'), help='Carpeta raíz del proyecto')
    parser.add_argument('--output', type=Path, required=True, help='Ruta del ZIP final')
    args = parser.parse_args()
    source = args.source.resolve()
    output = args.output.resolve()
    if not source.is_dir():
        parser.error('La carpeta --source no existe')
    included = excluded = 0
    with ZipFile(output, 'w', compression=ZIP_DEFLATED, compresslevel=7, allowZip64=True) as target:
        for item in sorted(source.rglob('*')):
            if not item.is_file() or item.is_symlink() or item.resolve() == output:
                continue
            rel = item.relative_to(source)
            if any(part.lower() in EXCLUDED_DIRS for part in rel.parts[:-1]):
                excluded += 1
                continue
            target.write(item, arcname=(Path(source.name) / rel).as_posix())
            included += 1
    print(f'Incluidos: {included}; omitidos como regenerables: {excluded}; archivo: {output}')

if __name__ == '__main__':
    main()
