import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { CenitOperationsApiService } from '../services/cenit-operations-api.service';
import { CenitOperationPageComponent } from './cenit-operation-page.component';

describe('CenitOperationPageComponent', () => {
  const service = () => jasmine.createSpyObj<CenitOperationsApiService>('CenitOperationsApiService', [
    'getCycles',
    'getQueueTransactions',
    'getNetPositions',
    'getOptimizationDecisions',
    'getReturns',
    'getOperationalTraceability',
    'getChamberResponses'
  ]);

  function create(
    view: string,
    api = service(),
    configure?: (api: jasmine.SpyObj<CenitOperationsApiService>) => void
  ): ComponentFixture<CenitOperationPageComponent> {
    api.getCycles.and.returnValue(of([]));
    api.getQueueTransactions.and.returnValue(of({ items: [] }));
    api.getNetPositions.and.returnValue(of({ items: [] }));
    api.getOptimizationDecisions.and.returnValue(of({ items: [] }));
    api.getReturns.and.returnValue(of([]));
    api.getOperationalTraceability.and.returnValue(of({ items: [] }));
    api.getChamberResponses.and.returnValue(of({ items: [] }));
    configure?.(api);

    TestBed.configureTestingModule({
      imports: [CenitOperationPageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { data: { view } } } },
        { provide: CenitOperationsApiService, useValue: api }
      ]
    });

    const fixture = TestBed.createComponent(CenitOperationPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => TestBed.resetTestingModule());

  it('Operation_ShouldCallCyclesEndpointForCyclesView', () => {
    const api = service();
    create('ciclos', api);
    expect(api.getCycles).toHaveBeenCalledWith({ page: 1, pageSize: 50 });
  });

  it('Operation_ShouldCallQueueEndpointForQueueView', () => {
    const api = service();
    create('cola', api);
    expect(api.getQueueTransactions).toHaveBeenCalledWith('', 1, 50);
  });

  it('Operation_ShouldCallNetPositionsEndpointForNettingView', () => {
    const api = service();
    create('neteo', api);
    expect(api.getNetPositions).toHaveBeenCalled();
  });

  it('Operation_ShouldCallOptimizationEndpointForOptimizationView', () => {
    const api = service();
    create('optimizacion', api);
    expect(api.getOptimizationDecisions).toHaveBeenCalled();
  });

  it('Operation_ShouldCallReturnsEndpointForReturnsView', () => {
    const api = service();
    create('devoluciones', api);
    expect(api.getReturns).toHaveBeenCalledWith({ page: 1, pageSize: 50 });
  });

  it('Operation_ShouldCallTraceabilityEndpointForTraceabilityView', () => {
    const api = service();
    create('trazabilidad', api);
    expect(api.getOperationalTraceability).toHaveBeenCalledWith(1, 50);
  });

  it('Operation_ShouldRepresentEveryChamberStateAndCorrelationProblem', () => {
    const api = service();
    const states = [
      ['Pending', 'Unknown', 'Pending', null],
      ['Accepted', 'Ack', 'Matched', null],
      ['Rejected', 'Nack', 'Matched', null],
      ['OperatorRejected', 'OperatorRejected', 'Matched', null],
      ['Reconciliation', 'Reconciliation', 'Matched', null],
      ['NoActivity', 'NoActivity', 'Matched', null],
      ['Pending', 'Unknown', 'Ambiguous', 'CENIT_CORRELATION_AMBIGUOUS']
    ] as const;
    const component = create('respuestas-camara', api, (mock) => {
      mock.getChamberResponses.and.returnValue(of({
        items: states.map(([state, responseType, correlationOutcome, problemCode], index) => ({
          id: `response-${index}`,
          isDuplicate: false,
          sourceResponseId: `source-${index}`,
          sourceFileName: `response-${index}.xml`,
          rawTechnicalReference: `response-${index}.xml#source-${index}`,
          responseType,
          state,
          correlationOutcome,
          relatedFileName: `file-${index}`,
          receivedAtUtc: '2026-08-31T12:00:00Z',
          isApplied: correlationOutcome === 'Matched',
          problemCode
        }))
      }));
    }).componentInstance;

    expect(api.getChamberResponses).toHaveBeenCalledWith(1, 50);
    expect(component.rows.map((row) => row['Estado cámara'])).toEqual([
      'Pendiente',
      'ACK aceptado',
      'NACK rechazado',
      'Rechazo definitivo del operador',
      'Reconciliación',
      'Sin actividad',
      'Pendiente'
    ]);
    expect(component.rows[6]['Correlación']).toContain('CENIT_CORRELATION_AMBIGUOUS');
  });

  it('Operation_ShouldRenderRowsWhenCyclesArrive', () => {
    const api = service();
    const component = create('ciclos', api, (mock) => {
      mock.getCycles.and.returnValue(of([
        {
          cycleId: 'CENIT-01',
          cycleName: 'Ciclo 1',
          processingDate: '2026-06-01',
          startTime: '08:00',
          endTime: '10:00',
          cutoffTime: '09:30',
          schedule: 'DIA',
          status: 'Open',
          clearingHouseName: 'CENIT',
          totalTransactions: 3,
          totalAmount: 150000
        }
      ]));
    }).componentInstance;

    expect(component.rows.length).toBe(1);
    expect(component.columnasTabla.length).toBeGreaterThan(0);
    expect(Object.values(component.rows[0])).toContain('Ciclo 1');
  });
});
