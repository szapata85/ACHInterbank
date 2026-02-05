#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'src'

service_interface_files = list(SRC.glob('**/Interfaces/I*Service.cs'))
result_return_pattern = re.compile(r'Task<\s*(Result(?:<[^>]+>)?)\s*>')
method_pattern = re.compile(r'Task<[^>]+>\s+\w+\s*\(')

violations = []
for file in service_interface_files:
    text = file.read_text(encoding='utf-8', errors='ignore')
    for line_no, line in enumerate(text.splitlines(), start=1):
        if 'Task<' in line and method_pattern.search(line):
            if not result_return_pattern.search(line):
                violations.append((str(file.relative_to(ROOT)), line_no, line.strip()))

# simplistic SRP heuristic: very large service classes
service_class_files = list(SRC.glob('**/Services/*.cs'))
srp_flags = []
for file in service_class_files:
    lines = file.read_text(encoding='utf-8', errors='ignore').splitlines()
    if len(lines) > 260:
        srp_flags.append((str(file.relative_to(ROOT)), len(lines)))

print('=== Result Pattern Interface Scan ===')
if violations:
    for v in violations:
        print(f'{v[0]}:{v[1]} -> {v[2]}')
else:
    print('OK: no interface violations found.')

print('\n=== SOLID SRP Heuristic (large services) ===')
if srp_flags:
    for f, n in srp_flags:
        print(f'{f} -> {n} lines (review SRP)')
else:
    print('OK: no oversized service classes detected by heuristic.')

# non-zero if result pattern violations exist
raise SystemExit(1 if violations else 0)
