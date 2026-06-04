import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  NachaConfigProfileDetail,
  NachaConfigProfileReadModel
} from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigRecordsPageComponent } from './nacha-config-records-page.component';

describe('NachaConfigRecordsPageComponent', () => {
  let fixture: ComponentFixture<NachaConfigRecordsPageComponent>;
  let component: NachaConfigRecordsPageComponent;
  let querySpy: jasmine.SpyObj<NachaConfigQueryService>;
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;
  let router: Router;
  let notificationsSpy: jasmine.SpyObj<NotificationService>;
  let authStub: { hasPermission: jasmine.Spy };

  const profiles: NachaConfigProfileReadModel[] = [
    {
      profileId: 11,
      profileCode: 'UAT-NACHA-CONFIG-RECORDS-001',
      profileName: 'Perfil borrador',
      clearingHouseCode: 'ACH',
      flowType: 'BORRADOR',
      status: 'BORRADOR',
      version: '1.0',
      isPublished: false,
      isCurrent: true,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      layoutVariantCount: 6,
      fieldCount: 20,
      recordTypes: ['1', '5', '6', '7', '8', '9'],
      isOfficialModel: true,
      legacyDeprecated: false
    },
    {
      profileId: 12,
      profileCode: 'UAT-NACHA-CONFIG-RECORDS-002',
      profileName: 'Perfil publicado',
      clearingHouseCode: 'ACH',
      flowType: 'ORIGINAL',
      status: 'PUBLICADO',
      version: '1.1',
      isPublished: true,
      isCurrent: true,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      layoutVariantCount: 6,
      fieldCount: 20,
      recordTypes: ['1', '5', '6', '7', '8', '9'],
      isOfficialModel: true,
      legacyDeprecated: false
    }
  ];

  const draftDetail: NachaConfigProfileDetail = {
    id: 11,
    profileCode: 'UAT-NACHA-CONFIG-RECORDS-001',
    nombreEs: 'Perfil borrador',
    descripcion: 'Perfil editable',
    estado: 'BORRADOR',
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
      { id: 101, recordCode: '1', sequence: 10, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' },
      { id: 102, recordCode: '5', sequence: 20, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' },
      { id: 103, recordCode: '6', sequence: 30, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' },
      { id: 104, recordCode: '7', sequence: 40, isEnabled: false, minOccurs: 0, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' },
      { id: 105, recordCode: '8', sequence: 50, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' },
      { id: 106, recordCode: '9', sequence: 60, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' }
    ],
    variantes: []
  };

  const publishedDetail: NachaConfigProfileDetail = {
    ...draftDetail,
    id: 12,
    profileCode: 'UAT-NACHA-CONFIG-RECORDS-002',
    nombreEs: 'Perfil publicado',
    estado: 'PUBLICADO',
    versionMinor: 1,
    rowVersion: 'cm93Mg=='
  };

  beforeEach(async () => {
    querySpy = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['perfilesReadOnly', 'detalle']);
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['actualizarSecuencia']);
    notificationsSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error', 'warning', 'info']);
    authStub = { hasPermission: jasmine.createSpy().and.returnValue(true) };

    querySpy.perfilesReadOnly.and.returnValue(of(profiles));
    querySpy.detalle.and.returnValue(of(draftDetail));
    commandSpy.actualizarSecuencia.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule, NachaConfigRecordsPageComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: querySpy },
        { provide: NachaConfigCommandService, useValue: commandSpy },
        { provide: NotificationService, useValue: notificationsSpy },
        { provide: AuthService, useValue: authStub }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigRecordsPageComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture.detectChanges();
  });

  it('Component_ShouldCreate', () => {
    expect(component).toBeTruthy();
  });

  it('Component_ShouldLoadProfilesAndDraftRecords', () => {
    expect(querySpy.perfilesReadOnly).toHaveBeenCalled();
    expect(querySpy.detalle).toHaveBeenCalledWith(11);
    expect(component.selectedProfile?.profileCode).toBe('UAT-NACHA-CONFIG-RECORDS-001');
    expect(component.records.length).toBe(6);
    expect(fixture.nativeElement.textContent).toContain('NACHA Config - Records');
    expect(fixture.nativeElement.textContent).toContain('UAT-NACHA-CONFIG-RECORDS-001');
    expect(fixture.nativeElement.textContent).toContain('Secuencia');
    expect(fixture.nativeElement.textContent).toContain('TABLE_DRIVEN');
  });

  it('Component_ShouldAllowSaveOnlyForDraftAndManagePermission', () => {
    expect(fixture.nativeElement.textContent).toContain('Guardar secuencia');
    component.onSequenceChange(component.records[0], { target: { value: '99' } } as unknown as Event);

    component.guardarSecuencia();

    expect(commandSpy.actualizarSecuencia).toHaveBeenCalledWith(11, {
      expectedRowVersion: 'cm93',
      records: [
        { profileRecordId: 101, sequence: 99 },
        { profileRecordId: 102, sequence: 20 },
        { profileRecordId: 103, sequence: 30 },
        { profileRecordId: 104, sequence: 40 },
        { profileRecordId: 105, sequence: 50 },
        { profileRecordId: 106, sequence: 60 }
      ]
    });
    expect(notificationsSpy.success).toHaveBeenCalled();
  });

  it('Component_ShouldHideSaveForPublishedProfiles', () => {
    querySpy.detalle.and.returnValue(of(publishedDetail));
    fixture = TestBed.createComponent(NachaConfigRecordsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.selectedProfile?.estado).toBe('PUBLICADO');
    expect(fixture.nativeElement.textContent).not.toContain('Guardar secuencia');
  });

  it('Component_ShouldHideSaveWithoutManagePermission', () => {
    authStub.hasPermission.and.returnValue(false);
    fixture = TestBed.createComponent(NachaConfigRecordsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Guardar secuencia');
    expect(fixture.nativeElement.textContent).toContain('Solo lectura');
  });

  it('Component_ShouldSurfaceConcurrencyErrorWhenSavingSequence', () => {
    commandSpy.actualizarSecuencia.and.returnValue(throwError(() => ({
      message: 'El perfil fue modificado por otro usuario.',
      issues: [{ severidad: 'ERROR', codigo: 'CONCURRENCY_CONFLICT', mensaje: 'Concurrencia detectada.' }]
    })));

    component.guardarSecuencia();

    expect(notificationsSpy.error).toHaveBeenCalled();
    expect(component.saveError).toContain('modificado por otro usuario');
    expect(component.saveIssues.length).toBe(1);
  });

  it('Component_ShouldNavigateToOfficialVariantsAndProfileRoutes', () => {
    component.irADetallePerfil();
    expect(router.navigate).toHaveBeenCalledWith(['/nacha-config-admin/perfiles', 11]);

    component.irAVariantsFields();
    expect(router.navigate).toHaveBeenCalledWith(['/nacha-config-admin/variants-fields']);
  });
});
