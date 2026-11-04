#!/usr/bin/env python3
"""Tests de consistencia del fixture documental APN; NO son pruebas C#/BD ni certificación de cámaras."""
import csv
from pathlib import Path
import unittest

CSV_PATH = Path(__file__).resolve().parents[3] / 'docs/auditoria-prenotificaciones/MATRIZ_CICLOS_REFERENCIA.csv'

class AuditReferenceScheduleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        with CSV_PATH.open(encoding='utf-8', newline='') as f:
            cls.rows = list(csv.DictReader(f))

    def test_ach_colombia_exactly_seven_origins(self):
        rows = [r for r in self.rows if r['camara'] == 'ACHCOL']
        self.assertEqual([str(x) for x in range(1,8)], [r['origen_ciclo'] for r in rows])

    def test_cenit_only_four_valid_origins(self):
        rows = [r for r in self.rows if r['camara'] == 'CENIT']
        self.assertEqual(['1','2','3','4'], [r['origen_ciclo'] for r in rows])

    def test_ach_colombia_c1_fourth_subsequent_cycle_c5(self):
        r = next(r for r in self.rows if r['camara']=='ACHCOL' and r['origen_ciclo']=='1')
        self.assertEqual(('5','15:15-15:30'), (r['ultimo_ciclo_devolucion'],r['ultima_ventana_salida']))

    def test_ach_colombia_c4_crosses_business_day(self):
        r = next(r for r in self.rows if r['camara']=='ACHCOL' and r['origen_ciclo']=='4')
        self.assertEqual(('1','siguiente día hábil'), (r['ultimo_ciclo_devolucion'],r['dia_ultimo_ciclo']))

    def test_cenit_last_output_1915(self):
        rows = [r for r in self.rows if r['camara']=='CENIT']
        self.assertTrue(all(r['ultima_ventana_salida']=='hasta 19:15' for r in rows))

    def test_all_rows_have_nonempty_cutoff_and_first_and_last_cycle(self):
        for r in self.rows:
            with self.subTest(camara=r['camara'],ciclo=r['origen_ciclo']):
                self.assertTrue(r['corte'])
                self.assertTrue(r['primer_ciclo_devolucion'])
                self.assertTrue(r['ultimo_ciclo_devolucion'])

if __name__ == '__main__':
    unittest.main(verbosity=2)
