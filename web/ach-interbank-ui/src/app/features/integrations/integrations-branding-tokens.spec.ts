import { IntegrationWorkspaceComponent } from './pages/integration-workspace.component';
import { MappingEditorPageComponent } from './pages/mapping-editor-page.component';
import { MappingPublicationDialogComponent } from './pages/mapping-publication-dialog.component';
import { ContractVersionsPageComponent } from './pages/contract-versions-page.component';
import { ContractImportStepperComponent } from './pages/contract-import-stepper.component';
import { RealSendConfirmationDialogComponent, SoapIntegrationSettingsComponent } from './pages/soap-integration-settings.component';
import { MappingSetsPageComponent } from './pages/mapping-sets-page.component';
import { IntegrationCenterShellComponent } from './presentation/integration-center-shell.component';
import { IntegrationServiceCardComponent } from './presentation/integration-service-card.component';

type ComponentWithStyles = { ɵcmp?: { styles?: string[] } };

function compiledStyles(component: unknown): string {
  return (((component as ComponentWithStyles).ɵcmp?.styles) ?? []).join('\n').replace(/\/\*# sourceMappingURL=[\s\S]*?\*\//g, '');
}

describe('Integraciones CFA branding hardening', () => {
  const components = [
    IntegrationWorkspaceComponent,
    IntegrationCenterShellComponent,
    IntegrationServiceCardComponent,
    MappingSetsPageComponent,
    MappingEditorPageComponent,
    MappingPublicationDialogComponent,
    ContractVersionsPageComponent,
    ContractImportStepperComponent,
    SoapIntegrationSettingsComponent,
    RealSendConfirmationDialogComponent
  ];

  it('no conserva colores hex/rgb/hsl hardcodeados en estilos de la feature tocada', () => {
    const css = components.map(compiledStyles).join('\n');

    expect(css).not.toMatch(/#[0-9a-f]{3,8}\b/i);
    expect(css).not.toMatch(/\b(?:rgb|rgba|hsl|hsla)\s*\(/i);
  });

  it('consume tokens globales o Material para marca, superficie y estados', () => {
    const css = components.map(compiledStyles).join('\n');

    expect(css).toContain('var(--color-primary)');
    expect(css).toContain('var(--color-surface)');
    expect(css).toContain('var(--color-success-surface)');
    expect(css).toContain('var(--mat-sys-surface)');
  });

  it('no introduce una familia tipográfica local en Integraciones CFA', () => {
    const css = components.map(compiledStyles).join('\n');

    expect(css.replace(/font-family\s*:\s*var\(--font-family-monospace,\s*monospace\);/gi, '')).not.toMatch(/font-family\s*:/i);
  });
  it('conserva breakpoints funcionales compacto y mediano sin estilos de marca locales', () => {
    const css = components.map(compiledStyles).join('\n');

    expect(css).toMatch(/max-width:\s*1199px/);
    expect(css).toMatch(/max-width:\s*767px/);
  });

});
