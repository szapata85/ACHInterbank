import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { MatSelect } from '@angular/material/select';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { BehaviorSubject, of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigContextBarComponent } from '../workspace/nacha-config-context-bar.component';
import { NachaConfigFieldEditorComponent } from '../workspace/nacha-config-field-editor.component';
import { NachaConfigFieldListComponent } from '../workspace/nacha-config-field-list.component';
import { NachaConfigLifecycleActionsComponent } from '../workspace/nacha-config-lifecycle-actions.component';
import { NachaConfigProfileMetadataComponent } from '../workspace/nacha-config-profile-metadata.component';
import { NachaConfigPositionRulerComponent } from '../workspace/nacha-config-position-ruler.component';
import { NachaConfigRecordListComponent } from '../workspace/nacha-config-record-list.component';
import { NachaConfigTestPanelComponent } from '../workspace/nacha-config-test-panel.component';
import { NachaConfigChangesPanelComponent } from '../workspace/nacha-config-changes-panel.component';
import { NachaConfigHistoryPanelComponent } from '../workspace/nacha-config-history-panel.component';
import { NachaConfigProfileWorkspacePageComponent } from './nacha-config-profile-workspace-page.component';

describe('NachaConfigProfileWorkspacePageComponent UX3', () => {
  let fixture: ComponentFixture<NachaConfigProfileWorkspacePageComponent>;
  let component: NachaConfigProfileWorkspacePageComponent;
  let querySpy: jasmine.SpyObj<NachaConfigQueryService>;
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;
  let router: Router;
  let routeParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let profile = buildWorkspace('BORRADOR');

  beforeEach(async () => {
    profile = buildWorkspace('BORRADOR');
    routeParams = new BehaviorSubject(convertToParamMap({ id: '11' }));
    querySpy = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['workspace', 'catalogosOperativos', 'editorCampo', 'cambios', 'historial']);
    querySpy.workspace.and.callFake((id: number) => of({ ...profile, id }));
    querySpy.catalogosOperativos.and.returnValue(of({
      estados: [{ code: 'BORRADOR', labelEs: 'Borrador' }],
      camaras: [{ code: 'ACH', labelEs: 'ACH Colombia' }],
      flujos: [{ code: 'ORIGINAL', labelEs: 'Original' }],
      direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }],
      servicios: [{ code: 'PPD', labelEs: 'Pago preacordado y depósito directo' }],
      registros: [{ code: '6', labelEs: 'Detalle de transacción' }],
      tiposOrigen: [{ code: 'Transaction', labelEs: 'Transacción' }],
      tiposRegla: []
    }));
    querySpy.cambios.and.returnValue(of({
      profileId: 11, hasComparisonBase: true, isEditable: true, rowVersion: 'cm93', currentVersion: '1.0', nextVersion: '1.1',
      camaraNombreEs: 'ACH Colombia', flujoNombreEs: 'Original', direccionNombreEs: 'Salida', servicioNombreEs: 'Pago preacordado y depósito directo',
      effectiveFrom: '2026-01-01', effectiveTo: null, changedFieldsCount: 0, changeCount: 0, summaryEs: 'Sin cambios', changes: []
    }));
    querySpy.historial.and.returnValue(of([]));
    querySpy.editorCampo.and.callFake((profileId: number, fieldId: number) => {
      const field = profile.variantes.flatMap((variant) => variant.fields).find((item) => item.id === fieldId)!;
      return of({
        profileId,
        fieldId,
        isEditable: profile.isEditable,
        rowVersion: profile.rowVersion,
        field,
        sourceReferenceCount: 1,
        sourceTypes: [{ code: 'ENTIDAD', labelEs: 'Dato del sistema', catalogLabelEs: 'Entidad del dominio', usesFixedValue: false, requiresDefinitionSelection: true }],
        sourceOptions: [{ sourceDefinitionId: field.sourceDefinitionId, sourceTypeCode: 'ENTIDAD', labelEs: `Dato del sistema → ${field.fieldNameEs}` }],
        alignments: [{ code: 'L', labelEs: 'Alinear a la izquierda' }, { code: 'R', labelEs: 'Alinear a la derecha' }],
        formats: [],
        severities: [{ code: 'ERROR', labelEs: 'Error bloqueante' }, { code: 'WARN', labelEs: 'Advertencia' }]
      });
    });
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', [
      'editarBorrador', 'validar', 'probar', 'publicar', 'inactivar', 'archivar', 'clonar', 'actualizarEditorCampo'
    ]);

    await TestBed.configureTestingModule({
      imports: [
        SharedModule,
        RouterTestingModule,
        MatButtonModule,
        MatCardModule,
        MatChipsModule,
        MatProgressSpinnerModule,
        MatTabsModule,
        NachaConfigContextBarComponent,
        NachaConfigRecordListComponent,
        NachaConfigFieldListComponent,
        NachaConfigPositionRulerComponent,
        NachaConfigFieldEditorComponent,
        NachaConfigProfileMetadataComponent,
        NachaConfigLifecycleActionsComponent,
        NachaConfigTestPanelComponent,
        NachaConfigChangesPanelComponent,
        NachaConfigHistoryPanelComponent
      ],
      declarations: [NachaConfigProfileWorkspacePageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: {
          paramMap: routeParams.asObservable(),
          snapshot: {
            queryParamMap: convertToParamMap({}),
            queryParams: {}
          }
        } },
        { provide: NachaConfigQueryService, useValue: querySpy },
        { provide: NachaConfigCommandService, useValue: commandSpy },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'warning', 'error']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture.detectChanges();
  });

  it('loads workspace and contextual catalogs from UX1 contracts', () => {
    expect(querySpy.workspace).toHaveBeenCalledWith(11);
    expect(querySpy.catalogosOperativos).toHaveBeenCalledWith('ACH', 'ORIGINAL');

    // These controls are read-only. Check Angular Material's bound values directly;
    // an asynchronous harness can wait for stabilization while the workspace is
    // still scheduling focus for its heading and exceed Jasmine's default timeout.
    const context = fixture.debugElement.query(By.directive(NachaConfigContextBarComponent));
    expect(context).not.toBeNull();
    const bar = context.componentInstance as NachaConfigContextBarComponent;
    expect(bar.form.controls.camara.value).toBe('ACH');
    expect(bar.form.controls.servicio.value).toBe('PPD');
    expect(bar.camaras).toContain(jasmine.objectContaining({ code: 'ACH', labelEs: 'ACH Colombia' }));
    expect(bar.servicios).toContain(jasmine.objectContaining({ code: 'PPD', labelEs: 'Pago preacordado y depósito directo' }));
    const selects = context.queryAll(By.directive(MatSelect)).map((item) => item.componentInstance as MatSelect);
    expect(selects.map((select) => select.value)).toEqual(['ACH', 'ORIGINAL', 'SALIDA', 'PPD']);
    expect(selects.every((select) => select.disabled)).toBeTrue();
  });

  it('shows humanized record, field list and basic field detail in one workspace', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Estructura del archivo');
    expect(text).toContain('Detalle de transacción');
    expect(text).toContain('Nombre del originador');
    expect(text).toContain('Posiciones del registro');
    expect(text).toContain('Editor del campo');
  });

  it('switches record and field context without leaving the workspace', () => {
    component.seleccionarRegistro('6');
    fixture.detectChanges();

    expect(component.selectedRecordCode).toBe('6');
    expect(component.selectedField?.fieldNameEs).toBe('Valor de la transacción');
    expect(router.navigate).toHaveBeenCalledWith([], jasmine.objectContaining({ replaceUrl: true }));
  });

  it('protects a published configuration against direct editing', () => {
    profile = buildWorkspace('PUBLICADO');
    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Configuración protegida');
    expect(text).not.toContain('Guardar borrador');
    expect(text).toContain('Clonar como borrador');
  });

  it('inherits runtime branding token without rebuilding the SPA', () => {
    const root = document.documentElement;
    const workspace = fixture.nativeElement.querySelector('.workspace-brand-accent') as HTMLElement;

    root.style.setProperty('--mat-sys-primary', '#123456');
    fixture.detectChanges();
    expect(getComputedStyle(workspace).borderTopColor).toBe('rgb(18, 52, 86)');

    root.style.setProperty('--mat-sys-primary', '#654321');
    fixture.detectChanges();
    expect(getComputedStyle(workspace).borderTopColor).toBe('rgb(101, 67, 33)');

    root.style.removeProperty('--mat-sys-primary');
  });
});

