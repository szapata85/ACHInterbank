import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { provideRouter } from '@angular/router';
import { IntegrationServiceReadModel } from '../data-access/integration-read.models';
import { IntegrationServiceCardComponent } from './integration-service-card.component';

describe('IntegrationServiceCardComponent', () => {
  let fixture: ComponentFixture<IntegrationServiceCardComponent>;
  let component: IntegrationServiceCardComponent;

  const service: IntegrationServiceReadModel = {
    methodId: 1,
    functionalTitle: 'Créditos recibidos en CFA',
    functionalDescription: 'Procesa los créditos recibidos por CFA.',
    technicalMethodName: 'Proc_Transacciones',
    movesMoney: true,
    contractVersionLabel: 'v3',
    contractStatus: 'Active',
    mappingStatus: 'Published',
    mappingCoverage: {
      totalParameters: 12,
      validParameters: 10,
      incompleteParameters: 2,
      invalidParameters: 0,
      inactiveParameters: 0,
      coveredByDefaultOrFixed: 3,
      coveredBySourceField: 7
    },
    connectionStatus: 'Live',
    canEditMapping: null,
    canPublish: null,
    canManageContract: null,
    canManageConnection: null
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IntegrationServiceCardComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(IntegrationServiceCardComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('service', service);
  });

  it('prioriza nombre funcional y deja el método SOAP como dato secundario', () => {
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Créditos recibidos en CFA');
    expect(text).toContain('Procesa los créditos recibidos por CFA.');
    expect(text).toContain('Nombre técnico');
    expect(text).toContain('Proc_Transacciones');
  });

  it('muestra contrato, cobertura y conexión con lenguaje visible en español', () => {
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('v3');
    expect(text).toContain('Activo');
    expect(text).toContain('Publicada');
    expect(text).toContain('10 de 12 datos validados');
    expect(text).toContain('Envío habilitado');
    expect(text).not.toContain('Published');
    expect(text).not.toContain('Live');
  });

  it('navega desde la tarjeta con contexto del servicio hacia las rutas funcionales', () => {
    fixture.componentRef.setInput('canManage', true);
    fixture.detectChanges();

    const links = fixture.debugElement.queryAll(By.directive(RouterLink));
    const urls = links.map((link) => link.injector.get(RouterLink).urlTree.toString());

    expect(urls).toContain('/integraciones/contrato?method=Proc_Transacciones');
    expect(urls).toContain('/integraciones/relacion-informacion?method=Proc_Transacciones');
    expect(urls).toContain('/integraciones/conexion?method=Proc_Transacciones');
  });

  it('oculta acciones de administración sin permiso y las muestra con permiso', () => {
    fixture.componentRef.setInput('canManage', false);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Ver Conexión entidad participante');

    fixture.componentRef.setInput('canManage', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Ver Conexión entidad participante');
  });
  it('soporta branding y textos extremos sin cambiar la interacción primaria a tabla', () => {
    component.service = {
      ...service,
      functionalTitle: 'Servicio CFA con nombre funcional extremadamente largo '.repeat(8),
      functionalDescription: 'Descripción funcional y operativa extensa '.repeat(30),
      technicalMethodName: 'Proc_Transacciones_Con_Nombre_Tecnico_Extremadamente_Largo_Sin_Separadores'
    };
    fixture.componentRef.setInput('canManage', true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect((fixture.nativeElement.querySelector('h2') as HTMLElement).textContent).toContain('Servicio CFA con nombre funcional');
    expect(fixture.nativeElement.querySelectorAll('.service-card__actions a').length).toBe(3);
  });

});
