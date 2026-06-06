import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
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

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule],
      declarations: [NachaConfigProfilesPageComponent],
      providers: [
        {
          provide: NachaConfigQueryService,
          useValue: {
            dashboardReadOnly: () => of(dashboard),
            perfilesReadOnly: () => of([perfilReadOnly]),
            catalogosFiltro: () => of(catalogos)
          }
        },
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

  it('Component_ShouldRenderAdminCreateAndValidateActionsWhenManagePermissionExists', () => {
    expect(fixture.nativeElement.textContent).toContain('Crear borrador');
    expect(fixture.nativeElement.textContent).toContain('Validar');
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
