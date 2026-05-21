import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  IntegrationMethod
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { MappingSetsPageComponent } from './mapping-sets-page.component';

describe('MappingSetsPageComponent', () => {
  let fixture: ComponentFixture<MappingSetsPageComponent>;
  let component: MappingSetsPageComponent;
  let api: jasmine.SpyObj<IntegrationMappingAdminService>;
  let router: jasmine.SpyObj<Router>;

  const methods: IntegrationMethod[] = [
    {
      id: 1,
      code: 'WSCFAACH.Proc_Contrapartidas',
      displayName: 'Proc_Contrapartidas',
      soapClientCode: 'WscfaachSoapClient',
      isActive: true
    },
    {
      id: 2,
      code: 'WSAXON.RegistrarRespuestaTransaccion',
      displayName: 'RegistrarRespuestaTransaccion',
      soapClientCode: 'WsAxonRespuestaTransaccionesSoapClient',
      isActive: true
    }
  ];

  const mappingSets: IntegrationMappingSet[] = [
    {
      id: 'mapping-1',
      methodId: 1,
      methodCode: 'WSCFAACH.Proc_Contrapartidas',
      name: 'Contrapartidas UAT',
      version: 1,
      status: 'Published',
      isActive: true,
      notes: 'Version controlada',
      publishedAtUtc: null,
      publishedBy: 'qa',
      rules: []
    }
  ];

  beforeEach(async () => {
    api = jasmine.createSpyObj<IntegrationMappingAdminService>('IntegrationMappingAdminService', [
      'getMethods',
      'getMappingSets',
      'createDraft'
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    api.getMethods.and.returnValue(of(methods));
    api.getMappingSets.and.callFake((methodId?: number) => of(methodId === 2 ? [] : mappingSets));
    api.createDraft.and.returnValue(of(mappingSets[0]));

    await TestBed.configureTestingModule({
      imports: [MappingSetsPageComponent],
      providers: [
        { provide: IntegrationMappingAdminService, useValue: api },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MappingSetsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga opciones de integracion incluyendo WsAxonRespuestaTransaccionesSoapClient', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('WsAxonRespuestaTransaccionesSoapClient');
    expect(fixture.nativeElement.querySelector('[data-testid="integration-select"]')).toBeTruthy();
  });

  it('permite seleccionar WsAxon y muestra estado vacio claro cuando no hay mappings', () => {
    component.createDraftForm.patchValue({ methodId: 2 });
    component.onMethodChange();
    fixture.detectChanges();

    expect(api.getMappingSets).toHaveBeenCalledWith(2);
    expect(fixture.nativeElement.querySelector('[data-testid="empty-mappings-state"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('No hay mappings para la integracion seleccionada.');
  });

  it('abre detalle read-only y modal de edicion guiada', () => {
    component.openDetail(mappingSets[0]);
    expect(component.modalMode).toBe('detail');
    expect(component.selectedMapping?.name).toBe('Contrapartidas UAT');

    component.closeModal();
    component.openEdit(mappingSets[0]);
    expect(component.modalMode).toBe('edit');
    expect(component.selectedMapping?.name).toBe('Contrapartidas UAT');
  });

  it('muestra acciones visibles por card sin grilla principal', () => {
    expect(fixture.nativeElement.querySelector('ui-grilla-empresarial')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-card"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-detail-button"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-edit-button"]')).toBeTruthy();
  });
});
