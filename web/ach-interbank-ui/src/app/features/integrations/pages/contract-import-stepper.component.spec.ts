import { signal } from '@angular/core';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatStepperHarness } from '@angular/material/stepper/testing';
import {
  IntegrationContractArtifactAnalysisDto,
  IntegrationContractArtifactImportResultDto,
  IntegrationContractArtifactPreviewDto,
  IntegrationContractVersionDto
} from '../data-access/integration-api.models';
import { ContractWorkspaceStore } from '../state/contract-workspace.store';
import { ContractImportStepperComponent } from './contract-import-stepper.component';

describe('ContractImportStepperComponent', () => {
  let fixture: ComponentFixture<ContractImportStepperComponent>;
  let component: ContractImportStepperComponent;
  let store: FakeContractWorkspaceStore;
  let loader: HarnessLoader;

  beforeEach(async () => {
    store = new FakeContractWorkspaceStore();
    await TestBed.configureTestingModule({
      imports: [ContractImportStepperComponent, NoopAnimationsModule],
      providers: [{ provide: ContractWorkspaceStore, useValue: store }]
    }).compileComponents();

    fixture = TestBed.createComponent(ContractImportStepperComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  it('falla cerrado dentro de los métodos mutables cuando falta capacidad de gestión', () => {
    store.permissions.set({ canRead: true, canManage: false });
    store.selectArtifact(new File(['<definitions/>'], 'contrato.wsdl', { type: 'text/xml' }));
    store.artifactAnalysis.set(analysis({ importable: true }));
    store.artifactAnalysisStatus.set('ready');
    store.artifactPreview.set(preview());
    store.artifactPreviewStatus.set('ready');
    component.selectedBaseContractId.set('base');
    component.diffReviewed.set(true);
    component.importForm.setValue({ versionLabel: 'v-bloqueada', notes: '' });
    component.operationForm.setValue({ operationName: 'Proc_Transacciones', standaloneXsdSoapAction: 'urn:test/Proc_Transacciones' });

    component.analyzeSelectedFile();
    component.validateOperation();
    component.previewDiff();
    component.importDraft();

    expect(store.analyzeArtifact).not.toHaveBeenCalled();
    expect(store.previewArtifact).not.toHaveBeenCalled();
    expect(store.importArtifactDraft).not.toHaveBeenCalled();
  });

  it('selecciona únicamente un archivo local y delega el análisis al backend', () => {
    const file = new File(['<definitions/>'], 'contrato.wsdl', { type: 'text/xml' });

    component.onFileSelected({ target: { files: { item: () => file } } } as unknown as Event);
    component.analyzeSelectedFile();

    expect(store.selectArtifact).toHaveBeenCalledWith(file);
    expect(store.analyzeArtifact).toHaveBeenCalledWith();
    expect(store.previewArtifact).not.toHaveBeenCalled();
    expect(store.importArtifactDraft).not.toHaveBeenCalled();
  });

  it('permite continuar cuando backend detecta una operación única importable', () => {
    store.selectArtifact(new File(['wsdl'], 'unica.wsdl'));
    component.analyzeSelectedFile();
    store.artifactAnalysis.set(analysis({ importable: true, operationName: 'Proc_Transacciones', availableOperations: ['Proc_Transacciones'] }));
    store.artifactAnalysisStatus.set('ready');
    fixture.detectChanges();

    expect(component.currentStep()).toBe(1);
    expect(component.analysisValid()).toBeTrue();
    expect(component.operationForm.controls.operationName.value).toBe('Proc_Transacciones');

    component.continueToSummary();
    expect(component.currentStep()).toBe(2);
  });

  it('obliga a seleccionar y reanalizar cuando backend reporta varias operaciones compatibles', () => {
    store.selectArtifact(new File(['wsdl'], 'multiple.wsdl'));
    component.analyzeSelectedFile();
    store.artifactAnalysis.set(analysis({
      importable: false,
      operationName: null,
      availableOperations: ['OperacionA', 'OperacionB'],
      issues: [{ code: 'WSDL_OPERATION_AMBIGUOUS', message: 'Seleccione una operación compatible.' }]
    }));
    store.artifactAnalysisStatus.set('ready');
    fixture.detectChanges();

    expect(component.operationCandidates()).toEqual(['OperacionA', 'OperacionB']);
    expect(component.analysisValid()).toBeFalse();

    component.operationForm.controls.operationName.setValue('OperacionB');
    component.validateOperation();
    expect(store.analyzeArtifact).toHaveBeenCalledWith('OperacionB', null);

    store.artifactAnalysis.set(analysis({ importable: true, operationName: 'OperacionB', availableOperations: ['OperacionB'] }));
    fixture.detectChanges();
    expect(component.analysisValid()).toBeTrue();
  });

  it('mantiene el flujo XSD standalone gobernado por metadata backend/base y reanálisis backend', () => {
    const file = new File(['<xs:schema/>'], 'contrato.xsd', { type: 'application/xml' });
    store.selectArtifact(file);
    component.analyzeSelectedFile();
    store.artifactAnalysis.set(analysis({
      artifactKind: 'XmlSchema',
      importable: false,
      operationName: null,
      soapAction: null,
      availableOperations: [],
      issues: [{ code: 'XSD_SOAP_METADATA_REQUIRED', message: 'Confirme operación y SOAP Action.' }]
    }));
    store.artifactAnalysisStatus.set('ready');
    fixture.detectChanges();

    expect(component.isStandaloneXsd()).toBeTrue();
    expect(component.operationForm.controls.operationName.value).toBe('Proc_Transacciones');
    expect(component.operationForm.controls.standaloneXsdSoapAction.value).toBe('urn:test/Proc_Transacciones');

    component.validateOperation();
    expect(store.analyzeArtifact).toHaveBeenCalledWith('Proc_Transacciones', 'urn:test/Proc_Transacciones');
  });

  it('muestra el resumen de contrato y mantiene hash/namespace/SOAP Action colapsados', () => {
    store.artifactAnalysis.set(analysis({ importable: true, operationName: 'Proc_Transacciones' }));
    store.artifactAnalysisStatus.set('ready');
    component.currentStep.set(2);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Resumen del contrato detectado');
    expect(text).toContain('contrato.wsdl');
    expect(text).toContain('Datos detectados');
    const details = fixture.debugElement.query(By.css('.technical-details')).nativeElement as HTMLDetailsElement;
    expect(details.open).toBeFalse();
    expect(details.textContent).toContain('Hash SHA-256');
  });

  it('presenta MappingRequired como Requiere relacionar y RequiresDevelopment como Requiere desarrollo sin reclasificar', () => {
    store.artifactAnalysis.set(analysis({ importable: true }));
    store.artifactAnalysisStatus.set('ready');
    store.artifactPreview.set(preview());
    store.artifactPreviewStatus.set('ready');
    component.currentStep.set(3);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Requiere relacionar');
    expect(text).toContain('Requiere desarrollo');
    expect(text).toContain('Dato nuevo');
    expect(text).toContain('Cambio de namespace');

    const badges = fixture.debugElement.queryAll(By.css('.impact-badge'));
    expect(badges.map((badge) => badge.attributes['data-impact'])).toEqual(['MappingRequired', 'RequiresDevelopment']);
  });

  it('humaniza el error de análisis y bloquea el avance cuando backend lo marca inválido', () => {
    store.artifactAnalysis.set(analysis({
      importable: false,
      operationName: null,
      availableOperations: [],
      issues: [{ code: 'EXTERNAL_REFERENCE_NOT_ALLOWED', message: 'El documento referencia recursos externos no permitidos.' }]
    }));
    store.artifactAnalysisStatus.set('ready');
    component.currentStep.set(1);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('El documento referencia recursos externos no permitidos.');
    component.continueToSummary();
    expect(component.currentStep()).toBe(1);

    const continueButton = fixture.debugElement.queryAll(By.css('button'))
      .find((button) => (button.nativeElement.textContent as string).trim() === 'Continuar')!.nativeElement as HTMLButtonElement;
    expect(continueButton.disabled).toBeTrue();
  });

  it('solicita importación exclusivamente como nueva versión Borrador', () => {
    store.artifactAnalysis.set(analysis({ importable: true, operationName: 'Proc_Transacciones' }));
    store.artifactAnalysisStatus.set('ready');
    store.artifactPreview.set(preview());
    store.artifactPreviewStatus.set('ready');
    component.currentStep.set(4);
    component.importForm.setValue({ versionLabel: 'v-nueva', notes: 'Cambio funcional' });
    fixture.detectChanges();
    component.diffReviewed.set(true);

    component.importDraft();

    expect(store.importArtifactDraft).toHaveBeenCalledWith(
      'v-nueva',
      'Cambio funcional',
      'Proc_Transacciones',
      null
    );

    store.artifactImportResult.set(importResult('Draft'));
    store.artifactImportStatus.set('ready');
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Borrador creado');
    expect(text).toContain('estado Draft');
    expect(text).not.toContain('estado Active');
  });

  it('no permite saltar al diff sin preview válido del backend', () => {
    store.artifactAnalysis.set(analysis({ importable: true }));
    component.currentStep.set(2);
    component.continueToConfirmation();
    expect(component.currentStep()).toBe(2);

    store.artifactPreviewStatus.set('error');
    store.artifactPreview.set({ analysis: analysis({ importable: true }), diff: null });
    component.continueToConfirmation();
    expect(component.currentStep()).toBe(2);
  });

  it('invalida el avance si cambia la versión base después del diff', () => {
    store.artifactAnalysis.set(analysis({ importable: true }));
    store.artifactAnalysisStatus.set('ready');
    store.artifactPreview.set(preview());
    store.artifactPreviewStatus.set('ready');
    component.selectBaseVersion('otra-base');

    expect(component.previewMatchesBase()).toBeFalse();
    component.currentStep.set(2);
    component.continueToConfirmation();
    expect(component.currentStep()).toBe(2);
  });

  it('mantiene controles de archivo y detalle técnico accesibles por teclado', async () => {
    fixture.detectChanges();
    const input = fixture.debugElement.query(By.css('input[type="file"]')).nativeElement as HTMLInputElement;
    input.focus();
    expect(document.activeElement).toBe(input);
    expect(input.getAttribute('aria-describedby')).toBe('artifact-file-help');

    // Establish the backend-ready state before navigating. MatStepper is
    // linear: the first step must have its [completed] input rendered before
    // a header selection is accepted. The automatic-analysis transition is
    // exercised separately by the operation-analysis specs above.
    store.selectArtifact(new File(['<definitions/>'], 'contrato.wsdl', { type: 'text/xml' }));
    store.artifactAnalysis.set(analysis({ importable: true }));
    store.artifactAnalysisStatus.set('ready');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    // Exercise an actual Material step-header interaction instead of writing
    // the component's signal and racing the completion input in the same pass.
    const materialStepper = await loader.getHarness(MatStepperHarness);
    const materialSteps = await materialStepper.getSteps();
    await materialSteps[1].select();
    fixture.detectChanges();
    const stepper = fixture.debugElement.query(By.css('mat-stepper')).componentInstance;
    expect(component.currentStep()).toBe(1);
    expect(stepper.selectedIndex).toBe(1);
    expect(await materialSteps[1].isSelected()).toBeTrue();

    component.continueToSummary();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(component.currentStep()).toBe(2);
    expect(stepper.selectedIndex).toBe(2);

    const summary = fixture.debugElement.query(
      By.css('section[aria-labelledby="contract-summary-title"] .technical-details summary')
    ).nativeElement as HTMLElement;
    summary.focus();
    expect(document.activeElement).toBe(summary);
  });

  function analysis(overrides: Partial<IntegrationContractArtifactAnalysisDto> = {}): IntegrationContractArtifactAnalysisDto {
    return {
      fileName: 'contrato.wsdl',
      artifactKind: 'Wsdl11',
      sizeBytes: 2048,
      sha256: 'abc123',
      operationName: 'Proc_Transacciones',
      targetNamespace: 'urn:test',
      soapAction: 'urn:test/Proc_Transacciones',
      availableOperations: ['Proc_Transacciones'],
      importable: true,
      issues: [],
      parameters: [{
        parameterPath: 'CUENTA',
        xmlElementName: 'CUENTA',
        displayName: 'Cuenta destino',
        dataType: 'string',
        xsdType: 'string',
        direction: 'Input',
        cardinality: 'Scalar',
        required: true,
        nillable: false,
        minOccurs: 1,
        maxOccurs: 1,
        supportStatus: 'Supported',
        sortOrder: 1
      }],
      ...overrides
    };
  }

  function preview(): IntegrationContractArtifactPreviewDto {
    return {
      analysis: analysis(),
      diff: {
        leftContractVersionId: 'base',
        rightContractVersionId: '00000000-0000-0000-0000-000000000000',
        methodId: 3,
        summary: { added: 1, removed: 0, modified: 1, requiresMapping: 1, requiresDevelopment: 1 },
        changes: [
          {
            changeType: 'Added',
            technicalName: 'NUEVO',
            functionalName: 'Dato nuevo funcional',
            impact: 'MappingRequired',
            leftValue: null,
            rightValue: 'string'
          },
          {
            changeType: 'NamespaceChanged',
            technicalName: '$contract',
            functionalName: 'Namespace SOAP',
            impact: 'RequiresDevelopment',
            leftValue: 'urn:old',
            rightValue: 'urn:new'
          }
        ]
      }
    };
  }

  function importResult(status: 'Draft' | 'Active'): IntegrationContractArtifactImportResultDto {
    return {
      analysis: analysis(),
      contract: {
        contract: { ...baseVersion, id: 'new-draft', versionLabel: 'v-nueva', status, revision: 1 },
        parameters: []
      }
    };
  }
  it('expone el stepper Material por Harness con orden funcional y primer paso seleccionado', async () => {
    const stepper = await loader.getHarness(MatStepperHarness);
    const steps = await stepper.getSteps();

    expect(await Promise.all(steps.map(step => step.getLabel()))).toEqual([
      'Archivo local',
      'Operación',
      'Resumen',
      'Cambios',
      'Crear borrador'
    ]);
    expect(await steps[0].isSelected()).toBeTrue();
    expect(await steps[1].isSelected()).toBeFalse();
  });

  it('mantiene flujo por pasos sin tabla primaria y con anuncios dinámicos', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('table')).toBeNull();

    const input = fixture.nativeElement.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['<definitions/>'], 'contrato.wsdl', { type: 'text/xml' });
    Object.defineProperty(input, 'files', { value: [file] });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.selected-file[aria-live="polite"]')).not.toBeNull();
  });

});