function buildWorkspace(status: string) {
  return {
    id: 11,
    profileCode: 'OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0',
    nombreEs: 'Perfil operativo ACH',
    descripcion: 'Configuración operacional',
    estado: status,
    estadoNombre: status === 'PUBLICADO' ? 'Publicado' : 'Borrador',
    isEditable: status === 'BORRADOR',
    camara: 'ACH',
    camaraNombre: 'ACH Colombia',
    flujo: 'ORIGINAL',
    flujoNombre: 'Original',
    direccion: 'SALIDA',
    direccionNombre: 'Salida',
    servicio: 'PPD',
    servicioNombre: 'Pago preacordado y depósito directo',
    versionMajor: 1,
    versionMinor: 0,
    contextPriority: 100,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    rowVersion: 'cm93',
    records: [
      { id: 1, recordCode: '6', recordNameEs: 'Detalle de transacción', sequence: 2, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'STATIC', sourceStrategyName: 'Estático' },
      { id: 2, recordCode: '5', recordNameEs: 'Encabezado de lote', sequence: 1, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'STATIC', sourceStrategyName: 'Estático' }
    ],
    variantes: [
      {
        id: 20,
        recordCode: '6',
        variantCode: 'ACH_R6_BASE_V1',
        nombreEs: 'Detalle base',
        priority: 1,
        isDefaultForRecord: true,
        totalLength: 106,
        fields: [{
          id: 101,
          fieldCode: 'AMOUNT',
          fieldNameEs: 'Valor de la transacción',
          startPosition: 30,
          length: 10,
          sourceType: 'ENTIDAD',
          sourceTypeName: 'Entidad del dominio',
          sourceDefinitionId: 501,
          isEnabled: true,
          reglas: []
        }]
      },
      {
        id: 21,
        recordCode: '5',
        variantCode: 'ACH_R5_BASE_V1',
        nombreEs: 'Encabezado base',
        priority: 1,
        isDefaultForRecord: true,
        totalLength: 106,
        fields: [{
          id: 102,
          fieldCode: 'ORIGINATOR_NAME',
          fieldNameEs: 'Nombre del originador',
          startPosition: 5,
          length: 16,
          sourceType: 'ENTIDAD',
          sourceTypeName: 'Entidad del dominio',
          sourceDefinitionId: 502,
          isEnabled: true,
          reglas: []
        }]
      }
    ]
  };
}
