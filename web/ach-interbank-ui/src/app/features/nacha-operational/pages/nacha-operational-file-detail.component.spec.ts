import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NachaOperationalReadinessService } from '../services/nacha-operational-readiness.service';
import { NachaOperationalFileDetailComponent } from './nacha-operational-file-detail.component';

describe('NachaOperationalFileDetailComponent', () => {
  let fixture: ComponentFixture<NachaOperationalFileDetailComponent>;
  let service: jasmine.SpyObj<NachaOperationalReadinessService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<NachaOperationalReadinessService>('NachaOperationalReadinessService', ['getFileDetail']);
    service.getFileDetail.and.returnValue(of(detail()));

    await TestBed.configureTestingModule({
      imports: [NachaOperationalFileDetailComponent],
      providers: [
        provideRouter([]),
        { provide: NachaOperationalReadinessService, useValue: service },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'nacha-N1' } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaOperationalFileDetailComponent);
    fixture.detectChanges();
  });

  it('FileDetailComponent_ShouldCreate', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('FileDetailComponent_ShouldRenderNoGoBanner', () => {
    expect(text()).toContain('Productivo NO-GO');
  });

  it('FileDetailComponent_ShouldRenderReadOnlyBanner', () => {
    expect(text()).toContain('Backend solo lectura sanitizado');
  });

  it('FileDetailComponent_ShouldRenderHeaderBatchesEntriesAddendasControls', () => {
    const content = text();

    expect(content).toContain('Encabezado');
    expect(content).toContain('Lotes');
    expect(content).toContain('Entradas');
    expect(content).toContain('Addendas');
    expect(content).toContain('Controles');
  });

  it('FileDetailComponent_ShouldRenderTotalsSummary', () => {
    expect(text()).toContain('Totales');
    expect(text()).toContain('Records persistidos');
  });

  it('FileDetailComponent_ShouldRenderWarningsWhenPartial', () => {
    expect(text()).toContain('Warnings operativos');
    expect(text()).toContain('detalle parcial');
  });

  it('FileDetailComponent_ShouldNotRenderDangerousActions', () => {
    const content = text();

    expect(content).not.toContain('Reprocesar');
    expect(content).not.toContain('Ejecutar SOAP');
    expect(content).not.toContain('Mover dinero');
    expect(content).not.toContain('Descargar productivo');
  });

  it('Service_ShouldCallFileDetailEndpointThroughComponent', () => {
    expect(service.getFileDetail).toHaveBeenCalledWith('nacha-N1');
  });

  it('FileDetailComponent_ShouldShowControlled404Message', () => {
    service.getFileDetail.and.returnValue(throwError(() => new Error('Archivo NACHA-M no encontrado o no persistido.')));
    const errorFixture = TestBed.createComponent(NachaOperationalFileDetailComponent);

    errorFixture.detectChanges();

    expect(errorFixture.nativeElement.textContent).toContain('Archivo NACHA-M no encontrado o no persistido.');
  });

  function text(): string {
    return fixture.nativeElement.textContent;
  }
});

function detail() {
  return {
    fileId: 'nacha-N1',
    headerId: 'N1',
    fileName: 'entrada.ach',
    clearingHouseCode: 'ACH',
    profileCode: 'nacha-config profiles',
    flowType: 'IncomingPersisted',
    isReturnFile: false,
    processingStatus: 'Persisted',
    validationPassed: true,
    receivedAt: '2026-05-24T23:00:00Z',
    createdAt: '2026-05-24T23:00:00Z',
    correlationId: 'corr-N1',
    dataSource: 'parcial',
    isPartialData: true,
    warnings: ['No persisted file controls found; detalle parcial read-only.'],
    header: { headerId: 'N1', priorityCode: '01', recordSize: '094', blockingFactor: '10', cycleNumber: 1 },
    batches: [{ batchId: 1, batchNumber: 1, serviceClassCode: '220', companyName: 'CFA', standardEntryClassCode: 'PPD' }],
    entries: [{ entryDetailId: 1, transactionCode: '22', accountNumberMasked: '****3456', recipIdNumberMasked: '****6789', amount: 100 }],
    addendas: [{ addendaId: 1, codeTypeAddendumRecord: '05', invoiceOrAccountNumberMasked: '****1111' }],
    batchControls: [{ batchControlId: 1, batchNumber: '1', entryAddendaCount: 2, entryHash: 1, totalDebitAmount: 0, totalCreditAmount: 100 }],
    fileControls: [{ fileControlId: 1, batchCount: 1, blockCount: 1, entryAddendaCount: 2, entryHash: 1, totalDebitAmount: 0, totalCreditAmount: 100 }],
    totalsSummary: {
      batchCount: 1,
      entryCount: 1,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      persistedRecordCount: 5,
      totalDebitAmount: 0,
      totalCreditAmount: 100,
      validationPassed: true
    },
    noSensitiveData: true
  };
}
