import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import {
  IntegrationContractVersionDetailDto,
  IntegrationMethodDto
} from '../data-access/integration-api.models';
import { IntegrationContractVersionReadModel } from '../data-access/integration-read.models';
import { ContractWorkspaceStore } from '../state/contract-workspace.store';
import { ContractVersionsPageComponent } from './contract-versions-page.component';

describe('ContractVersionsPageComponent', () => {
  let fixture: ComponentFixture<ContractVersionsPageComponent>;
  let store: FakeContractWorkspaceStore;

  beforeEach(async () => {
    store = new FakeContractWorkspaceStore();
    await TestBed.configureTestingModule({
      imports: [ContractVersionsPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: ContractWorkspaceStore, useValue: store },
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: of(convertToParamMap({ method: 'Proc_Contrapartidas' })),
            queryParams: of({ method: 'Proc_Contrapartidas' }),
            params: of({}),
            url: of([]),
            snapshot: {
              queryParamMap: convertToParamMap({ method: 'Proc_Contrapartidas' }),
              queryParams: { method: 'Proc_Contrapartidas' },
              params: {},
              url: []
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ContractVersionsPageComponent);
    fixture.detectChanges();
  });

  it('lista versiones por Borrador, Activa y Retirada con versión, revisión y relación de información', () => {
    store.listStatus.set('ready');
    store.versionRows.set([
      version('draft', 'v3', 'Draft', 'Borrador', false, true, 'Relación en borrador'),
      version('active', 'v2', 'Active', 'Activa', true, false, 'Relación publicada'),
      version('retired', 'v1', 'Retired', 'Retirada', true, false, 'Relación archivada')
    ]);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Borrador');
    expect(text).toContain('Activa');
    expect(text).toContain('Retirada');
    expect(text).toContain('Versión v3');
    expect(text).toContain('Revisión 4');
    expect(text).toContain('Relación publicada');
  });

  it('muestra detalle con metadata funcional, revisión y actor/fecha entregados por backend', () => {
    const active = version('active', 'v2', 'Active', 'Activa', true, false, 'Relación publicada');
    store.listStatus.set('ready');
    store.detailStatus.set('ready');
    store.selectedVersion.set(active);
    store.detail.set(detail('Active'));
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Débitos originados en CFA');
    expect(text).toContain('Origina débitos hacia CFA.');
    expect(text).toContain('Versión v2');
    expect(text).toContain('Revisión');
    expect(text).toContain('operador.activa');
    expect(text).toContain('Cuenta destino');
    expect(text).toContain('Cuenta funcional de destino.');
    expect(text).toContain('Relación publicada');
  });

  it('permite editar metadata funcional de Draft solo cuando backend y permisos lo habilitan', () => {
    const draft = version('draft', 'v3', 'Draft', 'Borrador', false, true, 'Relación en borrador');
    store.listStatus.set('ready');
    store.detailStatus.set('ready');
    store.selectedVersion.set(draft);
    store.detail.set(detail('Draft'));
    store.permissions.set({ canRead: true, canManage: true });
    fixture.detectChanges();

    const editButton = fixture.debugElement.queryAll(By.css('button'))
      .find((item) => (item.nativeElement.textContent as string).includes('Editar metadatos funcionales'));
    expect(editButton).toBeDefined();
    editButton!.nativeElement.click();
    fixture.detectChanges();

    const name = fixture.debugElement.query(By.css('input[formControlName="displayName"]')).nativeElement as HTMLInputElement;
    name.value = 'Cuenta beneficiaria';
    name.dispatchEvent(new Event('input'));
    const form = fixture.debugElement.query(By.css('form.functional-editor'));
    form.triggerEventHandler('ngSubmit', {});

    expect(store.saveParameterPresentation).toHaveBeenCalledWith(1, jasmine.objectContaining({
      displayName: 'Cuenta beneficiaria'
    }));
  });

  it('mantiene Active y Retired solo lectura aunque el usuario tenga CanManageAch', () => {
    for (const state of ['Active', 'Retired'] as const) {
      store.detail.set(detail(state));
      store.selectedVersion.set(version(state.toLowerCase(), 'v1', state, state === 'Active' ? 'Activa' : 'Retirada', true, false, 'Relación publicada'));
      store.detailStatus.set('ready');
      store.permissions.set({ canRead: true, canManage: true });
      fixture.detectChanges();

      const text = fixture.nativeElement.textContent as string;
      expect(text).toContain('solo lectura');
      expect(text).not.toContain('Editar metadatos funcionales');
    }
  });

  it('no muestra edición de Draft cuando falta permiso de administración', () => {
    store.detail.set(detail('Draft'));
    store.selectedVersion.set(version('draft', 'v3', 'Draft', 'Borrador', false, true, 'Relación en borrador'));
    store.detailStatus.set('ready');
    store.permissions.set({ canRead: true, canManage: false });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Editar metadatos funcionales');
  });

  it('mantiene el detalle técnico y XSD colapsados por defecto', () => {
    store.detail.set(detail('Draft'));
    store.selectedVersion.set(version('draft', 'v3', 'Draft', 'Borrador', false, true, 'Relación en borrador'));
    store.detailStatus.set('ready');
    fixture.detectChanges();

    const contractDetails = fixture.debugElement.query(By.css('[data-testid="contract-technical-details"]')).nativeElement as HTMLDetailsElement;
    const parameterDetails = fixture.debugElement.query(By.css('.parameter-card__technical')).nativeElement as HTMLDetailsElement;
    expect(contractDetails.open).toBeFalse();
    expect(parameterDetails.open).toBeFalse();
    expect(contractDetails.querySelector('summary')?.textContent).toContain('Detalle técnico avanzado');
  });

  it('presenta estados loading, error, empty y forbidden de forma explícita y accesible', () => {
    const cases = [
      ['loading', 'Cargando versiones contractuales'],
      ['error', 'No fue posible cargar las versiones'],
      ['empty', 'Sin versiones contractuales'],
      ['forbidden', 'Permisos insuficientes']
    ] as const;

    for (const [status, expected] of cases) {
      store.detail.set(null);
      store.listStatus.set(status);
      store.listError.set(status === 'error' || status === 'forbidden'
        ? { kind: status === 'forbidden' ? 'forbidden' : 'http', status: status === 'forbidden' ? 403 : 500, message: 'Mensaje backend' }
        : null);
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain(expected);
    }
  });

  it('usa controles nativos enfocables y semántica de detalle accesible por teclado', () => {
    store.detail.set(detail('Draft'));
    store.selectedVersion.set(version('draft', 'v3', 'Draft', 'Borrador', false, true, 'Relación en borrador'));
    store.detailStatus.set('ready');
    store.permissions.set({ canRead: true, canManage: true });
    fixture.detectChanges();

    const summary = fixture.debugElement.query(By.css('.parameter-card__technical summary')).nativeElement as HTMLElement;
    const editButton = fixture.debugElement.queryAll(By.css('button'))
      .find((item) => (item.nativeElement.textContent as string).includes('Editar metadatos funcionales'))!.nativeElement as HTMLButtonElement;

    summary.focus();
    expect(document.activeElement).toBe(summary);
    editButton.focus();
    expect(document.activeElement).toBe(editButton);
    expect(editButton.type).toBe('button');
    expect(fixture.debugElement.query(By.css('section[aria-labelledby="contract-detail-title"]'))).not.toBeNull();
  });

  function version(
    id: string,
    versionLabel: string,
    displayState: IntegrationContractVersionReadModel['displayState'],
    statusLabel: string,
    isReadOnly: boolean,
    canEditFunctionalMetadata: boolean,
    relationshipLabel: string
  ): IntegrationContractVersionReadModel {
    return {
      id,
      versionLabel,
      backendStatus: displayState === 'Draft' ? 'Draft' : displayState,
      displayState,
      statusLabel,
      revision: 4,
      isReadOnly,
      canEditFunctionalMetadata,
      notes: 'Versión funcional',
      sourceType: 'ManualApproved',
      sourceLabel: 'Versión administrada',
      importedAtUtc: '2026-10-13T10:00:00Z',
      importedBy: 'operador.crea',
      activatedAtUtc: displayState === 'Draft' ? null : '2026-10-13T11:00:00Z',
      activatedBy: displayState === 'Draft' ? null : 'operador.activa',
      relationship: {
        mappingSetId: 'mapping-1',
        mappingRevision: 2,
        status: displayState === 'Retired' ? 'Archived' : displayState === 'Active' ? 'Published' : 'Draft',
        label: relationshipLabel
      }
    };
  }

  function detail(status: 'Draft' | 'Active' | 'Retired'): IntegrationContractVersionDetailDto {
    return {
      contract: {
        id: `${status.toLowerCase()}-contract`,
        methodId: 3,
        versionLabel: status === 'Draft' ? 'v3' : 'v2',
        status,
        operationName: 'Proc_Contrapartidas',
        targetNamespace: 'urn:test',
        soapAction: 'urn:test/action',
        sourceType: 'ManualApproved',
        notes: 'Notas funcionales.',
        revision: 4,
        importedAtUtc: '2026-10-13T10:00:00Z',
        importedBy: 'operador.crea',
        activatedAtUtc: status === 'Draft' ? null : '2026-10-13T11:00:00Z',
        activatedBy: status === 'Draft' ? null : 'operador.activa',
        capabilities: {
          isReadOnly: status !== 'Draft',
          canEditFunctionalMetadata: status === 'Draft'
        }
      },
      parameters: [{
        id: 1,
        contractVersionId: `${status.toLowerCase()}-contract`,
        methodId: 3,
        parameterPath: 'CUENTA',
        xmlElementName: 'CUENTA',
        displayName: 'Cuenta destino',
        descriptionEs: 'Cuenta funcional de destino.',
        uiHelpText: 'Seleccione la cuenta autorizada.',
        category: 'Cuenta',
        exampleValue: '****1234',
        dataType: 'string',
        xsdType: 'xsd:string',
        direction: 'Input',
        cardinality: 'Scalar',
        required: true,
        nillable: false,
        minOccurs: 1,
        maxOccurs: 1,
        supportStatus: 'Supported',
        sortOrder: 1,
        isActive: true
      }]
    };
  }
  it('usa cards/listas en lugar de tablas y anuncia estados dinámicos', () => {
    store.listStatus.set('loading');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    const live = fixture.nativeElement.querySelector('[role="status"][aria-live="polite"]') as HTMLElement;
    expect(live).not.toBeNull();
    expect(live.textContent).toContain('Cargando versiones contractuales');
  });

});

class FakeContractWorkspaceStore {
  readonly methodId = signal<number | null>(3);
  readonly method = signal<IntegrationMethodDto | null>({
    id: 3,
    code: 'PROC_CONTRAPARTIDAS',
    displayName: 'Débitos originados en CFA',
    soapClientCode: 'WSCFAACH',
    isActive: true,
    integrationKey: 'CFA',
    operationKey: 'Proc_Contrapartidas',
    mappingDirection: 'Outbound',
    mappingPurpose: 'Origina débitos hacia CFA.',
    functionalNature: 'Debit',
    functionalOriginator: 'Internal',
    movesMoney: true
  });
  readonly versionRows = signal<IntegrationContractVersionReadModel[]>([]);
  readonly listStatus = signal<'idle' | 'loading' | 'ready' | 'empty' | 'error' | 'forbidden'>('idle');
  readonly detailStatus = signal<'idle' | 'loading' | 'ready' | 'empty' | 'error' | 'forbidden'>('idle');
  readonly detail = signal<IntegrationContractVersionDetailDto | null>(null);
  readonly selectedVersion = signal<IntegrationContractVersionReadModel | null>(null);
  readonly listError = signal<any>(null);
  readonly detailError = signal<any>(null);
  readonly parameterSaveError = signal<any>(null);
  readonly parameterSaveStatus = signal<'idle' | 'loading' | 'ready' | 'empty' | 'error' | 'forbidden'>('idle');
  readonly permissions = signal({ canRead: true, canManage: true });
  readonly parameters = signal<any[]>([{
    parameterId: 1,
    functionalName: 'Cuenta destino',
    functionalDescription: 'Cuenta funcional de destino.',
    technicalName: 'CUENTA',
    required: true,
    categoryLabel: 'Cuenta',
    status: null,
    sourceSummary: null,
    transformationSummary: null,
    exampleValue: '****1234',
    uiHelpText: 'Seleccione la cuenta autorizada.',
    technicalDetails: {
      xmlElementName: 'CUENTA',
      dataType: 'string',
      xsdType: 'xsd:string',
      direction: 'Input',
      cardinality: 'Scalar',
      nillable: false,
      minOccurs: 1,
      maxOccurs: 1,
      supportStatus: 'Supported'
    }
  }]);

  loadForOperation = jasmine.createSpy('loadForOperation');
  loadDetail = jasmine.createSpy('loadDetail');
  clearDetail = jasmine.createSpy('clearDetail').and.callFake(() => {
    this.detail.set(null);
    this.detailStatus.set('idle');
  });
  reload = jasmine.createSpy('reload');
  resetArtifactImport = jasmine.createSpy('resetArtifactImport');
  clearParameterSaveState = jasmine.createSpy('clearParameterSaveState').and.callFake(() => {
    this.parameterSaveStatus.set('idle');
    this.parameterSaveError.set(null);
  });
  saveParameterPresentation = jasmine.createSpy('saveParameterPresentation');
}
