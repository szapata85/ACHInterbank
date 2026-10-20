import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { provideRouter } from '@angular/router';
import { IntegrationServiceReadModel } from '../data-access/integration-read.models';
import { IntegrationCenterShellComponent } from './integration-center-shell.component';

function service(methodId: number, title: string, technicalMethodName: string): IntegrationServiceReadModel {
  return {
    methodId,
    functionalTitle: title,
    functionalDescription: `Descripción de ${title}.`,
    technicalMethodName,
    movesMoney: methodId !== 3,
    contractVersionLabel: 'v1',
    contractStatus: 'Active',
    mappingStatus: 'Published',
    mappingCoverage: null,
    connectionStatus: 'DryRun',
    canEditMapping: null,
    canPublish: null,
    canManageContract: null,
    canManageConnection: null
  };
}

describe('IntegrationCenterShellComponent', () => {
  let fixture: ComponentFixture<IntegrationCenterShellComponent>;
  let component: IntegrationCenterShellComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IntegrationCenterShellComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(IntegrationCenterShellComponent);
    component = fixture.componentInstance;
  });

  it('expone estado loading con mensaje accesible', () => {
    component.status = 'loading';
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Cargando información de integración');
  });

  it('expone error con reintento', () => {
    component.status = 'error';
    component.error = { kind: 'http', status: 500, message: 'Fallo controlado' };
    spyOn(component.retry, 'emit');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Fallo controlado');
    button.click();
    expect(component.retry.emit).toHaveBeenCalled();
  });

  it('distingue estados empty y forbidden', () => {
    component.status = 'empty';
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin servicios CFA disponibles');

    fixture.componentRef.setInput('status', 'forbidden');
    fixture.componentRef.setInput('error', { kind: 'forbidden', status: 403, message: 'Acceso denegado' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Permisos insuficientes');
    expect(fixture.nativeElement.textContent).toContain('Acceso denegado');
  });

  it('renderiza las tres tarjetas funcionales de Conexiones', () => {
    component.status = 'ready';
    component.permissions = { canRead: true, canManage: false };
    component.services = [
      service(1, 'Créditos recibidos en CFA', 'Proc_Transacciones'),
      service(2, 'Débitos originados en CFA', 'Proc_Contrapartidas'),
      service(3, 'Respuestas de transacciones', 'RegistrarRespuestaTransaccion')
    ];
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-integration-service-card').length).toBe(3);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Conexiones');
    expect(text).toContain('Créditos recibidos en CFA');
    expect(text).toContain('Débitos originados en CFA');
    expect(text).toContain('Respuestas de transacciones');

    const mappingUrls = fixture.debugElement
      .queryAll(By.css('[data-testid="service-mapping-link"]'))
      .map((link) => link.injector.get(RouterLink).urlTree.toString());
    expect(mappingUrls).toEqual([
      '/integraciones/relacion-informacion?method=Proc_Transacciones',
      '/integraciones/relacion-informacion?method=Proc_Contrapartidas',
      '/integraciones/relacion-informacion?method=RegistrarRespuestaTransaccion'
    ]);
  });
});
