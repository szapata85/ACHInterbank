import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigProfilesPageComponent } from './nacha-config-profiles-page.component';

describe('NachaConfigProfilesPageComponent', () => {
  let fixture: ComponentFixture<NachaConfigProfilesPageComponent>;
  let component: NachaConfigProfilesPageComponent;
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;
  let router: Router;
  let notificationsSpy: jasmine.SpyObj<NotificationService>;
  let authStub: { hasPermission: jasmine.Spy };
  let querySpy: jasmine.SpyObj<NachaConfigQueryService>;

  const catalogos = {
    estados: [{ code: 'BORRADOR', labelEs: 'Borrador' }],
    camaras: [{ code: 'ACH', labelEs: 'ACH Colombia' }],
    flujos: [{ code: 'ORIGINAL', labelEs: 'Original' }],
    direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }],
    servicios: [{ code: 'PPD', labelEs: 'PPD' }],
    registros: [{ code: '6', labelEs: 'Detalle de transacciones' }],
    tiposOrigen: [{ code: 'ORIGINAL', labelEs: 'Original' }],
    tiposRegla: [{ code: 'ESTRUCTURA', labelEs: 'Estructura' }]
  };

  const dashboard = {
    productiveStatus: 'NO-GO',
    isOfficialModel: true,
    legacyDeprecated: true,
    profileCount: 1,
    publishedProfileCount: 1,
    currentProfileCount: 1,
    layoutVariantCount: 6,
    fieldCount: 20,
    clearingHouses: ['ACH'],
    recordTypes: ['1', '5', '6', '7', '8', '9'],
    warnings: []
  };

  const perfilReadOnly = {
    profileId: 1,
    profileCode: 'OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0',
    profileName: 'Perfil oficial',
    clearingHouseCode: 'ACH',
    flowType: 'ORIGINAL',
    status: 'BORRADOR',
    version: 'v1.0',
    isPublished: false,
    isCurrent: true,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    layoutVariantCount: 6,
    fieldCount: 20,
    recordTypes: ['1', '5', '6', '7', '8', '9'],
    isOfficialModel: true,
    legacyDeprecated: true
  };

  beforeEach(async () => {
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['crearBorrador', 'validar']);
    notificationsSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error', 'info', 'warning']);
    authStub = { hasPermission: jasmine.createSpy().and.returnValue(true) };
    querySpy = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', [
      'dashboardReadOnly',
      'perfilesReadOnly',
      'catalogosOperativos'
    ]);
    querySpy.dashboardReadOnly.and.returnValue(of(dashboard));
    querySpy.perfilesReadOnly.and.returnValue(of([perfilReadOnly]));
    querySpy.catalogosOperativos.and.returnValue(of(catalogos));

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule, MatFormFieldModule, MatSelectModule],
      declarations: [NachaConfigProfilesPageComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: querySpy },
        { provide: NachaConfigCommandService, useValue: commandSpy },
        { provide: NotificationService, useValue: notificationsSpy },
        { provide: AuthService, useValue: authStub }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfilesPageComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture.detectChanges();
  });

  it('Component_ShouldCreate', () => {
    expect(component).toBeTruthy();
  });

  it('Component_ShouldRenderAdminCreateActionWhenManagePermissionExists', () => {
    expect(fixture.nativeElement.textContent).toContain('Crear borrador');
  });

  it('Component_ShouldHideAdminActionsWhenOnlyReadPermissionExists', async () => {
    authStub.hasPermission.and.returnValue(false);
    fixture = TestBed.createComponent(NachaConfigProfilesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Crear borrador');
    expect(fixture.nativeElement.textContent).not.toContain('Validar');
  });

  it('Component_ShouldCreateDraftAndNavigateToWorkspace', () => {
    commandSpy.crearBorrador.and.returnValue(of({ id: 99, profileCode: 'UAT-NACHA-CONFIG-99' } as any));
    component.crearForm.patchValue({
      profileCode: 'UAT-NACHA-CONFIG-99',
      nombreEs: 'Perfil UAT',
      descripcion: 'Descripcion UAT',
      camaraCode: 'ACH',
      flujoCode: 'ORIGINAL',
      direccionCode: 'SALIDA',
      servicioCode: 'PPD',
      effectiveFrom: '2026-01-01'
    });

    component.crearBorrador();

    expect(commandSpy.crearBorrador).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/nacha-config-admin/perfiles', 99]);
  });

  it('mantiene el formulario deshabilitado mientras cargan los catálogos', () => {
    const pendingCatalogs = new Subject<typeof catalogos>();
    querySpy.catalogosOperativos.and.returnValue(pendingCatalogs.asObservable());

    component.cargarCatalogos(true);
    fixture.detectChanges();

    expect(component.crearForm.disabled).toBeTrue();
    expect(component.catalogosCargando).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Cargando catálogos requeridos');
  });

  it('bloquea creación y muestra reintento cuando fallan los catálogos', () => {
    querySpy.catalogosOperativos.and.returnValue(
      throwError(() => new HttpErrorResponse({
        status: 429,
        statusText: 'Too Many Requests',
        headers: new HttpHeaders({ 'Retry-After': '1' })
      }))
    );

    component.cargarCatalogos(true);
    fixture.detectChanges();

    expect(component.crearForm.disabled).toBeTrue();
    expect(component.catalogosDisponibles).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('procesando varias solicitudes');
    expect(fixture.nativeElement.textContent).toContain('Reintentar catálogos');
  });

  it('reconstruye opciones y habilita el formulario después de recuperar catálogos', () => {
    const recoveredCatalogs = {
      ...catalogos,
      camaras: [...catalogos.camaras, { code: 'JOB6TEST', labelEs: 'Red sintética JOB 6' }]
    };
    let attempt = 0;
    querySpy.catalogosOperativos.and.callFake(() => {
      attempt += 1;
      return attempt === 1
        ? throwError(() => new HttpErrorResponse({ status: 429, statusText: 'Too Many Requests' }))
        : of(recoveredCatalogs);
    });

    component.cargarCatalogos(true);
    component.reintentarCatalogos();
    fixture.detectChanges();

    expect(component.catalogosEstado).toBe('recuperados');
    expect(component.crearForm.enabled).toBeTrue();
    expect(component.opcionesCamara.map((option) => option.valor)).toContain('JOB6TEST');
    component.crearForm.controls.camaraCode.setValue('JOB6TEST');
    expect(component.crearForm.controls.camaraCode.value).toBe('JOB6TEST');
  });

  it('maneja catálogos vacíos sin habilitar una creación inválida', () => {
    querySpy.catalogosOperativos.and.returnValue(of({
      estados: [],
      camaras: [],
      flujos: [],
      direcciones: [],
      servicios: [],
      registros: [],
      tiposOrigen: [],
      tiposRegla: []
    }));

    component.cargarCatalogos(true);
    fixture.detectChanges();

    expect(component.catalogosEstado).toBe('vacios');
    expect(component.crearForm.disabled).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('No existen cámaras, flujos y direcciones activas');
  });

  it('no crea un perfil cuando los catálogos no están disponibles', () => {
    querySpy.catalogosOperativos.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 503, statusText: 'Service Unavailable' }))
    );
    component.cargarCatalogos(true);
    component.crearForm.patchValue({
      profileCode: 'JOB6-NO-CATALOGS',
      nombreEs: 'Perfil bloqueado',
      camaraCode: 'JOB6TEST',
      flujoCode: 'ORIGINAL',
      direccionCode: 'SALIDA',
      effectiveFrom: '2026-07-24'
    });

    component.crearBorrador();

    expect(commandSpy.crearBorrador).not.toHaveBeenCalled();
  });

  it('usa catálogos contextuales del backend al seleccionar cámara y flujo', () => {
    const initial = { ...catalogos, servicios: [{ code: 'PPD', labelEs: 'Servicio global' }] };
    const ach = { ...catalogos, servicios: [{ code: 'ACH_ONLY', labelEs: 'Servicio ACH' }] };
    const achFlow = {
      ...ach,
      direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }]
    };
    querySpy.catalogosOperativos.and.callFake((camaraCode?: string | null, flujoCode?: string | null) => {
      if (camaraCode === 'ACH' && flujoCode === 'ORIGINAL') {
        return of(achFlow);
      }
      if (camaraCode === 'ACH') {
        return of(ach);
      }
      return of(initial);
    });

    component.cargarCatalogos();
    component.crearForm.controls.camaraCode.setValue('ACH');
    component.crearForm.controls.flujoCode.setValue('ORIGINAL');
    fixture.detectChanges();

    expect(querySpy.catalogosOperativos).toHaveBeenCalledWith('ACH', 'ORIGINAL', jasmine.any(Function));
    expect(component.opcionesServicio.map((option) => option.valor)).toEqual(['ACH_ONLY']);
    expect(component.opcionesDireccion.map((option) => option.valor)).toEqual(['SALIDA']);
    expect(component.crearForm.controls.direccionCode.value).toBe('SALIDA');
  });

  it('Component_ShouldValidateProfileAndStoreValidationResult', () => {
    commandSpy.validar.and.returnValue(of({
      profileId: 1,
      isValid: false,
      erroresBloqueantes: 1,
      advertencias: 2,
      resumen: 'Perfil con observaciones',
      issues: [{ severidad: 'ERROR', codigo: 'NACHA-001', mensaje: 'Falta record control.' }]
    }));

    component.validarPerfil(perfilReadOnly as any);

    expect(commandSpy.validar).toHaveBeenCalledWith(1);
    expect(component.validationResult?.isValid).toBeFalse();
    expect(component.validationProfile?.profileCode).toBe('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0');
    expect(notificationsSpy.warning).toHaveBeenCalled();
  });

  it('Component_ShouldNotInvokeAdminActionsWithoutManagePermission', () => {
    authStub.hasPermission.and.returnValue(false);
    fixture = TestBed.createComponent(NachaConfigProfilesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(commandSpy.crearBorrador).not.toHaveBeenCalled();
    expect(commandSpy.validar).not.toHaveBeenCalled();
  });
});
