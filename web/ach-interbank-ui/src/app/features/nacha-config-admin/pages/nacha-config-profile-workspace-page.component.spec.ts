import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { BehaviorSubject, of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigProfileWorkspacePageComponent } from './nacha-config-profile-workspace-page.component';

describe('NachaConfigProfileWorkspacePageComponent', () => {
  let fixture: ComponentFixture<NachaConfigProfileWorkspacePageComponent>;
  let component: NachaConfigProfileWorkspacePageComponent;
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;
  let router: Router;
  let notificationsSpy: jasmine.SpyObj<NotificationService>;
  let authStub: { hasPermission: jasmine.Spy };
  let routeParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let queryDetailSpy: jasmine.Spy;
  let profileData = buildDetail('BORRADOR');

  beforeEach(async () => {
    profileData = buildDetail('BORRADOR');
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['editarBorrador', 'validar', 'publicar', 'inactivar', 'archivar', 'clonar']);
    notificationsSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error', 'info', 'warning']);
    authStub = { hasPermission: jasmine.createSpy().and.returnValue(true) };
    routeParams = new BehaviorSubject(convertToParamMap({ id: '1' }));
    queryDetailSpy = jasmine.createSpy('detalle').and.callFake((id: number) => of({
      ...profileData,
      id,
      profileCode: id === 1 ? profileData.profileCode : 'PW-LIVE-CLON'
    }));

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule],
      declarations: [NachaConfigProfileWorkspacePageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { paramMap: routeParams.asObservable() } },
        {
          provide: NachaConfigQueryService,
          useValue: {
            detalle: queryDetailSpy
          }
        },
        { provide: NachaConfigCommandService, useValue: commandSpy },
        { provide: NotificationService, useValue: notificationsSpy },
        { provide: AuthService, useValue: authStub }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture.detectChanges();
  });

  it('Component_ShouldCreate', () => {
    expect(component).toBeTruthy();
  });

  it('Component_ShouldRenderAdministrativeActionsForDraft', () => {
    expect(fixture.nativeElement.textContent).toContain('Guardar borrador');
    expect(fixture.nativeElement.textContent).toContain('Validar');
    expect(fixture.nativeElement.textContent).toContain('Publicar');
    expect(fixture.nativeElement.textContent).toContain('Clonar como borrador');
  });

  it('Component_ShouldHideSaveActionWhenProfileIsPublished', async () => {
    profileData = buildDetail('PUBLICADO');
    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Guardar borrador');
    expect(fixture.nativeElement.textContent).toContain('Clonar como borrador');
    expect(fixture.nativeElement.textContent).toContain('Inactivar');
    expect(fixture.nativeElement.textContent).toContain('Archivar');
  });

  it('Component_ShouldUpdateDraftMetadata', () => {
    component.editarForm.patchValue({
      nombreEs: 'Perfil editable',
      descripcion: 'Descripcion editable',
      contextPriority: 200,
      effectiveFrom: '2026-01-01',
      effectiveTo: '',
      expectedRowVersion: 'cm93'
    });
    commandSpy.editarBorrador.and.returnValue(of(profileData));

    component.guardarBorrador();

    expect(commandSpy.editarBorrador).toHaveBeenCalledWith(1, jasmine.objectContaining({
      nombreEs: 'Perfil editable',
      descripcion: 'Descripcion editable',
      contextPriority: 200,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      expectedRowVersion: 'cm93'
    }));
    expect(notificationsSpy.success).toHaveBeenCalled();
  });

  it('Component_ShouldValidateAndOpenPublicationConfirmation', () => {
    commandSpy.validar.and.returnValue(of({
      profileId: 1,
      isValid: true,
      erroresBloqueantes: 0,
      advertencias: 0,
      resumen: 'Perfil valido',
      issues: []
    }));

    component.publicarPerfil();

    expect(commandSpy.validar).toHaveBeenCalledWith(1);
    expect(component.modalAbierto).toBeTrue();
    expect(component.modalAccion).toBe('publicar');
  });

  it('Component_ShouldPublishAfterConfirmation', () => {
    commandSpy.validar.and.returnValue(of({
      profileId: 1,
      isValid: true,
      erroresBloqueantes: 0,
      advertencias: 0,
      resumen: 'Perfil valido',
      issues: []
    }));
    commandSpy.publicar.and.returnValue(of({
      profileId: 1,
      publicado: true,
      mensaje: 'Publicado',
      versionMajor: 1,
      versionMinor: 1,
      rowVersion: 'bmV3'
    }));

    component.publicarPerfil();
    component.confirmarModal();

    expect(commandSpy.publicar).toHaveBeenCalled();
    expect(notificationsSpy.success).toHaveBeenCalled();
  });

  it('Component_ShouldCloneProfileAndNavigateToCreatedDetail', () => {
    commandSpy.clonar.and.returnValue(of(buildDetail('BORRADOR')));
    component.cloneForm.patchValue({
      nuevoProfileCode: 'UAT-NACHA-CONFIG-CLONE-01',
      nuevoNombreEs: 'Perfil clonado',
      effectiveFrom: '2026-01-01',
      expectedRowVersion: 'cm93'
    });

    component.clonarPerfil();

    expect(commandSpy.clonar).toHaveBeenCalledWith(1, jasmine.objectContaining({
      nuevoProfileCode: 'UAT-NACHA-CONFIG-CLONE-01',
      nuevoNombreEs: 'Perfil clonado',
      effectiveFrom: '2026-01-01',
      expectedRowVersion: 'cm93'
    }));
    expect(router.navigate).toHaveBeenCalledWith(['/nacha-config-admin/perfiles', 1]);
  });

  it('Component_ShouldReloadWhenRouteChangesToClonedProfile', () => {
    routeParams.next(convertToParamMap({ id: '2' }));
    fixture.detectChanges();

    expect(component.perfilId).toBe(2);
    expect(queryDetailSpy).toHaveBeenCalledWith(2);
    expect(component.perfil?.profileCode).toBe('PW-LIVE-CLON');
  });

  it('Component_ShouldNotRenderAdminActionsWithoutManagePermission', async () => {
    authStub.hasPermission.and.returnValue(false);
    profileData = buildDetail('PUBLICADO');
    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Guardar borrador');
    expect(fixture.nativeElement.textContent).not.toContain('Clonar como borrador');
    expect(fixture.nativeElement.textContent).not.toContain('Publicar');
  });
});

function buildDetail(status: string) {
  return {
    id: 1,
    profileCode: 'OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0',
    nombreEs: 'Perfil oficial',
    descripcion: 'Descripcion oficial',
    estado: status,
    camara: 'ACH',
    flujo: 'ORIGINAL',
    direccion: 'SALIDA',
    servicio: 'PPD',
    versionMajor: 1,
    versionMinor: 0,
    contextPriority: 100,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    rowVersion: 'cm93',
    records: [
      { id: 1, recordCode: '6', sequence: 1, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'STATIC' }
    ],
    variantes: [
      {
        id: 1,
        recordCode: '6',
        variantCode: 'ACH_R6_BASE_V1',
        nombreEs: 'Base',
        priority: 1,
        isDefaultForRecord: true,
        totalLength: 106,
        fields: [
          {
            id: 10,
            fieldCode: 'AMOUNT',
            fieldNameEs: 'Amount',
            startPosition: 1,
            length: 10,
            propertyPath: 'AchTransaction.Amount',
            sourceType: 'Transaction',
            isEnabled: true,
            reglas: []
          }
        ]
      }
    ]
  };
}
