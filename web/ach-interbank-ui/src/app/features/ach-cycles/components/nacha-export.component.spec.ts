import { HttpErrorResponse, HttpHeaders, HttpResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { NachaExportApiService } from '../services/nacha-export-api.service';
import { NachaExportComponent } from './nacha-export.component';

describe('NachaExportComponent', () => {
  let fixture: ComponentFixture<NachaExportComponent>;
  let component: NachaExportComponent;
  let api: jasmine.SpyObj<NachaExportApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<NachaExportApiService>('NachaExportApiService', ['getExportableCycles', 'downloadFile']);
    api.getExportableCycles.and.returnValue(of([]));
    api.downloadFile.and.returnValue(of(new HttpResponse<Blob>({
      body: new Blob(['nacha']),
      headers: new HttpHeaders({ 'content-disposition': 'attachment; filename="test.ach"' })
    })));

    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['error', 'info']);

    await TestBed.configureTestingModule({
      imports: [NachaExportComponent],
      providers: [
        provideRouter([]),
        { provide: NachaExportApiService, useValue: api },
        { provide: NotificationService, useValue: notifications },
        { provide: ClearingHousesApiService, useValue: { list: () => of([]) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaExportComponent);
    component = fixture.componentInstance;
  });

  it('Dashboard_ShouldDisableDownloadForDemoFiles', () => {
    component.download({
      id: 'demo-hash',
      cycleId: null,
      cycleName: 'Demo',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 1,
      isExportable: false,
      exportUnavailableReason: 'Registro demo no persistido.'
    }, false);

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.info).toHaveBeenCalledWith('Registro demo no persistido.');
  });

  it('Dashboard_ShouldNotCallNachaExportForNonExportableDemoRows', () => {
    component.download({
      id: '1b12995d45906869e194e237f3db64bfd7e07d2f',
      cycleId: null,
      cycleName: 'Hash demo',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 1,
      isExportable: false,
      exportUnavailableReason: 'Este ciclo no tiene archivo NACHA-M exportable.'
    }, true);

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.info).toHaveBeenCalled();
  });

  it('ExportFlow_ShouldUseCycleIdFromAchCyclesNachaExport', () => {
    component.download({
      id: '1b12995d45906869e194e237f3db64bfd7e07d2f',
      cycleId: '42',
      cycleName: 'Persisted',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 2,
      isExportable: true
    }, false);

    expect(api.downloadFile).toHaveBeenCalledWith('42', false);
  });

  it('ExportFlow_ShouldNotFallbackToHashFromRowId', () => {
    component.download({
      id: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa',
      cycleName: 'Hash only',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 2,
      isExportable: true,
      fileHash: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa'
    }, false);

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No fue posible exportar: el ciclo no tiene identificador cycleId.');
  });

  it('ExportRegression_ShouldNotUseHashAfterReadStoreChanges', () => {
    component.download({
      id: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa',
      cycleId: '',
      cycleName: 'Read-store metadata only',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 2,
      isExportable: true,
      hash: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa',
      fileHash: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa',
      exportIdentifier: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa'
    }, false);

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No fue posible exportar: el ciclo no tiene identificador cycleId.');
  });

  it('ExportFlow_ShouldNotCallNachaExportForNonExportableRows', () => {
    component.download({
      id: 'cycle-row',
      cycleId: '42',
      cycleName: 'No exportable',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 2,
      isExportable: false,
      exportUnavailableReason: 'Sin lotes exportables.'
    }, false);

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.info).toHaveBeenCalledWith('Sin lotes exportables.');
  });

  it('ExportFlow_ShouldDisableExportActionWhenCycleIdIsMissing', () => {
    const actionColumn = component.columnas.find(column => column.colId === 'acciones');
    const rendered = actionColumn?.cellRenderer?.({
      data: {
        id: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa',
        cycleName: 'Hash only',
        processingDate: '2026-05-25T00:00:00Z',
        transactionCount: 1,
        isExportable: true
      }
    } as any) as string;

    expect(rendered).toContain('disabled');
    expect(rendered).toContain('Este ciclo no tiene archivo NACHA-M exportable.');
  });

  it('OnCellClicked_ShouldOnlyDownloadFromExportActionColumn', () => {
    const actionColumn = component.columnas.find(column => column.colId === 'acciones');
    const button = document.createElement('button');
    button.setAttribute('data-action', 'generar-nacha');

    actionColumn?.onCellClicked?.({
      data: {
        id: 'hash-metadata',
        cycleId: '42',
        cycleName: 'Exportable',
        processingDate: '2026-05-25T00:00:00Z',
        transactionCount: 1,
        isExportable: true
      },
      event: { target: button }
    } as any);

    expect(api.downloadFile).toHaveBeenCalledWith('42', false);
    expect(api.downloadFile).toHaveBeenCalledTimes(1);
  });

  it('OnCellClicked_EncryptedAction_ShouldOnlyCallEnvelopeEndpointOnce', () => {
    const actionColumn = component.columnas.find(column => column.colId === 'acciones');
    const button = document.createElement('button');
    button.setAttribute('data-action', 'generar-sobre');

    actionColumn?.onCellClicked?.({
      data: {
        id: 'hash-metadata',
        cycleId: '42',
        cycleName: 'Exportable',
        processingDate: '2026-05-25T00:00:00Z',
        transactionCount: 1,
        isExportable: true
      },
      event: { target: button }
    } as any);

    expect(api.downloadFile).toHaveBeenCalledOnceWith('42', true);
  });

  it('Download_ShouldBlockConcurrentClicksUntilTheRequestCompletes', () => {
    const pending = new Subject<HttpResponse<Blob>>();
    api.downloadFile.and.returnValue(pending);
    const cycle = {
      id: 'cycle-42', cycleId: '42', cycleName: 'Exportable',
      processingDate: '2026-05-25T00:00:00Z', transactionCount: 1, isExportable: true
    };

    component.download(cycle, false);
    component.download(cycle, true);

    expect(api.downloadFile).toHaveBeenCalledTimes(1);
    pending.complete();
  });

  it('OnCellClicked_ShouldNotDownloadOnRegularCellClick', () => {
    const actionColumn = component.columnas.find(column => column.colId === 'acciones');
    const cell = document.createElement('span');

    actionColumn?.onCellClicked?.({
      data: {
        id: 'hash-metadata',
        cycleId: '42',
        cycleName: 'Exportable',
        processingDate: '2026-05-25T00:00:00Z',
        transactionCount: 1,
        isExportable: true
      },
      event: { target: cell }
    } as any);

    expect(api.downloadFile).not.toHaveBeenCalled();
  });

  it('Download_ShouldHandle422WithControlledMessage', async () => {
    const errorBody = new Blob([
      JSON.stringify({
        codigo: 'NACHA_NO_EXPORTABLE_CONTENT',
        mensaje: 'No hay transacciones exportables para el ciclo.'
      })
    ], { type: 'application/json' });
    api.downloadFile.and.returnValue(throwError(() => new HttpErrorResponse({ status: 422, error: errorBody })));

    component.download({
      id: 'cycle-empty',
      cycleId: 'cycle-empty',
      cycleName: 'Empty',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 1,
      isExportable: true
    }, false);
    await waitForAsyncErrorHandling();

    expect(notifications.error).toHaveBeenCalledWith(
      'No hay transacciones exportables para el ciclo. (NACHA_NO_EXPORTABLE_CONTENT)'
    );
  });

  it('Download_ShouldNotExposeRawBackendError', async () => {
    api.downloadFile.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 422,
      error: new Blob(['not-json'], { type: 'text/plain' })
    })));

    component.download({
      id: 'cycle-invalid',
      cycleId: 'cycle-invalid',
      cycleName: 'Invalid',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 1,
      isExportable: true
    }, false);
    await waitForAsyncErrorHandling();

    expect(notifications.error).toHaveBeenCalledWith(
      'El ciclo no cumple las condiciones funcionales para exportar NACHA-M.'
    );
  });

  it('Download_ShouldReadProblemDetailsBlobForAnyHttpStatusAndIncludeTraceId', async () => {
    const problem = new Blob([JSON.stringify({
      title: 'Certificado no disponible',
      detail: 'No existe un certificado vigente para cifrar el archivo.',
      code: 'CERTIFICATE_NOT_FOUND',
      traceId: 'trace-safe-42'
    })], { type: 'application/problem+json' });
    api.downloadFile.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: problem })));

    component.download({
      id: 'cycle-42', cycleId: 'cycle-42', cycleName: 'Exportable',
      processingDate: '2026-05-25T00:00:00Z', transactionCount: 1, isExportable: true
    }, true);
    await waitForAsyncErrorHandling();

    expect(notifications.error).toHaveBeenCalledWith(
      'No existe un certificado vigente para cifrar el archivo. (CERTIFICATE_NOT_FOUND) [traceId: trace-safe-42]'
    );
  });
});

async function waitForAsyncErrorHandling(): Promise<void> {
  for (let attempt = 0; attempt < 20; attempt += 1) {
    await new Promise(resolve => setTimeout(resolve, 25));
  }
}
