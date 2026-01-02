import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { NachaExportApiService } from '../services/nacha-export-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ExportableAchCycle } from '../models/ach-cycle-export.model';
import { ClearingHouseOption } from '../models/ach-cycle.model';
import { ClearingHousesApiService } from '../services/ach-cycles-api.service';

interface ExportableAchCycleView extends ExportableAchCycle {
  processingDateText: string;
}

@Component({
  selector: 'app-nacha-export',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './nacha-export.component.html',
  styleUrls: ['./nacha-export.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaExportComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(NachaExportApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);

  cycles: ExportableAchCycleView[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  loading = false;
  downloadingId: string | null = null;

  readonly filterForm = this.fb.group({
    clearingHouseId: [null as number | null],
    startDate: [''],
    endDate: ['']
  });

  ngOnInit(): void {
    this.loadClearingHouses();
    this.load();
  }

  submit(): void {
    if (this.isInvalidDateRange()) {
      this.notifications.error('La fecha inicial no puede ser posterior a la fecha final');
      return;
    }

    this.load();
  }

  load(): void {
    this.loading = true;
    this.cdr.markForCheck();
    const filter = {
      clearingHouseId: this.filterForm.value.clearingHouseId ?? undefined,
      startDate: this.filterForm.value.startDate || undefined,
      endDate: this.filterForm.value.endDate || undefined
    };

    this.api.getExportableCycles(filter).subscribe({
      next: (items) => {
        const formatter = new Intl.DateTimeFormat('es-CO', {
          timeZone: 'UTC',
          year: 'numeric',
          month: '2-digit',
          day: '2-digit'
        });

        this.cycles = items.map((cycle) => ({
          ...cycle,
          processingDateText: formatter.format(new Date(cycle.processingDate))
        }));
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar los ciclos con transacciones');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  download(cycle: ExportableAchCycle, encrypted: boolean): void {
    this.downloadingId = cycle.id;
    this.api.downloadFile(cycle.id, encrypted).subscribe({
      next: (response) => {
        const fileName = this.extractFileName(response.headers.get('content-disposition')) ??
          `NACHA_${cycle.id}_${this.buildTimestamp()}.${encrypted ? 'ENV' : 'txt'}`;
        const blob = response.body ?? new Blob();
        const url = window.URL.createObjectURL(blob);

        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();

        window.URL.revokeObjectURL(url);
        this.downloadingId = null;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error(encrypted
          ? 'No fue posible generar el archivo NACHA-M con Sobre Digital'
          : 'No fue posible generar el archivo NACHA-M');
        this.downloadingId = null;
        this.cdr.markForCheck();
      }
    });
  }

  private loadClearingHouses(): void {
    this.clearingHouseApi.list().subscribe({
      next: (items) => {
        this.clearingHouses = items;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar las cámaras compensadoras');
        this.cdr.markForCheck();
      }
    });
  }

  private extractFileName(contentDisposition: string | null): string | null {
    if (!contentDisposition) {
      return null;
    }

    const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(contentDisposition);
    const fileName = match?.[1] ?? match?.[2];

    return fileName ? decodeURIComponent(fileName) : null;
  }

  private buildTimestamp(): string {
    const now = new Date();
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  }

  private isInvalidDateRange(): boolean {
    const { startDate, endDate } = this.filterForm.value;
    return Boolean(startDate && endDate && new Date(startDate) > new Date(endDate));
  }
}
