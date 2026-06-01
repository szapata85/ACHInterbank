import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaRecordDefinitionDto } from '../models/nacha-record-definition.model';
import { NachaRecordLayoutDto } from '../models/nacha-layout.model';
import { NachaLayoutsService } from '../services/nacha-layouts.service';
import { NachaRecordDefinitionsService } from '../services/nacha-record-definitions.service';
import { NachaLayoutsComponent } from './nacha-layouts.component';
import { NachaRecordDefinitionsComponent } from './nacha-record-definitions.component';

const layout: NachaRecordLayoutDto = {
  id: 1,
  recordCode: '6',
  recordType: 'Entry Detail',
  totalLength: 94,
  description: 'Detalle de transacción',
  fields: [
    {
      id: 10,
      fieldName: 'TraceNumber',
      startPosition: 80,
      length: 15,
      padChar: '0',
      justification: 'R',
      dbColumn: 'TraceNumber',
      format: null
    }
  ]
};

const definition: NachaRecordDefinitionDto = {
  id: 2,
  recordCode: '6',
  sequence: 30,
  sourceType: 1,
  sourceName: 'AchTransaction',
  filterKey: 'EntryDetail',
  isEnabled: true
};

describe('NACHA-M layouts y definiciones fase 4', () => {
  afterEach(() => TestBed.resetTestingModule());

  async function createLayoutsFixture(options?: { layoutsError?: boolean; definitionsError?: boolean; layouts?: NachaRecordLayoutDto[] }) {
    const layoutsService = jasmine.createSpyObj<NachaLayoutsService>('NachaLayoutsService', ['getAll', 'create', 'update', 'delete']);
    const definitionsService = jasmine.createSpyObj<NachaRecordDefinitionsService>('NachaRecordDefinitionsService', ['getAll']);

    layoutsService.getAll.and.returnValue(options?.layoutsError ? throwError(() => new Error('api')) : of(options?.layouts ?? [layout]));
    definitionsService.getAll.and.returnValue(options?.definitionsError ? throwError(() => new Error('api')) : of([definition]));

    await TestBed.configureTestingModule({
      imports: [NachaLayoutsComponent],
      providers: [
        { provide: NachaLayoutsService, useValue: layoutsService },
        { provide: NachaRecordDefinitionsService, useValue: definitionsService },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error', 'info']) },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(NachaLayoutsComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, layoutsService };
  }

  async function createDefinitionsFixture(options?: { definitionsError?: boolean; layoutsError?: boolean; definitions?: NachaRecordDefinitionDto[] }) {
    const definitionsService = jasmine.createSpyObj<NachaRecordDefinitionsService>('NachaRecordDefinitionsService', ['getAll', 'create', 'update', 'delete']);
    const layoutsService = jasmine.createSpyObj<NachaLayoutsService>('NachaLayoutsService', ['getAll']);
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    definitionsService.getAll.and.returnValue(options?.definitionsError ? throwError(() => new Error('api')) : of(options?.definitions ?? [definition]));
    definitionsService.create.and.returnValue(of(definition));
    definitionsService.update.and.returnValue(of(definition));
    definitionsService.delete.and.returnValue(of(void 0));
    layoutsService.getAll.and.returnValue(options?.layoutsError ? throwError(() => new Error('api')) : of([layout]));

    await TestBed.configureTestingModule({
      imports: [NachaRecordDefinitionsComponent],
      providers: [
        { provide: NachaRecordDefinitionsService, useValue: definitionsService },
        { provide: NachaLayoutsService, useValue: layoutsService },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error', 'info']) },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(NachaRecordDefinitionsComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, definitionsService, router };
  }

  it('NachaLayouts_ShouldRenderHeader', async () => {
    const { fixture } = await createLayoutsFixture();

    expect(fixture.nativeElement.textContent).toContain('Layouts NACHA-M legacy');
    expect(fixture.nativeElement.textContent).toContain('LEGACY / Deprecated');
    expect(fixture.nativeElement.textContent).toContain('Total layouts');
  });

  it('NachaLayouts_ShouldShowEmptyState_WhenNoData', async () => {
    const { fixture, component } = await createLayoutsFixture({ layouts: [] });
    fixture.detectChanges();

    expect(component.layouts).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('No hay layouts configurados');
    expect(fixture.nativeElement.textContent).toContain('perfiles NACHA Config');
  });

  it('NachaLayouts_ShouldShowError_WhenApiFails', async () => {
    const { fixture, component } = await createLayoutsFixture({ layoutsError: true });
    fixture.detectChanges();

    expect(component.loadError).toContain('No fue posible cargar');
    expect(component.loading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('No fue posible cargar layouts NACHA-M');
  });

  it('LegacyScreen_ShouldBeReadOnly_WhenLayoutsKeptForDiagnostics', async () => {
    const { fixture, component, layoutsService } = await createLayoutsFixture();

    expect(component.columns.find((column) => column.key === 'recordType')?.width).toBe('240px');
    expect(component.totalFields).toBe(1);
    expect(fixture.nativeElement.textContent).not.toContain('Nuevo layout');
    expect(fixture.nativeElement.textContent).not.toContain('Eliminar');

    component.startCreate();
    component.startEdit(layout);
    component.remove(layout);

    expect(layoutsService.create).not.toHaveBeenCalled();
    expect(layoutsService.update).not.toHaveBeenCalled();
    expect(layoutsService.delete).not.toHaveBeenCalled();
  });

  it('NachaDefinitions_ShouldRenderHeader', async () => {
    const { fixture } = await createDefinitionsFixture();

    expect(fixture.nativeElement.textContent).toContain('Definiciones NACHA-M legacy');
    expect(fixture.nativeElement.textContent).toContain('LEGACY / Deprecated');
    expect(fixture.nativeElement.textContent).toContain('Total definiciones');
  });

  it('NachaDefinitions_ShouldShowEmptyState_WhenNoData', async () => {
    const { fixture, component } = await createDefinitionsFixture({ definitions: [] });
    fixture.detectChanges();

    expect(component.definitions).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('No hay definiciones configuradas');
    expect(fixture.nativeElement.textContent).toContain('perfiles NACHA Config');
  });

  it('NachaDefinitions_ShouldShowError_WhenApiFails', async () => {
    const { fixture, component } = await createDefinitionsFixture({ definitionsError: true });
    fixture.detectChanges();

    expect(component.loadError).toContain('No fue posible cargar');
    expect(component.loading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('No fue posible cargar definiciones NACHA-M');
  });

  it('LegacyScreen_ShouldBeReadOnly_WhenDefinitionsKeptForDiagnostics', async () => {
    const { fixture, component, definitionsService } = await createDefinitionsFixture();

    component.startEdit(definition);
    component.startCreate();
    component.remove(definition);
    fixture.detectChanges();

    expect(component.editorOpen).toBeFalse();
    expect(fixture.nativeElement.querySelector('[data-testid="nacha-definition-edit-modal"]')).toBeFalsy();
    expect(fixture.nativeElement.textContent).not.toContain('Nueva definición');
    expect(fixture.nativeElement.textContent).not.toContain('Eliminar');
    expect(definitionsService.create).not.toHaveBeenCalled();
    expect(definitionsService.update).not.toHaveBeenCalled();
    expect(definitionsService.delete).not.toHaveBeenCalled();
  });

  it('LegacyScreen_ShouldShowDeprecatedBanner_WhenAccessible', async () => {
    const { fixture, component } = await createDefinitionsFixture();

    fixture.detectChanges();

    expect(component.legacyReadOnly).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('El modelo oficial NACHA-M es nacha-config profiles');
    expect(fixture.nativeElement.textContent).toContain('Abrir perfiles NACHA oficiales');
  });

  it('NachaDefinitions_EditModal_ShouldCloseOnCancel', async () => {
    const { component } = await createDefinitionsFixture();

    component.startEdit(definition);
    component.cancel();

    expect(component.editorOpen).toBeFalse();
    expect(component.editing).toBeNull();
  });

  it('NachaDefinitions_EditModal_ShouldShowValidationErrors', async () => {
    const { fixture, component } = await createDefinitionsFixture();

    component.startCreate();
    component.save();
    fixture.detectChanges();

    expect(component.editorOpen).toBeFalse();
    expect(component.form.invalid).toBeTrue();
  });

  it('NachaDefinitions_EditModal_ShouldNotSaveInLegacyReadOnlyMode', async () => {
    const { component, definitionsService } = await createDefinitionsFixture();

    component.startEdit(definition);
    component.save();

    expect(definitionsService.update).not.toHaveBeenCalled();
    expect(definitionsService.getAll).toHaveBeenCalledTimes(1);
    expect(component.editorOpen).toBeFalse();
  });

  it('LegacyDefinitionsRoute_ShouldNotShowMutableActions', async () => {
    const { fixture, component } = await createDefinitionsFixture();

    component.startEdit(definition);
    fixture.detectChanges();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const criticalButtons = buttons.filter((button) => /Guardar|Editar|Eliminar/.test(button.textContent ?? ''));

    expect(criticalButtons.length).toBe(0);
  });
});
