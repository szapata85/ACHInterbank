#!/usr/bin/env python3
"""Validación estática SEC-I04; no incluye ni imprime secretos del seed."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
app = root / 'src/Cfa.ACHInterbank.Application'
ps = app / 'Security/PasswordHashing/CredentialPasswordHasher.cs'
auth = app / 'Services/Authentication/Implementations/AuthService.cs'
users = root / 'src/Cfa.ACHInterbank.Persistence/Security/Services/UsersService.cs'
seed = root / 'src/Cfa.ACHInterbank.Persistence/Configuration/UserConfiguration.cs'
tests = root / 'tests/Cfa.ACHInterbank.Tests/Security/CredentialPasswordHasherTests.cs'

assert '600_000' in ps.read_text() and 'RandomNumberGenerator.GetBytes' in ps.read_text()
assert 'FixedTimeEquals' in ps.read_text() and 'SuccessLegacy' in ps.read_text()
assert 'CredentialPasswordHasher.Verify' in auth.read_text()
assert 'CredentialPasswordHasher.HashPassword' in auth.read_text()
assert auth.read_text().count('HashHelper.GenerateHashSha256') == 0
assert users.read_text().count('CredentialPasswordHasher.HashPassword(request.Password)') == 2
assert 'private static string BuildHash(' not in users.read_text()
assert 'HasData(new User' in seed.read_text() and 'PasswordHash = BuildHash(' in seed.read_text()
assert 'NewPasswordsUseSaltedVersionedPbkdf2' in tests.read_text()
print('PASS_STATIC: 9 checks (seed presence not a byte-level origin comparison).')
