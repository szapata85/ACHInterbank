import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Subject, of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigChangesPanelComponent } from './nacha-config-changes-panel.component';

describe('NachaConfigChangesPanelComponent UX5', () => {
  let fixture: ComponentFixture<NachaConfigChangesPanelComponent>;
  let component: NachaConfigChangesPanelComponent;
  let query: jasmine.SpyObj<NachaConfigQueryService>;
  let command: jasmine.SpyObj<NachaConfigCommandService>;
  let dialog: jasmine.SpyObj<MatDialog>;

  beforeEach(async () => {
    query = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['cambios']);
    command = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['probar', 'publicar']);
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    query.cambios.and.returnValue(of(buildReview()));
    command.probar.and.returnValue(of(buildValidation(true)));
    command.publicar.and.returnValue(of({
      profileId: 11, publicado: true, mensaje: 'Publicado', versionMajor: 2, versionMinor: 1,
      estado: 'PUBLICADO', estadoNombreEs: 'Publicado', rowVersion: 'bmV3'
    }));
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as never);

    await TestBed.configureTestingModule({
      imports: [NachaConfigChangesPanelComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: query },
        { provide: NachaConfigCommandService, useValue: command },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'warning', 'error']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigChangesPanelComponent);
    component = fixture.componentInstance;
    (component as any).dialog = dialog;
    fixture.componentRef.setInput('perfil', buildWorkspace());
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  });

  it('muestra solo cambios reales con comparación antes y después', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Posición inicial');
    expect(text).toContain('13');
    expect(text).toContain('14');
    expect(text).toContain('Número de cuenta');
    expect(text).not.toContain('PropertyPath');
  });

  it('soporta diff vacío sin inventar cambios', () => {
    query.cambios.and.returnValue(of({ ...buildReview(), changeCount: 0, changedFieldsCount: 0, summaryEs: 'Sin cambios', changes: [] }));
    const refresh = fixture.nativeElement.querySelector('.panel-heading button') as HTMLButtonElement;
    refresh.click();
    fixture.detectChanges();
    expect(component.review?.changes).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('Sin cambios reales');
  });

  it('valida de nuevo, confirma y publica con el RowVersion fresco', fakeAsync(() => {
    let refreshed = 0;
    component.recargar.subscribe(() => refreshed += 1);

    component.publicar();
    tick();

    expect(command.probar).toHaveBeenCalledOnceWith(11);
    expect(query.cambios).toHaveBeenCalledTimes(2);
    expect(dialog.open).toHaveBeenCalled();
    expect(command.publicar).toHaveBeenCalledWith(11, 'cm93');
    expect(refreshed).toBe(1);
    expect(component.publicationResult?.estadoNombreEs).toBe('Publicado');
  }));

  it('bloquea publicación cuando la prueba reporta errores', () => {
    command.probar.and.returnValue(of(buildValidation(false)));
    component.publicar();
    expect(command.publicar).not.toHaveBeenCalled();
  });

  it('evita doble publicación mientras la validación está en curso', () => {
    const pending = new Subject<ReturnType<typeof buildValidation>>();
    command.probar.and.returnValue(pending.asObservable());
    component.publicar();
    component.publicar();
    expect(command.probar).toHaveBeenCalledTimes(1);
  });
});

function buildReview() {
  return {
    profileId: 11,
    baseProfileId: 7,
    hasComparisonBase: true,
    isEditable: true,
    rowVersion: 'cm93',
    currentVersion: '2.0',
    nextVersion: '2.1',
    camaraNombreEs: 'ACH Colombia',
    flujoNombreEs: 'Original',
    direccionNombreEs: 'Salida',
    servicioNombreEs: 'Pago preacordado y depósito directo',
    effectiveFrom: '2026-10-11',
    effectiveTo: null,
    changedFieldsCount: 1,
    changeCount: 1,
    summaryEs: 'Un cambio real.',
    changes: [{
      scope: 'FIELD', changeKind: 'MODIFIED', elementNameEs: 'Número de cuenta', recordCode: '6', recordNameEs: 'Detalle de transacción',
      fieldId: 101, fieldNameEs: 'Número de cuenta', propertyNameEs: 'Posición inicial', beforeValueEs: '13', afterValueEs: '14', summaryEs: 'Posición inicial: 13 → 14'
    }]
  };
}

function buildValidation(canPublish: boolean) {
  return {
    profileId: 11,
    processDateUtc: '2026-10-11T00:00:00Z',
    canPublish,
    resultLabelEs: canPublish ? 'Apto para publicar' : 'Requiere correcciones',
    erroresBloqueantes: canPublish ? 0 : 1,
    advertencias: 0,
    issues: [],
    selection: { success: canPublish, summaryEs: '', warningsEs: [], technicalTrace: [] },
    records: []
  };
}

function buildWorkspace() {
  return {
    id: 11, profileCode: 'P', nombreEs: 'Perfil', estado: 'BORRADOR', estadoNombre: 'Borrador', isEditable: true,
    camara: 'ACH', camaraNombre: 'ACH Colombia', flujo: 'ORIGINAL', flujoNombre: 'Original', direccion: 'SALIDA', direccionNombre: 'Salida',
    servicio: 'PPD', servicioNombre: 'PPD', versionMajor: 2, versionMinor: 0, contextPriority: 100, effectiveFrom: '2026-10-11', effectiveTo: null,
    rowVersion: 'cm93', records: [], variantes: []
  };
}
