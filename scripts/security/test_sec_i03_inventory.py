#!/usr/bin/env python3
"""Pruebas offline de SEC-I03, sin pytest ni acceso de red."""
from __future__ import annotations
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
import generate_sec_i03_inventory as scanner

class SecI03Tests(unittest.TestCase):
    def test_node_resolution_nested_and_scoped(self):
        paths={'node_modules/a','node_modules/x','node_modules/a/node_modules/x','node_modules/@scope/foo'}
        self.assertEqual(scanner.resolve_dep('node_modules/a','x',paths),'node_modules/a/node_modules/x')
        self.assertEqual(scanner.resolve_dep('node_modules/a','@scope/foo',paths),'node_modules/@scope/foo')
        self.assertIsNone(scanner.resolve_dep('node_modules/a','missing',paths))

    def test_bom_incomplete(self):
        b=scanner.bom('Test scope',[scanner.prop('scope','static')],[],[])
        self.assertEqual(b['compositions'][0]['aggregate'],'incomplete')
        self.assertEqual(b['specVersion'],'1.6')

    def test_real_source_reproducible_and_consistent(self):
        root=Path(__file__).resolve().parents[2]
        with tempfile.TemporaryDirectory() as a, tempfile.TemporaryDirectory() as b:
            cmd=[sys.executable,str(root/'scripts/security/generate_sec_i03_inventory.py'),'--root',str(root)]
            for target in (a,b):
                cp=subprocess.run(cmd+['--out',target],capture_output=True,text=True)
                self.assertEqual(cp.returncode,0,cp.stdout+cp.stderr)
            names=('SEC-I03_INVENTARIO_FUENTE.json','SEC-I03_NPM_LOCK.cdx.json','SEC-I03_NUGET_DECLARADO.cdx.json','SEC-I03_VALIDACION_ESTATICA.json')
            for name in names:
                self.assertEqual((Path(a)/name).read_bytes(),(Path(b)/name).read_bytes())
            s=json.loads((Path(a)/'SEC-I03_VALIDACION_ESTATICA.json').read_text())
            inv=json.loads((Path(a)/'SEC-I03_INVENTARIO_FUENTE.json').read_text())
            self.assertEqual(s['npm_packages'],inv['npm']['total_packages'])
            self.assertEqual(s['npm_missing_integrity'],len(inv['npm']['missing_integrity_paths']))
            self.assertEqual(len(json.loads((Path(a)/'SEC-I03_NPM_LOCK.cdx.json').read_text())['components']),s['npm_packages'])
            self.assertEqual(len(json.loads((Path(a)/'SEC-I03_NUGET_DECLARADO.cdx.json').read_text())['components']),s['nuget_unique_declared_packages'])
            self.assertEqual(s['docker_images_unpinned'],sum(not x['is_pinned_by_digest'] for x in inv['docker']['declared_images']))
            self.assertEqual(s['docker_layer_scan'],'NOT_RUN_ENVIRONMENT')

if __name__=='__main__':unittest.main(verbosity=2)
