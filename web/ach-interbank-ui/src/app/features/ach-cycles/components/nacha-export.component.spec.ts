import { HttpErrorResponse, HttpHeaders, HttpResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
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
      cycleName: 'Hash demo',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 1,
      isExportable: false,
      exportUnavailableReason: 'Este ciclo no tiene archivo NACHA-M exportable.'
    }, true);

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.info).toHaveBeenCalled();
  });

  it('Download_ShouldUsePersistedExportIdentifierWhenAvailable', () => {
    component.download({
      id: 'cycle-persisted-001',
      cycleName: 'Persisted',
      processingDate: '2026-05-25T00:00:00Z',
      transactionCount: 2,
      isExportable: true
    }, false);

    expect(api.downloadFile).toHaveBeenCalledWith('cycle-persisted-001', false);
  });

  it('Download_ShouldHandle422WithUserFriendlyMessage', async () => {
    const errorBody = new Blob([
      JSON.stringify({
        codigo: 'NACHA_NO_EXPORTABLE_CONTENT',
        mensaje: 'No hay transacciones exportables para el ciclo.'
      })
    ], { type: 'application/json' });
    api.downloadFile.and.returnValue(throwError(() => new HttpErrorResponse({ status: 422, error: errorBody })));

    component.download({
      id: 'cycle-empty',
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
});

async function waitForAsyncErrorHandling(): Promise<void> {
  for (let attempt = 0; attempt < 20; attempt += 1) {
    await new Promise(resolve => setTimeout(resolve, 25));
  }
}
