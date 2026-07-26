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
    servicios: [{ code: 'PPD', labelEs: 'PPD' }]
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
      'catalogosFiltro'
    ]);
    querySpy.dashboardReadOnly.and.returnValue(of(dashboard));
    querySpy.perfilesReadOnly.and.returnValue(of([perfilReadOnly]));
    querySpy.catalogosFiltro.and.returnValue(of(catalogos));

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule],
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
    querySpy.catalogosFiltro.and.returnValue(pendingCatalogs.asObservable());

    component.cargarCatalogos(true);
    fixture.detectChanges();

    expect(component.crearForm.disabled).toBeTrue();
    expect(component.catalogosCargando).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Cargando catálogos requeridos');
  });

  it('bloquea creación y muestra reintento cuando fallan los catálogos', () => {
    querySpy.catalogosFiltro.and.returnValue(
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
    querySpy.catalogosFiltro.and.returnValues(
      throwError(() => new HttpErrorResponse({ status: 429, statusText: 'Too Many Requests' })),
      of(recoveredCatalogs)
    );

    component.cargarCatalogos(true);
    component.reintentarCatalogos();
    fixture.detectChanges();

    expect(component.catalogosEstado).toBe('recuperados');
    expect(component.crearForm.enabled).toBeTrue();
    expect(component.opcionesCamara.map((option) => option.valor)).toContain('JOB6TEST');
    component.crearForm.controls.camaraCode.setValue('JOB6TEST');
    expect(component.crearForm.controls.camaraCode.value).toBe('JOB6TEST');
  });

  it('no crea un perfil cuando los catálogos no están disponibles', () => {
    querySpy.catalogosFiltro.and.returnValue(
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
