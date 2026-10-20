import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TransactionIntegrationResultItem } from '../../transactions.models';
import { TransactionIntegrationResultComponent } from './transaction-integration-result.component';

describe('TransactionIntegrationResultComponent', () => {
  let fixture: ComponentFixture<TransactionIntegrationResultComponent>;
  let component: TransactionIntegrationResultComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionIntegrationResultComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionIntegrationResultComponent);
    component = fixture.componentInstance;
  });

  it('muestra R96 de Proc_Contrapartidas como débito exitoso sin payload técnico', () => {
    component.result = result(item({
      method: 'Proc_Contrapartidas',
      responseCode: 'R96',
      responseDescription: 'Débito aplicado correctamente',
      businessStatus: 'Success'
    }));
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('RESULTADO DEL PROCESAMIENTO EN EL CORE');
    expect(text).toContain('Proc_Contrapartidas');
    expect(text).toContain('R96');
    expect(text).toContain('Débito aplicado correctamente');
    expect(text).toContain('Exitoso');
    expect(text).not.toContain('<Envelope');
    expect(text).not.toContain('RequestPayloadXml');
    expect(text).not.toContain('ResponsePayloadXml');
  });

  it('muestra R96 de Proc_Transacciones como crédito exitoso', () => {
    component.result = result(item({
      method: 'Proc_Transacciones',
      responseCode: 'R96',
      responseDescription: 'Crédito aplicado correctamente',
      businessStatus: 'Success'
    }));
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Proc_Transacciones');
    expect(text).toContain('Crédito aplicado correctamente');
  });

  it('presenta cada estado desde BusinessStatus y separa el error técnico', () => {
    expect(component.resultLabel(item({ businessStatus: 'Success' }))).toBe('Exitoso');
    expect(component.resultLabel(item({ businessStatus: 'Rejected' }))).toBe('Rechazado');
    expect(component.resultLabel(item({ businessStatus: 'PendingCatalog' }))).toBe('Pendiente de interpretación');
    expect(component.resultLabel(item({ businessStatus: 'ManualReview' }))).toBe('Requiere revisión');
    expect(component.resultLabel(item({ businessStatus: 'Unknown' }))).toBe('Desconocido');
    expect(component.resultLabel(item({ transportStatus: 'Failed' }))).toBe('Error técnico');
    expect(component.resultLabel(item({ transportStatus: 'TimedOut' }))).toBe('Error técnico');
    expect(component.resultLabel(item({ transportStatus: 'Succeeded', businessStatus: 'Rejected' }))).toBe('Rechazado');
    expect(component.statusClass(item({ transportStatus: 'Succeeded', businessStatus: 'Rejected' }))).toBe('rejected');
    expect(component.statusClass(item({ transportStatus: 'TimedOut', businessStatus: 'Rejected' }))).toBe('failed');
  });

  it('conserva latest e historial y renderiza el historial resumido', () => {
    const latest = item({ method: 'Proc_Transacciones', responseCode: 'R96' });
    const previous = item({
      method: 'Proc_Contrapartidas',
      responseCode: 'R01',
      businessStatus: 'Rejected'
    });
    component.result = { transactionId: 1, latest, history: [latest, previous] };

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(component.latest).toBe(latest);
    expect(text).toContain('Historial resumido (2)');
    expect(text).toContain('Proc_Transacciones');
    expect(text).toContain('Proc_Contrapartidas');
    expect(text).toContain('Rechazado');
  });

  it('usa un botón Material para cerrar y emite closePanel', () => {
    component.result = result(item());
    spyOn(component.closePanel, 'emit');
    fixture.detectChanges();

    const closeButton = fixture.nativeElement.querySelector('button[mat-icon-button][aria-label="Cerrar resultado"]') as HTMLButtonElement;
    expect(closeButton).not.toBeNull();

    closeButton.click();
    expect(component.closePanel.emit).toHaveBeenCalled();
  });

});

function result(latest: TransactionIntegrationResultItem) {
  return { transactionId: 1, latest, history: [latest] };
}

function item(overrides: Partial<TransactionIntegrationResultItem> = {}): TransactionIntegrationResultItem {
  return {
    catalogId: 1,
    method: 'Proc_Contrapartidas',
    transportStatus: 'Succeeded',
    businessStatus: 'Success',
    responseCode: 'R96',
    responseDescription: 'Débito aplicado correctamente',
    processedAt: '2026-07-16T12:00:00Z',
    attemptNumber: 1,
    retryAllowed: false,
    requiresManualReview: false,
    transactionState: 'AppliedTacitly',
    ...overrides
  };
}
