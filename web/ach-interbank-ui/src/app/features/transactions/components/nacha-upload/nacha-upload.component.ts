import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs/operators';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { NachaUploadRecord, NachaUploadService } from '../../services/nacha-upload.service';

const achColombiaProductionReferencePattern = /^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$/i;
const cenitProductionReferencePattern = /^\d{7}\.\d{3}\.\d{8}\.\d+$/;
const legacyOperationalPattern = /^\d{7}\.\d{3}\.[1-9]\d*$/;
const officialReturnPattern = /^\d{7}\.\d{3}\.RET$/i;
const internalFixturePattern = /\.ach$/i;
const digitalEnvelopePattern = /\.env$/i;
const rejectedExtensionPattern = /\.(txt|nacha)$/i;

@Component({
  selector: 'app-nacha-upload',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './nacha-upload.component.html',
  styleUrls: ['./nacha-upload.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaUploadComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly nachaUpload = inject(NachaUploadService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('fileInput') private readonly fileInput?: ElementRef<HTMLInputElement>;

  uploading = false;
  loadingRecords = false;
  records: NachaUploadRecord[] = [];
  lastUploadResult: NachaUploadResultView | null = null;
  selectedFileValidation: NachaUploadFileValidation | null = null;

  readonly columnDefs: ColDef<NachaUploadRecord>[] = [
    { headerName: 'Nacha ID', valueGetter: (params) => params.data?.nachaId || '-' },
    { headerName: 'Originador', valueGetter: (params) => params.data?.immediateOriginName || params.data?.immediateOrigin || '-' },
    { headerName: 'Receptor', valueGetter: (params) => params.data?.immediateDestinationName || params.data?.immediateDestination || '-' },
    { field: 'clearingHouseName', headerName: 'Cámara', valueGetter: (params) => params.data?.clearingHouseName || '-' },
    { headerName: 'Ciclo', valueGetter: (params) => params.data?.achCycleName || params.data?.achCycleId || '-' },
    { headerName: 'Fecha archivo', valueGetter: (params) => params.data?.fileCreationDate || '-' },
    { headerName: 'Hora', valueGetter: (params) => params.data?.fileCreationTime || '-' },
    { field: 'totalBatches', headerName: 'Lotes' },
    { field: 'totalEntries', headerName: 'Entradas' },
    { field: 'totalAddendas', headerName: 'Adendas' },
    { field: 'totalAmount', headerName: 'Total monto', valueFormatter: (params) => Number(params.value ?? 0).toFixed(2) },
    { field: 'totalDebitAmount', headerName: 'Total débito', valueFormatter: (params) => Number(params.value ?? 0).toFixed(2) },
    { field: 'totalCreditAmount', headerName: 'Total crédito', valueFormatter: (params) => Number(params.value ?? 0).toFixed(2) }
  ];

  form = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    file: [null as File | null, Validators.required]
  });

  filtersForm = this.fb.group({
    immediateOrigin: [''],
    immediateDestination: [''],
    referenceCode: [''],
    achCycleId: [''],
    fileCreationDate: ['']
  });

  ngOnInit(): void {
    this.loadRecords();
  }

  get fileName(): string {
    return this.form.get('file')?.value?.name ?? '';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.form.patchValue({ file });
    this.form.markAsTouched();
    this.selectedFileValidation = file ? classifyNachaUploadFile(file.name) : null;
    this.lastUploadResult = null;
    this.cdr.markForCheck();
  }

  upload(): void {
    this.submitUpload(false);
  }

  reprocess(): void {
    if (!this.lastUploadResult?.canReprocess || !this.lastUploadResult.ingestionId) {
      this.notifications.error('La ingesta actual no está habilitada para reproceso controlado.');
      return;
    }

    this.submitUpload(true, this.lastUploadResult.ingestionId);
  }

  private submitUpload(forceReprocess: boolean, parentIngestionId?: string): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Selecciona una cámara y un archivo NACHA-M para continuar.');
      return;
    }

    const clearingHouseId = this.form.get('clearingHouseId')?.value;
    const file = this.form.get('file')?.value;
    if (!clearingHouseId || !file) {
      this.notifications.error('Selecciona una cámara y un archivo NACHA-M para continuar.');
      return;
    }

    const validation = this.selectedFileValidation ?? classifyNachaUploadFile(file.name);
    this.selectedFileValidation = validation;

    if (!validation.allowed) {
      this.notifications.error(validation.rejectionMessage || 'Formato NACHA-M no permitido.');
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    this.uploading = true;
    this.nachaUpload
      .upload(file, clearingHouseId, { forceReprocess, parentIngestionId })
      .pipe(
        finalize(() => {
          this.uploading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (response) => {
          this.lastUploadResult = this.toUploadResult(response, 200, clearingHouseId);
          this.notifyUploadResult(this.lastUploadResult);
          if (!this.lastUploadResult.canReprocess) {
            this.resetFileSelection();
          }
          this.loadRecords();
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          this.lastUploadResult = this.toUploadResult(
            this.extractErrorBody(error),
            this.extractStatus(error),
            clearingHouseId
          );
          this.notifyUploadResult(this.lastUploadResult);
          if (!this.lastUploadResult.canReprocess) {
            this.resetFileSelection();
          }
          this.loadRecords();
          this.cdr.markForCheck();
        }
      });
  }

  private notifyUploadResult(result: NachaUploadResultView): void {
    const message = result.message || 'No fue posible cargar el archivo NACHA-M.';
    switch (classifyNachaUploadNotification(result)) {
      case 'success':
        this.notifications.success(message);
        break;
      case 'warning':
        this.notifications.warning(message);
        break;
      default:
        this.notifications.error(message);
        break;
    }
  }

  searchRecords(): void {
    this.loadRecords();
  }

  clearFilters(): void {
    this.filtersForm.reset({
      immediateOrigin: '',
      immediateDestination: '',
      referenceCode: '',
      achCycleId: '',
      fileCreationDate: ''
    });
    this.loadRecords();
  }

  private loadRecords(): void {
    this.loadingRecords = true;
    const filters = this.filtersForm.getRawValue();

    this.nachaUpload
      .listRecords(filters)
      .pipe(
        finalize(() => {
          this.loadingRecords = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (records) => {
          this.records = records ?? [];
          this.cdr.markForCheck();
        },
        error: () => {
          this.records = [];
          this.notifications.error('No fue posible consultar el detalle de archivos NACHA-M cargados.');
          this.cdr.markForCheck();
        }
      });
  }

  private resetFileSelection(): void {
    this.form.reset({ clearingHouseId: null, file: null });
    this.selectedFileValidation = null;
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }

  private extractStatus(error: unknown): number {
    if (error instanceof HttpErrorResponse) {
      return error.status;
    }

    if (error && typeof error === 'object' && 'status' in error) {
      const value = (error as { status?: unknown }).status;
      return typeof value === 'number' ? value : 0;
    }

    return 0;
  }

  private extractErrorBody(error: unknown): unknown {
    if (error instanceof HttpErrorResponse) {
      return error.error;
    }

    if (error && typeof error === 'object' && 'error' in error) {
      return (error as { error?: unknown }).error;
    }

    return error;
  }

  private toUploadResult(
    payload: unknown,
    statusCode: number,
    selectedClearingHouseId: number
  ): NachaUploadResultView {
    const body = payload && typeof payload === 'object' ? (payload as Record<string, unknown>) : {};
    const success = this.readBoolean(body, 'success');
    const partial = this.readBoolean(body, 'partial');
    const message = this.readString(body, 'message');
    const errors = this.readStringArray(body, 'errors');
    const traceId = this.readString(body, 'traceId');
    const ingestionId = this.readString(body, 'ingestionId');
    const ingestionStatus = this.readString(body, 'ingestionStatus');
    const cycleResolutionStatus = this.readString(body, 'cycleResolutionStatus');
    const parsingStatus = this.readString(body, 'parsingStatus');
    const detectedClearingHouseId = this.readNumber(body, 'detectedClearingHouseId');
    const resolvedClearingHouseId = this.readNumber(body, 'resolvedClearingHouseId');
    const totalBatches = this.readNumber(body, 'totalBatches');
    const totalEntries = this.readNumber(body, 'totalEntries');
    const totalAddendas = this.readNumber(body, 'totalAddendas');
    const canReprocess = Boolean(ingestionId)
      && ['Fallido', 'Duplicado'].includes(ingestionStatus)
      && ['FallidoReprocesable', 'EnProceso'].includes(parsingStatus);

    const lowerMessage = message.toLowerCase();
    const isControlledRejection = statusCode >= 400
      || lowerMessage.includes('duplic')
      || lowerMessage.includes('bloque')
      || lowerMessage.includes('pendient')
      || lowerMessage.includes('rechaz');

    const statusLabel = isControlledRejection
      ? 'Rechazo controlado'
      : success && !partial
        ? 'Procesado correctamente'
        : partial
          ? 'Procesado con observaciones'
          : 'Recepción controlada';

    return {
      statusCode,
      statusLabel,
      success,
      partial,
      message,
      errors,
      traceId,
      ingestionId,
      canReprocess,
      ingestionStatus,
      cycleResolutionStatus,
      parsingStatus,
      selectedClearingHouseId,
      detectedClearingHouseId,
      resolvedClearingHouseId,
      totalBatches,
      totalEntries,
      totalAddendas
    };
  }

  private readBoolean(body: Record<string, unknown>, key: string): boolean {
    const value = this.readRawValue(body, key);
    return typeof value === 'boolean' ? value : false;
  }

  private readString(body: Record<string, unknown>, key: string): string {
    const value = this.readRawValue(body, key);
    return typeof value === 'string' ? value : '';
  }

  private readNumber(body: Record<string, unknown>, key: string): number | null {
    const value = this.readRawValue(body, key);
    return typeof value === 'number' ? value : null;
  }

  private readStringArray(body: Record<string, unknown>, key: string): string[] {
    const value = this.readRawValue(body, key);
    return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : [];
  }

  private readRawValue(body: Record<string, unknown>, key: string): unknown {
    return body[key] ?? body[key[0].toUpperCase() + key.slice(1)];
  }
}

export function classifyNachaUploadFile(fileName: string): NachaUploadFileValidation {
  const normalizedName = fileName.trim().split(/[\\/]/).pop() ?? '';

  if (!normalizedName) {
    return {
      allowed: false,
      kind: 'rejected',
      label: 'Formato no permitido',
      detail: 'Selecciona un archivo NACHA-M válido.',
      rejectionMessage: 'Selecciona un archivo NACHA-M válido.'
    };
  }

  if (rejectedExtensionPattern.test(normalizedName)) {
    return {
      allowed: false,
      kind: 'rejected',
      label: 'Formato no permitido',
      detail: 'Los archivos .txt y .nacha no se admiten en NachaUpload.',
      rejectionMessage: 'Los archivos .txt y .nacha no se admiten en NachaUpload.'
    };
  }

  if (digitalEnvelopePattern.test(normalizedName)) {
    return {
      allowed: true,
      kind: 'digital-envelope-achcol',
      label: 'Sobre digital ACH Colombia',
      detail: 'Archivo cifrado .ENV; requiere seleccionar ACH Colombia.',
      rejectionMessage: ''
    };
  }

  if (achColombiaProductionReferencePattern.test(normalizedName)) {
    return {
      allowed: true,
      kind: 'production-reference-achcol',
      label: 'Referencia productiva ACH Colombia',
      detail: 'Patrón operativo RRRRTTT.ZZZ.YYYYMMDD.N.OUT; no implica homologación normativa.',
      rejectionMessage: ''
    };
  }

  if (cenitProductionReferencePattern.test(normalizedName)) {
    return {
      allowed: true,
      kind: 'production-reference-cenit',
      label: 'Referencia productiva CENIT',
      detail: 'Patrón operativo RRRRTTT.ZZZ.YYYYMMDD.N; no implica homologación normativa.',
      rejectionMessage: ''
    };
  }

  if (legacyOperationalPattern.test(normalizedName)) {
    return {
      allowed: true,
      kind: 'official-ach',
      label: 'Archivo operativo ACH Colombia',
      detail: 'Patrón normativo: RRRRTTT.ZZZ.N',
      rejectionMessage: ''
    };
  }

  if (officialReturnPattern.test(normalizedName)) {
    return {
      allowed: true,
      kind: 'official-ret',
      label: 'Devolución ACH Colombia',
      detail: 'Patrón normativo: RRRRTTT.ZZZ.RET',
      rejectionMessage: ''
    };
  }

  if (internalFixturePattern.test(normalizedName)) {
    return {
      allowed: true,
      kind: 'uat-fixture',
      label: 'Fixture UAT/golden interno',
      detail: 'Snapshot funcional semirreal para validación técnica.',
      rejectionMessage: ''
    };
  }

  return {
    allowed: false,
    kind: 'rejected',
    label: 'Formato no permitido',
      detail: 'Usa un nombre operativo ACHCOL/CENIT, RRRRTTT.ZZZ.RET o un fixture UAT .ach.',
      rejectionMessage: 'Formato no permitido. Usa un nombre operativo ACHCOL/CENIT, RRRRTTT.ZZZ.RET o un fixture UAT .ach.'
    };
  }

export type NachaUploadResultView = {
  statusCode: number;
  statusLabel: string;
  success: boolean;
  partial: boolean;
  message: string;
  errors: string[];
  traceId: string;
  ingestionId: string;
  canReprocess: boolean;
  ingestionStatus: string;
  cycleResolutionStatus: string;
  parsingStatus: string;
  selectedClearingHouseId: number;
  detectedClearingHouseId: number | null;
  resolvedClearingHouseId: number | null;
  totalBatches: number | null;
  totalEntries: number | null;
  totalAddendas: number | null;
};

export function classifyNachaUploadNotification(
  result: Pick<NachaUploadResultView, 'success' | 'partial' | 'canReprocess' | 'statusCode'>
): 'success' | 'warning' | 'error' {
  if (result.success && !result.partial) {
    return 'success';
  }

  if (result.canReprocess || result.statusCode < 500) {
    return 'warning';
  }

  return 'error';
}

export type NachaUploadFileValidation = {
  allowed: boolean;
  kind: 'digital-envelope-achcol' | 'production-reference-achcol' | 'production-reference-cenit' | 'official-ach' | 'official-ret' | 'uat-fixture' | 'rejected';
  label: string;
  detail: string;
  rejectionMessage: string;
};
