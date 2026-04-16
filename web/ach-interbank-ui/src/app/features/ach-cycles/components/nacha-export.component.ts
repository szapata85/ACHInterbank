import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ColDef, GridApi } from 'ag-grid-community';
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
  private gridApi?: GridApi<ExportableAchCycleView>;

  readonly columnas: ColDef<ExportableAchCycleView>[] = [
    { field: 'cycleName', headerName: 'Ciclo', minWidth: 160 },
    { field: 'clearingHouseName', headerName: 'Cámara', minWidth: 200 },
    { field: 'processingDateText', headerName: 'Fecha efectiva', minWidth: 160 },
    { field: 'transactionCount', headerName: 'Transacciones', width: 140, cellStyle: { textAlign: 'right' } },
    {
      colId: 'acciones',
      headerName: 'Acciones',
      minWidth: 250,
      width: 280,
      maxWidth: 320,
      sortable: false,
      filter: false,
      cellRenderer: (params) => {
        const rowId = params.data?.id;
        const ocupado = Boolean(rowId && this.downloadingId === rowId);
        const disabledAttr = ocupado ? 'disabled aria-disabled="true"' : '';
        const tooltipGenerar = ocupado ? 'Generación en curso' : 'Generar archivo NACHA-M';
        const tooltipSobre = ocupado ? 'Generación en curso' : 'Generar archivo con sobre digital';
        const textoGenerar = ocupado ? 'Generando...' : 'Generar archivo NACHA';
        const textoSobre = ocupado ? 'Generando...' : 'Generar con sobre digital';

        return `
          <div class="acciones-fila-nacha">
            <button type="button" class="btn btn-primary btn-grid" data-action="generar-nacha" title="${tooltipGenerar}" ${disabledAttr}>${textoGenerar}</button>
            <button type="button" class="btn btn-outline btn-grid" data-action="generar-sobre" title="${tooltipSobre}" ${disabledAttr}>${textoSobre}</button>
          </div>
        `;
      },
      onCellClicked: (params) => {
        if (!params.data || this.downloadingId) {
          return;
        }

        const target = params.event?.target as HTMLElement | null;
        const actionElement = target?.closest<HTMLElement>('[data-action]');
        const action = actionElement?.getAttribute('data-action');

        if (action === 'generar-nacha') {
          this.download(params.data, false);
        }
        if (action === 'generar-sobre') {
          this.download(params.data, true);
        }
      }
    }
  ];

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
    if (this.downloadingId) {
      return;
    }

    this.downloadingId = cycle.id;
    this.refrescarAccionesGrilla();
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
        this.refrescarAccionesGrilla();
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error(encrypted
          ? 'No fue posible generar el archivo NACHA-M con Sobre Digital'
          : 'No fue posible generar el archivo NACHA-M');
        this.downloadingId = null;
        this.refrescarAccionesGrilla();
        this.cdr.markForCheck();
      }
    });
  }

  onGrillaLista(api: GridApi<ExportableAchCycleView>): void {
    this.gridApi = api;
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

  private refrescarAccionesGrilla(): void {
    this.gridApi?.refreshCells({ force: true, columns: ['acciones'] });
  }
}
