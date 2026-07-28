import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs/operators';
import {
  ApplicationDownloadError,
  BlobDownloadService
} from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { ExportableAchCycle } from '../models/ach-cycle-export.model';
import { ClearingHouseOption } from '../models/ach-cycle.model';
import { ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { NachaExportApiService } from '../services/nacha-export-api.service';

type ExportAction = 'plain' | 'encrypted';

interface ExportOperationError {
  message: string;
  errorCode?: string;
  traceId?: string;
  cycle: ExportableAchCycle;
  action: ExportAction;
}

@Component({
  selector: 'app-nacha-export',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './nacha-export.component.html',
  styleUrls: ['./nacha-export.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaExportComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(NachaExportApiService);
  private readonly downloads = inject(BlobDownloadService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);

  readonly displayedColumns = ['clearingHouse', 'cycle', 'processingDate', 'transactions', 'status', 'fileName', 'actions'];
  readonly filterForm = this.fb.group({
    clearingHouseId: [null as number | null],
    startDate: [null as Date | null],
    endDate: [null as Date | null],
    search: ['']
  }, { validators: dateRangeValidator() });

  cycles: ExportableAchCycle[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  loading = false;
  loadingClearingHouses = false;
  lastUpdatedAt: Date | null = null;
  loadError: string | null = null;
  operationError: ExportOperationError | null = null;
  private readonly operations = new Map<string, ExportAction>();

  get visibleCycles(): ExportableAchCycle[] {
    const search = this.filterForm.controls.search.value?.trim().toLocaleLowerCase('es-CO') ?? '';
    if (!search) {
      return this.cycles;
    }
    return this.cycles.filter(cycle =>
      cycle.cycleName.toLocaleLowerCase('es-CO').includes(search)
      || (cycle.clearingHouseName ?? '').toLocaleLowerCase('es-CO').includes(search)
      || (cycle.fileName ?? '').toLocaleLowerCase('es-CO').includes(search)
      || cycle.cycleId?.toLocaleLowerCase('es-CO').includes(search));
  }

  get summary(): { total: number; ready: number; generated: number; blocked: number; protected: number } {
    return {
      total: this.cycles.length,
      ready: this.cycles.filter(cycle => cycle.isExportable && !cycle.hasGeneratedFile).length,
      generated: this.cycles.filter(cycle => cycle.hasGeneratedFile).length,
      blocked: this.cycles.filter(cycle => !cycle.isExportable).length,
      protected: this.cycles.filter(cycle => cycle.hasDigitalEnvelope).length
    };
  }

  ngOnInit(): void {
    this.loadClearingHouses();
    this.load();
  }

  submit(): void {
    this.filterForm.markAllAsTouched();
    if (this.filterForm.invalid || this.loading) {
      return;
    }
    this.load();
  }

  clearFilters(): void {
    this.filterForm.reset({
      clearingHouseId: null,
      startDate: null,
      endDate: null,
      search: ''
    });
    if (!this.loading) {
      this.load();
    }
  }

  load(): void {
    if (this.loading) {
      return;
    }
    const raw = this.filterForm.getRawValue();
    this.loading = true;
    this.filterForm.disable({ emitEvent: false });
    this.loadError = null;
    this.cdr.markForCheck();

    this.api.getExportableCycles({
      clearingHouseId: raw.clearingHouseId ?? undefined,
      startDate: toApiDate(raw.startDate),
      endDate: toApiDate(raw.endDate)
    }).pipe(finalize(() => {
      this.loading = false;
      this.filterForm.enable({ emitEvent: false });
      this.cdr.markForCheck();
    })).subscribe({
      next: items => {
        this.cycles = items;
        this.lastUpdatedAt = new Date();
      },
      error: () => {
        this.loadError = 'No fue posible consultar los ciclos disponibles. Verifica la conexión e inténtalo nuevamente.';
      }
    });
  }

  downloadPlain(cycle: ExportableAchCycle): void {
    this.download(cycle, 'plain');
  }

  downloadEncrypted(cycle: ExportableAchCycle): void {
    this.download(cycle, 'encrypted');
  }

  retryOperation(): void {
    const retry = this.operationError;
    if (!retry) {
      return;
    }
    this.operationError = null;
    this.download(retry.cycle, retry.action);
  }

  isProcessing(cycle: ExportableAchCycle): boolean {
    return Boolean(cycle.cycleId && this.operations.has(cycle.cycleId));
  }

  processingAction(cycle: ExportableAchCycle): ExportAction | null {
    return cycle.cycleId ? this.operations.get(cycle.cycleId) ?? null : null;
  }

  actionUnavailableReason(cycle: ExportableAchCycle): string {
    if (!cycle.cycleId?.trim()) {
      return 'El ciclo no tiene un identificador de exportación válido.';
    }
    return cycle.exportUnavailableReason ?? 'Este ciclo no cumple las condiciones para exportar NACHA-M.';
  }

  statusLabel(cycle: ExportableAchCycle): string {
    if (!cycle.isExportable) {
      return 'Bloqueado';
    }
    if (cycle.hasDigitalEnvelope) {
      return 'Protegido';
    }
    if (cycle.hasGeneratedFile) {
      return 'Generado';
    }
    return 'Disponible';
  }

  statusClass(cycle: ExportableAchCycle): string {
    return `status-${this.statusLabel(cycle).toLocaleLowerCase('es-CO')}`;
  }

  trackCycle(_: number, cycle: ExportableAchCycle): string {
    return cycle.cycleId ?? cycle.id;
  }

  private download(cycle: ExportableAchCycle, action: ExportAction): void {
    const cycleId = cycle.cycleId?.trim();
    if (!cycle.isExportable) {
      this.notifications.info(this.actionUnavailableReason(cycle));
      return;
    }
    if (!cycleId) {
      this.notifications.error('No fue posible exportar: el ciclo no tiene un identificador válido.');
      return;
    }
    if (this.operations.has(cycleId)) {
      return;
    }

    this.operations.set(cycleId, action);
    this.operationError = null;
    this.cdr.markForCheck();
    const encrypted = action === 'encrypted';

    this.api.downloadFile(cycleId, encrypted).pipe(finalize(() => {
      this.operations.delete(cycleId);
      this.cdr.markForCheck();
    })).subscribe({
      next: response => {
        void this.saveDownload(response, cycle, action);
      },
      error: error => {
        void this.handleDownloadError(error, cycle, action);
      }
    });
  }

  private async saveDownload(
    response: Parameters<BlobDownloadService['save']>[0],
    cycle: ExportableAchCycle,
    action: ExportAction
  ): Promise<void> {
    try {
      const result = await this.downloads.save(response);
      this.notifications.success(
        action === 'encrypted'
          ? `Sobre digital generado: ${result.fileName}`
          : `Archivo NACHA-M descargado: ${result.fileName}`
      );
      this.load();
    } catch (error) {
      await this.handleDownloadError(error, cycle, action);
    }
  }

  private async handleDownloadError(error: unknown, cycle: ExportableAchCycle, action: ExportAction): Promise<void> {
    const fallback = action === 'encrypted'
      ? 'No fue posible generar y proteger el archivo NACHA-M.'
      : 'No fue posible generar el archivo NACHA-M.';
    const parsed: ApplicationDownloadError = await this.downloads.fromHttpError(error, fallback);
    this.operationError = {
      message: parsed.message,
      errorCode: parsed.errorCode,
      traceId: parsed.traceId,
      cycle,
      action
    };
    this.notifications.error(this.formatError(parsed));
    this.cdr.markForCheck();
  }

  private formatError(error: ApplicationDownloadError): string {
    const code = error.errorCode ? ` (${error.errorCode})` : '';
    const trace = error.traceId ? ` [traceId: ${error.traceId}]` : '';
    return `${error.message}${code}${trace}`;
  }

  private loadClearingHouses(): void {
    this.loadingClearingHouses = true;
    this.clearingHouseApi.list().pipe(finalize(() => {
      this.loadingClearingHouses = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: items => {
        this.clearingHouses = items;
      },
      error: () => {
        this.notifications.error('No fue posible cargar las cámaras compensadoras.');
      }
    });
  }
}

function dateRangeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const start = control.get('startDate')?.value as Date | null;
    const end = control.get('endDate')?.value as Date | null;
    return start && end && start.getTime() > end.getTime() ? { dateRange: true } : null;
  };
}

function toApiDate(value: Date | null): string | undefined {
  if (!value) {
    return undefined;
  }
  const pad = (part: number) => part.toString().padStart(2, '0');
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`;
}
