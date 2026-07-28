import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import {
  ApplicationDownloadError,
  BlobDownloadService,
  SavedDownload
} from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ExportableAchCycle } from '../models/ach-cycle-export.model';
import { ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { NachaExportApiService } from '../services/nacha-export-api.service';
import { NachaExportComponent } from './nacha-export.component';

describe('NachaExportComponent', () => {
  let fixture: ComponentFixture<NachaExportComponent>;
  let component: NachaExportComponent;
  let api: jasmine.SpyObj<NachaExportApiService>;
  let downloads: jasmine.SpyObj<BlobDownloadService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const exportableCycle: ExportableAchCycle = {
    id: 'f78ca7bae2b80c3034353fc3dbccd801c605e7ee',
    cycleId: 'f78ca7bae2b80c3034353fc3dbccd801c605e7ee',
    cycleName: 'Ciclo 1',
    clearingHouseName: 'ACH Colombia',
    processingDate: '2026-07-28T00:00:00Z',
    batchCount: 1,
    transactionCount: 1,
    isExportable: true
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<NachaExportApiService>('NachaExportApiService', ['getExportableCycles', 'downloadFile']);
    api.getExportableCycles.and.returnValue(of([]));
    api.downloadFile.and.returnValue(of(new HttpResponse({
      status: 200,
      body: new Blob(['nacha'])
    })));

    downloads = jasmine.createSpyObj<BlobDownloadService>('BlobDownloadService', ['save', 'fromHttpError']);
    downloads.save.and.resolveTo({
      fileName: '0000001.001.20260728.1.OUT',
      size: 106,
      contentType: 'text/plain'
    } satisfies SavedDownload);

    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['error', 'info', 'success']);

    await TestBed.configureTestingModule({
      imports: [NachaExportComponent],
      providers: [
        provideRouter([]),
        { provide: NachaExportApiService, useValue: api },
        { provide: BlobDownloadService, useValue: downloads },
        { provide: NotificationService, useValue: notifications },
        { provide: ClearingHousesApiService, useValue: { list: () => of([]) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaExportComponent);
    component = fixture.componentInstance;
  });

  it('crea el formulario y carga los datos una sola vez al iniciar', () => {
    fixture.detectChanges();

    expect(component.filterForm.getRawValue()).toEqual({
      clearingHouseId: null,
      startDate: null,
      endDate: null,
      search: ''
    });
    expect(api.getExportableCycles).toHaveBeenCalledTimes(1);
  });

  it('valida que la fecha inicial no sea posterior a la final', () => {
    component.filterForm.patchValue({
      startDate: new Date(2026, 6, 29),
      endDate: new Date(2026, 6, 28)
    });

    expect(component.filterForm.hasError('dateRange')).toBeTrue();
    component.submit();
    expect(api.getExportableCycles).not.toHaveBeenCalled();
  });

  it('aplica filtros soportados por el backend en una sola consulta', () => {
    component.filterForm.patchValue({
      clearingHouseId: 1,
      startDate: new Date(2026, 6, 27),
      endDate: new Date(2026, 6, 28)
    });

    component.submit();

    expect(api.getExportableCycles).toHaveBeenCalledOnceWith({
      clearingHouseId: 1,
      startDate: '2026-07-27',
      endDate: '2026-07-28'
    });
  });

  it('limpia filtros y restaura la consulta', () => {
    component.filterForm.patchValue({ clearingHouseId: 1, search: 'Ciclo' });

    component.clearFilters();

    expect(component.filterForm.getRawValue()).toEqual({
      clearingHouseId: null,
      startDate: null,
      endDate: null,
      search: ''
    });
    expect(api.getExportableCycles).toHaveBeenCalledTimes(1);
  });

  it('expone un estado vacío útil', () => {
    fixture.detectChanges();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No se encontraron ciclos disponibles');
    expect(fixture.nativeElement.textContent).toContain('no existen transacciones elegibles');
  });

  it('usa un handler explícito y el cycleId para descargar NACHA-M', () => {
    component.downloadPlain(exportableCycle);

    expect(api.downloadFile).toHaveBeenCalledOnceWith(exportableCycle.cycleId!, false);
  });

  it('usa un handler explícito para generar el sobre digital', () => {
    component.downloadEncrypted(exportableCycle);

    expect(api.downloadFile).toHaveBeenCalledOnceWith(exportableCycle.cycleId!, true);
  });

  it('no reutiliza campos hash alternativos cuando falta cycleId', () => {
    component.downloadPlain({
      ...exportableCycle,
      cycleId: null,
      fileHash: exportableCycle.id,
      hash: exportableCycle.id,
      exportIdentifier: exportableCycle.id
    });

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith(
      'No fue posible exportar: el ciclo no tiene un identificador válido.'
    );
  });

  it('explica por qué una acción está deshabilitada', () => {
    const cycle = {
      ...exportableCycle,
      isExportable: false,
      exportUnavailableReason: 'No hay transacciones elegibles para este ciclo.'
    };

    component.downloadPlain(cycle);

    expect(api.downloadFile).not.toHaveBeenCalled();
    expect(notifications.info).toHaveBeenCalledWith('No hay transacciones elegibles para este ciclo.');
  });

  it('previene el doble clic mientras la fila está procesándose', () => {
    const pending = new Subject<HttpResponse<Blob>>();
    api.downloadFile.and.returnValue(pending);

    component.downloadPlain(exportableCycle);
    component.downloadEncrypted(exportableCycle);

    expect(api.downloadFile).toHaveBeenCalledTimes(1);
    expect(component.isProcessing(exportableCycle)).toBeTrue();
    pending.complete();
    expect(component.isProcessing(exportableCycle)).toBeFalse();
  });

  it('procesa Problem Details como Blob y conserva detalle, código y traceId', async () => {
    const problemBlob = new Blob([JSON.stringify({
      detail: 'No existe un perfil NACHA-M vigente.',
      errorCode: 'NACHA_PROFILE_NOT_PUBLISHED',
      traceId: 'trace-safe-42'
    })], { type: 'application/problem+json' });
    api.downloadFile.and.returnValue(throwError(() => new HttpErrorResponse({ status: 422, error: problemBlob })));
    downloads.fromHttpError.and.resolveTo(new ApplicationDownloadError(
      'No existe un perfil NACHA-M vigente.',
      422,
      'NACHA_PROFILE_NOT_PUBLISHED',
      'trace-safe-42'
    ));

    component.downloadPlain(exportableCycle);
    await new Promise(resolve => setTimeout(resolve, 0));
    await new Promise(resolve => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(component.operationError?.message).toBe('No existe un perfil NACHA-M vigente.');
    expect(component.operationError?.errorCode).toBe('NACHA_PROFILE_NOT_PUBLISHED');
    expect(component.operationError?.traceId).toBe('trace-safe-42');
    expect(fixture.nativeElement.textContent).toContain('Identificador de soporte: trace-safe-42');
  });

  it('finaliza el progreso de fila cuando ocurre un error', async () => {
    api.downloadFile.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    downloads.fromHttpError.and.resolveTo(Object.assign(new Error('Error controlado'), { status: 500 }));

    component.downloadEncrypted(exportableCycle);
    await fixture.whenStable();

    expect(component.isProcessing(exportableCycle)).toBeFalse();
  });
});