const baseVersion: IntegrationContractVersionDto = {
  id: 'base',
  methodId: 3,
  versionLabel: 'v1',
  status: 'Active',
  operationName: 'Proc_Transacciones',
  targetNamespace: 'urn:test',
  soapAction: 'urn:test/Proc_Transacciones',
  sourceType: 'ManualApproved',
  revision: 4,
  capabilities: { isReadOnly: true, canEditFunctionalMetadata: false }
};

class FakeContractWorkspaceStore {
  readonly permissions = signal({ canRead: true, canManage: true });
  readonly artifactFile = signal<File | null>(null);
  readonly artifactAnalysis = signal<IntegrationContractArtifactAnalysisDto | null>(null);
  readonly artifactPreview = signal<IntegrationContractArtifactPreviewDto | null>(null);
  readonly artifactImportResult = signal<IntegrationContractArtifactImportResultDto | null>(null);
  readonly artifactAnalysisStatus = signal<any>('idle');
  readonly artifactPreviewStatus = signal<any>('idle');
  readonly artifactImportStatus = signal<any>('idle');
  readonly artifactAnalysisError = signal<any>(null);
  readonly artifactPreviewError = signal<any>(null);
  readonly artifactImportError = signal<any>(null);
  readonly versions = signal<IntegrationContractVersionDto[]>([baseVersion]);
  readonly activeContract = signal<IntegrationContractVersionDto | null>(baseVersion);

  selectArtifact = jasmine.createSpy('selectArtifact').and.callFake((file: File | null) => {
    this.artifactFile.set(file);
    this.artifactAnalysis.set(null);
    this.artifactPreview.set(null);
    this.artifactImportResult.set(null);
    this.artifactAnalysisStatus.set('idle');
    this.artifactPreviewStatus.set('idle');
    this.artifactImportStatus.set('idle');
  });
  analyzeArtifact = jasmine.createSpy('analyzeArtifact');
  previewArtifact = jasmine.createSpy('previewArtifact');
  importArtifactDraft = jasmine.createSpy('importArtifactDraft');
  resetArtifactImport = jasmine.createSpy('resetArtifactImport');
}
