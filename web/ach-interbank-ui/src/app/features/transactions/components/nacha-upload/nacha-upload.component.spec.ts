import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { ClearingHousesApiService } from '../../../../core/services/clearing-houses-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { NachaUploadRecord, NachaUploadService } from '../../services/nacha-upload.service';
import {
  NachaUploadComponent,
  NachaUploadResultView,
  classifyNachaUploadFile,
  classifyNachaUploadNotification
} from './nacha-upload.component';

describe('NachaUploadComponent file validation', () => {
  it('should classify ACH Colombia production references without implying homologation', () => {
    const result = classifyNachaUploadFile('0001283.001.20250331.1.OUT');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('production-reference-achcol');
    expect(result.label).toBe('Referencia productiva ACH Colombia');
    expect(result.detail).toContain('no implica homologación normativa');
  });

  it('should classify CENIT production references without adding an extension', () => {
    const result = classifyNachaUploadFile('0001283.002.20250331.1');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('production-reference-cenit');
    expect(result.label).toBe('Referencia productiva CENIT');
    expect(result.detail).toContain('no implica homologación normativa');
  });

  it('should classify ACH Colombia official operational files', () => {
    for (const suffix of ['1', '5', '6', '10']) {
      const result = classifyNachaUploadFile(`0001283.001.${suffix}`);

      expect(result.allowed).toBeTrue();
      expect(result.kind).toBe('official-ach');
      expect(result.label).toBe('Archivo operativo ACH Colombia');
      expect(result.detail).toContain('RRRRTTT.ZZZ.N');
    }
  });

  it('should classify ACH Colombia return files', () => {
    const result = classifyNachaUploadFile('0001283.001.RET');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('official-ret');
    expect(result.label).toBe('Devolución ACH Colombia');
    expect(result.detail).toContain('RRRRTTT.ZZZ.RET');
  });

  it('should classify .ach files as UAT internal fixtures', () => {
    const result = classifyNachaUploadFile('ACH_COL_IN_001.ach');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('uat-fixture');
    expect(result.label).toBe('Fixture UAT/golden interno');
  });

  it('should classify .ach suffix on an official-looking name as a UAT internal fixture, not official', () => {
    const result = classifyNachaUploadFile('0001283.001.1.ach');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('uat-fixture');
    expect(result.label).toBe('Fixture UAT/golden interno');
  });

  it('should classify encrypted ACH Colombia envelopes', () => {
    const result = classifyNachaUploadFile('0001283.001.20260731.1.OUT.env');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('digital-envelope-achcol');
    expect(result.label).toBe('Sobre digital ACH Colombia');
    expect(result.detail).toContain('seleccionar ACH Colombia');
  });

  it('should reject txt and nacha files', () => {
    for (const fileName of ['rechazo.txt', 'rechazo.nacha']) {
      const result = classifyNachaUploadFile(fileName);

      expect(result.allowed).toBeFalse();
      expect(result.kind).toBe('rejected');
      expect(result.rejectionMessage).toContain('no se admiten');
    }
  });

  it('should reject unknown extensions', () => {
    const result = classifyNachaUploadFile('archivo.bin');

    expect(result.allowed).toBeFalse();
    expect(result.kind).toBe('rejected');
    expect(result.rejectionMessage).toContain('nombre operativo ACHCOL/CENIT');
  });

  it('should never report success for a controlled reprocess candidate', () => {
    const result = classifyNachaUploadNotification({
      success: false,
      partial: false,
      canReprocess: true,
      statusCode: 200
    });

    expect(result).toBe('warning');
  });

  it('should report success only for a complete successful response', () => {
    const result = classifyNachaUploadNotification({
      success: true,
      partial: false,
      canReprocess: false,
      statusCode: 200
    });

    expect(result).toBe('success');
  });
});

