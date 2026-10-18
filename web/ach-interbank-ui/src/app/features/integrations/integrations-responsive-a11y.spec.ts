import { MappingEditorPageComponent } from './pages/mapping-editor-page.component';
import { MappingPublicationDialogComponent } from './pages/mapping-publication-dialog.component';
import { MappingSetsPageComponent } from './pages/mapping-sets-page.component';
import { ContractImportStepperComponent } from './pages/contract-import-stepper.component';
import { ContractVersionsPageComponent } from './pages/contract-versions-page.component';
import {
  RealSendConfirmationDialogComponent,
  SoapIntegrationSettingsComponent
} from './pages/soap-integration-settings.component';
import { IntegrationCenterShellComponent } from './presentation/integration-center-shell.component';
import { IntegrationServiceCardComponent } from './presentation/integration-service-card.component';

function compiledStyles(component: unknown): string {
  return (((component as { ɵcmp?: { styles?: string[] } }).ɵcmp?.styles) ?? []).join('\n').replace(/\/\*# sourceMappingURL=[\s\S]*?\*\//g, '');
}

describe('Integraciones CFA P7.4 responsive y accesibilidad', () => {
  const primaryStyles = () => [
    IntegrationCenterShellComponent,
    IntegrationServiceCardComponent,
    MappingSetsPageComponent,
    MappingEditorPageComponent,
    ContractVersionsPageComponent,
    ContractImportStepperComponent,
    SoapIntegrationSettingsComponent
  ].map(compiledStyles).join('\n');

  it('define comportamientos compacto <768, mediano 768-1199 y amplio por defecto', () => {
    const css = primaryStyles();

    expect(css).toMatch(/@media\s*\(max-width:\s*767px\)/);
    expect(css).toMatch(/@media\s*\(min-width:\s*768px\)\s*and\s*\(max-width:\s*1199px\)/);
    expect(css).toMatch(/repeat\(3,\s*minmax\(0,\s*1fr\)\)/);
  });

  it('mantiene targets táctiles, foco visible y copy largo sin depender de hover', () => {
    const css = primaryStyles();

    expect(css).toMatch(/min-height:\s*2\.75rem/);
    expect(css).toMatch(/min-height:\s*3rem/);
    expect(css).toContain(':focus-visible');
    expect(css).toMatch(/overflow-wrap:\s*anywhere/);
    expect(css).toMatch(/white-space:\s*normal/);
  });

  it('evita doble scroll interno y conserva reflow compatible con zoom', () => {
    const css = primaryStyles();
    const mappingEditorCss = compiledStyles(MappingEditorPageComponent);

    expect(mappingEditorCss).not.toMatch(/overflow\s*:\s*(auto|scroll)/);
    expect(mappingEditorCss).not.toMatch(/max-height\s*:\s*\d+px/);
    expect(css).toMatch(/minmax\(0,\s*1fr\)/);
    expect(css).toMatch(/\d+(?:\.\d+)?rem\b/);
  });

  it('convierte el stepper a cabeceras apiladas en compacto y no fuerza navegación horizontal', () => {
    const css = compiledStyles(ContractImportStepperComponent);

    expect(css).toContain('.mat-horizontal-stepper-header-container');
    expect(css).toMatch(/grid-template-columns:\s*1fr/);
    expect(css).toContain('.mat-stepper-horizontal-line');
    expect(css).toMatch(/display:\s*none/);
  });

  it('mantiene dialogs dentro del viewport con acciones accesibles', () => {
    const css = [
      compiledStyles(MappingPublicationDialogComponent),
      compiledStyles(RealSendConfirmationDialogComponent)
    ].join('\n');

    expect(css).toMatch(/max-width:\s*100%/);
    expect(css).toMatch(/min-height:\s*2\.75rem/);
    expect(css).toContain(':focus-visible');
  });

  it('protege branding extremo y textos técnicos largos en cards y tabs', () => {
    const css = [
      compiledStyles(IntegrationServiceCardComponent),
      compiledStyles(SoapIntegrationSettingsComponent),
      compiledStyles(MappingSetsPageComponent)
    ].join('\n');

    expect(css).toMatch(/overflow-wrap:\s*anywhere/);
    expect(css).not.toMatch(/text-overflow:\s*ellipsis/);
  });
});
