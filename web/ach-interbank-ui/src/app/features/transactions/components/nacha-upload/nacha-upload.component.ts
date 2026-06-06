import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs/operators';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { NachaUploadRecord, NachaUploadService } from '../../services/nacha-upload.service';

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
    this.lastUploadResult = null;
    this.cdr.markForCheck();
  }

  upload(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Selecciona un archivo NACHA-M para continuar.');
      return;
    }

    const file = this.form.get('file')?.value;
    if (!file) {
      this.notifications.error('Selecciona un archivo NACHA-M para continuar.');
      return;
    }

    this.uploading = true;
    this.nachaUpload
      .upload(file)
      .pipe(
        finalize(() => {
          this.uploading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (response) => {
          this.lastUploadResult = this.toUploadResult(response, 200);
          this.notifications.success(this.lastUploadResult.message || 'Archivo cargado correctamente.');
          this.resetFileSelection();
          this.loadRecords();
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          this.lastUploadResult = this.toUploadResult(this.extractErrorBody(error), this.extractStatus(error));
          this.notifications.error(this.lastUploadResult.message || 'No fue posible cargar el archivo NACHA-M.');
          this.resetFileSelection();
          this.loadRecords();
          this.cdr.markForCheck();
        }
      });
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
    this.form.reset({ file: null });
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

  private toUploadResult(payload: unknown, statusCode: number): NachaUploadResultView {
    const body = payload && typeof payload === 'object' ? (payload as Record<string, unknown>) : {};
    const success = this.readBoolean(body, 'success');
    const partial = this.readBoolean(body, 'partial');
    const message = this.readString(body, 'message');
    const errors = this.readStringArray(body, 'errors');
    const traceId = this.readString(body, 'traceId');
    const ingestionStatus = this.readString(body, 'ingestionStatus');
    const cycleResolutionStatus = this.readString(body, 'cycleResolutionStatus');
    const parsingStatus = this.readString(body, 'parsingStatus');
    const totalBatches = this.readNumber(body, 'totalBatches');
    const totalEntries = this.readNumber(body, 'totalEntries');
    const totalAddendas = this.readNumber(body, 'totalAddendas');

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
      ingestionStatus,
      cycleResolutionStatus,
      parsingStatus,
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

type NachaUploadResultView = {
  statusCode: number;
  statusLabel: string;
  success: boolean;
  partial: boolean;
  message: string;
  errors: string[];
  traceId: string;
  ingestionStatus: string;
  cycleResolutionStatus: string;
  parsingStatus: string;
  totalBatches: number | null;
  totalEntries: number | null;
  totalAddendas: number | null;
};