describe('NachaUploadComponent behavior', () => {
  let fixture: ComponentFixture<NachaUploadComponent>;
  let component: NachaUploadComponent;
  let api: jasmine.SpyObj<NachaUploadService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let clearingHouses: jasmine.SpyObj<ClearingHousesApiService>;

  const record: NachaUploadRecord = {
    nachaId: 'nacha-1',
    immediateOrigin: '0001283',
    immediateDestination: '0000001',
    referenceCode: 'REF-1',
    fileCreationDate: '2026-10-02',
    fileCreationTime: '080000',
    achCycleId: 'cycle-1',
    achCycleName: 'Ciclo 1',
    clearingHouseName: 'ACH Colombia',
    totalEntries: 2,
    totalAddendas: 1,
    totalBatches: 1,
    totalAmount: 100,
    totalDebitAmount: 100,
    totalCreditAmount: 0
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<NachaUploadService>('NachaUploadService', ['upload', 'listRecords']);
    api.listRecords.and.returnValue(of([record]));
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);
    clearingHouses = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    clearingHouses.list.and.returnValue(of([{ id: 1, code: 'ACHCOL', name: 'ACH Colombia' }]));

    await TestBed.configureTestingModule({
      imports: [NachaUploadComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: NachaUploadService, useValue: api },
        { provide: NotificationService, useValue: notifications },
        { provide: ClearingHousesApiService, useValue: clearingHouses }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaUploadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function selectFile(file: File, clearingHouseId = 1): void {
    component.form.patchValue({ clearingHouseId, file });
    component.selectedFileValidation = classifyNachaUploadFile(file.name);
  }

  function controlledReprocessResult(overrides: Partial<NachaUploadResultView> = {}): NachaUploadResultView {
    return {
      statusCode: 200,
      statusLabel: 'Rechazo controlado',
      success: false,
      partial: false,
      message: 'Duplicado controlado',
      errors: [],
      traceId: 'trace-1',
      ingestionId: 'ing-1',
      canReprocess: true,
      ingestionStatus: 'Duplicado',
      cycleResolutionStatus: 'Resuelto',
      parsingStatus: 'FallidoReprocesable',
      selectedClearingHouseId: 1,
      detectedClearingHouseId: 1,
      resolvedClearingHouseId: 1,
      totalBatches: 1,
      totalEntries: 2,
      totalAddendas: 1,
      ...overrides
    };
  }

  it('loads persisted records on initialization and preserves record filters', () => {
    expect(api.listRecords).toHaveBeenCalledOnceWith({
      immediateOrigin: '',
      immediateDestination: '',
      referenceCode: '',
      achCycleId: '',
      fileCreationDate: ''
    });
    expect(component.records).toEqual([record]);

    component.filtersForm.setValue({
      immediateOrigin: ' 0001283 ',
      immediateDestination: ' 0000001 ',
      referenceCode: ' REF-1 ',
      achCycleId: ' cycle-1 ',
      fileCreationDate: '2026-10-02'
    });
    component.searchRecords();

    expect(api.listRecords).toHaveBeenCalledWith({
      immediateOrigin: ' 0001283 ',
      immediateDestination: ' 0000001 ',
      referenceCode: ' REF-1 ',
      achCycleId: ' cycle-1 ',
      fileCreationDate: '2026-10-02'
    });

    component.clearFilters();
    expect(component.filtersForm.getRawValue()).toEqual({
      immediateOrigin: '',
      immediateDestination: '',
      referenceCode: '',
      achCycleId: '',
      fileCreationDate: ''
    });
    expect(api.listRecords).toHaveBeenCalledTimes(3);
  });

  it('keeps clearing house and file mandatory and does not call upload when invalid', () => {
    component.form.reset({ clearingHouseId: null, file: null });

    component.upload();

    expect(api.upload).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('Selecciona una cámara y un archivo NACHA-M para continuar.');
  });

  it('rejects a disallowed selected file before calling the API', () => {
    const file = new File(['bad'], 'rechazo.txt', { type: 'text/plain' });
    selectFile(file);

    component.upload();

    expect(api.upload).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('Los archivos .txt y .nacha no se admiten en NachaUpload.');
  });

  it('uploads an allowed file with the existing options and reloads records', () => {
    const file = new File(['content'], '0001283.001.20250331.1.OUT');
    selectFile(file, 7);
    api.upload.and.returnValue(of({
      success: true,
      partial: false,
      message: 'Carga completada.',
      traceId: 'trace-ok',
      ingestionId: 'ing-ok',
      ingestionStatus: 'Completado',
      cycleResolutionStatus: 'Resuelto',
      parsingStatus: 'Completado',
      detectedClearingHouseId: 7,
      resolvedClearingHouseId: 7,
      totalBatches: 1,
      totalEntries: 2,
      totalAddendas: 1,
      errors: []
    }));

    component.upload();

    expect(api.upload).toHaveBeenCalledOnceWith(file, 7, { forceReprocess: false, parentIngestionId: undefined });
    expect(notifications.success).toHaveBeenCalledWith('Carga completada.');
    expect(api.listRecords).toHaveBeenCalledTimes(2);
    expect(component.lastUploadResult?.statusLabel).toBe('Procesado correctamente');
    expect(component.form.get('file')?.value).toBeNull();
  });

  it('preserves controlled reprocess eligibility and reuses the same file with parent ingestion id', () => {
    const file = new File(['content'], '0001283.001.20250331.1.OUT');
    selectFile(file, 3);
    component.lastUploadResult = controlledReprocessResult({ selectedClearingHouseId: 3 });
    api.upload.and.returnValue(of({
      success: true,
      partial: false,
      message: 'Reproceso completado.',
      ingestionId: 'ing-2',
      ingestionStatus: 'Completado',
      cycleResolutionStatus: 'Resuelto',
      parsingStatus: 'Completado',
      errors: []
    }));

    component.reprocess();

    expect(api.upload).toHaveBeenCalledOnceWith(file, 3, { forceReprocess: true, parentIngestionId: 'ing-1' });
    expect(notifications.success).toHaveBeenCalledWith('Reproceso completado.');
  });

  it('does not start a reprocess when the current result is not eligible', () => {
    component.lastUploadResult = controlledReprocessResult({ canReprocess: false });

    component.reprocess();

    expect(api.upload).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('La carga actual no está habilitada para reproceso controlado.');
  });

  it('does not automatically reprocess a controlled failure and keeps the selected file', () => {
    const file = new File(['content'], '0001283.001.20250331.1.OUT');
    selectFile(file, 5);
    api.upload.and.returnValue(of({
      success: false,
      partial: false,
      message: 'Duplicado controlado',
      traceId: 'trace-dup',
      ingestionId: 'ing-dup',
      ingestionStatus: 'Duplicado',
      cycleResolutionStatus: 'Resuelto',
      parsingStatus: 'FallidoReprocesable',
      detectedClearingHouseId: 5,
      resolvedClearingHouseId: 5,
      totalBatches: 0,
      totalEntries: 0,
      totalAddendas: 0,
      errors: ['Duplicado']
    }));

    component.upload();

    expect(api.upload).toHaveBeenCalledTimes(1);
    expect(component.lastUploadResult?.canReprocess).toBeTrue();
    expect(component.form.get('file')?.value).toBe(file);
    expect(notifications.warning).toHaveBeenCalledWith('Duplicado controlado');
  });

  it('renders the complete result surface and the foundation controls without ui-boton', () => {
    component.lastUploadResult = controlledReprocessResult();
    component.uploading = true;
    (component as any).cdr.markForCheck();
    fixture.detectChanges();

    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('app-page-header')).not.toBeNull();
    expect(root.querySelector('ui-grilla-empresarial')).not.toBeNull();
    expect(root.querySelector('ui-estado-carga')).not.toBeNull();
    expect(root.querySelector('app-clearing-house-select[formControlName="clearingHouseId"]')).not.toBeNull();
    expect(root.querySelector('input[type="file"]')).not.toBeNull();
    expect(root.querySelectorAll('button[mat-flat-button], button[mat-stroked-button]').length).toBeGreaterThan(0);
    expect(root.querySelector('ui-boton')).toBeNull();
    expect(root.querySelector('[data-testid="nacha-upload-result-message"]')?.textContent).toContain('Duplicado controlado');
    expect(root.querySelector('[data-testid="nacha-upload-ingestion-id"]')?.textContent).toContain('ing-1');
    expect(root.textContent).toContain('Rechazo controlado');
    expect(root.textContent).toContain('Resuelto');
    expect(root.textContent).toContain('FallidoReprocesable');
    expect(root.textContent).toContain('Lotes: 1');
    expect(root.textContent).toContain('Entradas: 2');
    expect(root.textContent).toContain('Adendas: 1');
  });
});
