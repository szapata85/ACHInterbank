#!/usr/bin/env python3
"""Source-level regression checks for canonical root ordering. Not a .NET test runner."""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
read = lambda path: (root / path).read_text(encoding='utf-8')
model = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemConfiguration.cs')
functional = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NachaSecurityMenuSeeder.cs')
canonical = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
outgoing = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/OutgoingTransactionMonitoringSeeder.cs')
infra = read('src/Cfa.ACHInterbank.Persistence/DataBase/DatabaseInitializationHostedService.cs')
cli = read('tools/AchInterbank.DockerCli/Program.cs')
tests = read('tests/Cfa.ACHInterbank.Tests/NavigationMenuSecurityCanonicalSeedRegressionTests.cs')
legacy = read('tests/Cfa.ACHInterbank.Tests/NavigationMenuNav1BaselineTests.cs')
nav26 = read('tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs')
script = read('scripts/sql/repair-navigation-file-security-root.sql')
document = read('docs/ai/SPA-NAVIGATION-CANONICAL-SEED-2026-11-05.md')

checks = [
    ('home stable ID', 'public const int DashboardId = 1;' in model),
    ('security stable ID', 'public const int NachaSecurityId = 10;' in model),
    ('historical EF home seed untouched', bool(re.search(r'Id = DashboardId,.*?Label = "Panel principal",.*?Route = "/dashboard"', model, re.S))),
    ('historical EF security seed untouched', bool(re.search(r'Id = NachaSecurityId,.*?Label = "Seguridad NACHA",\s*Route = "/nacha-security/certificates",\s*Icon = "security",\s*Order = 8,', model, re.S))),
    ('canonical home order', 'public const int HomeRootOrder = 10;' in canonical and 'order: HomeRootOrder' in canonical),
    ('canonical security order', 'public const int FileSecurityRootOrder = 60;' in canonical and 'order: FileSecurityRootOrder' in canonical),
    ('canonical security name', 'public const string FileSecurityLabel = "Seguridad de archivos";' in canonical),
    ('security functional references canonical', all(x in functional for x in ('NavigationInformationArchitectureSeeder.FileSecurityRootOrder','NavigationInformationArchitectureSeeder.FileSecurityLabel','NavigationInformationArchitectureSeeder.FileSecurityRoute'))),
    ('security functional no legacy label', 'Seguridad NACHA' not in functional),
    ('security functional child ordering', '"verified_user",\n            order: 10);' in functional and '"lock",\n            order: 20);' in functional),
    ('security functional child label', '"Cifrar y descifrar archivos"' in functional),
    ('security functional permission guard retained', all(x in functional for x in ('EnsureManageCertificatesPermissionAsync(certificates.Id)','RemoveRoleRestrictionsAsync','DisableDuplicateCertificateRoutesAsync'))),
    ('navigation final reconciler order', 'public const int FinalReconciliationOrder = 100;' in canonical),
    ('final uses canonical file security', 'Reconcile(fileSecurity, FileSecurityLabel, parentId: null, order: FileSecurityRootOrder)' in canonical),
    ('final uses canonical home', 'Reconcile(home, HomeLabel, parentId: null, order: HomeRootOrder)' in canonical),
    ('outgoing dashboard not reset to old label', 'CanonicalDashboardLabel = NavigationInformationArchitectureSeeder.HomeLabel' in outgoing),
    ('seed startup guard', 'Database:ApplySeed' in infra and 'DbInitializer.SeedAllAsync' in infra),
    ('CLI seed opt in', 'options.Contains("--seed")' in cli and 'options.Contains("--no-seed")' in cli),
    ('updated functional seed baseline expectation', '("/nacha-security", 60)' in legacy and '("/nacha-security", 8)' not in legacy),
    ('new tests cover fresh database', 'FreshDatabase_FunctionalSeedPlacesFileSecurityAfterHome' in tests),
    ('new tests cover repair and idempotence', 'FunctionalSeed_RepairsLegacyRow_AndRepeatedExecutionDoesNotCreateDuplicates' in tests and 'Assert.Equal(first, second);' in tests),
    ('tests guard later functional seeder', 'FunctionalSeeder_AfterCanonicalReconciliation_DoesNotRestoreLegacyLabelOrOrder' in nav26),
    ('SQL repair is transactional', 'SET XACT_ABORT ON;' in script and 'BEGIN TRANSACTION;' in script and 'COMMIT TRANSACTION;' in script),
    ('SQL repair avoids ACL changes', all(x not in script for x in ('UPDATE dbo.MenuItemRoles', 'UPDATE dbo.MenuItemPermissions', 'DELETE FROM'))),
    ('documentation requires explicit seed', 'up --seed --no-auto-migrations' in document and 'migrate' in document),
    ('documentation warns skip', '--no-seed' in document and 'no puede reparar' in document),
]

for name, passed in checks:
    print(f'{"PASS" if passed else "FAIL"}: {name}')
print(f'TOTAL {sum(ok for _, ok in checks)}/{len(checks)}')
raise SystemExit(0 if all(ok for _, ok in checks) else 1)
