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
