import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { IncomingNachaIngestionListItem } from '../models/incoming-nacha-command-center.models';
import { IncomingNachaCommandCenterApiService } from '../services/incoming-nacha-command-center-api.service';

@Component({
  selector: 'app-incoming-nacha-ingestions-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './incoming-nacha-ingestions-page.component.html',
  styleUrls: ['./incoming-nacha-ingestions-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IncomingNachaIngestionsPageComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterApiService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  cargando = false;
  error = '';
  totalItems = 0;
  items: IncomingNachaIngestionListItem[] = [];

  readonly migas = [{ etiqueta: 'Inicio', ruta: '/dashboard' }, { etiqueta: 'Command Center Inbound NACHA' }];
  readonly filtrosForm = this.fb.group({
    correlationId: [''],
    fileName: [''],
    ingestionStatus: [''],
    parsingStatus: [''],
    page: [1],
    pageSize: [20]
  });

  readonly columnas: ColDef<IncomingNachaIngestionListItem>[] = [
    { field: 'fileName', headerName: 'Archivo', minWidth: 180 },
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 220 },
    { field: 'ingestionStatus', headerName: 'Estado ingesta', minWidth: 150 },
    { field: 'parsingStatus', headerName: 'Estado parser', minWidth: 140 },
    { field: 'resolvedAchCycleId', headerName: 'Ciclo ACH', minWidth: 120 },
    { field: 'queueItems', headerName: 'Ítems cola', minWidth: 120 },
    { field: 'processingEvents', headerName: 'Eventos', minWidth: 100 },
    {
      headerName: 'Cargado',
      minWidth: 150,
      valueGetter: (p) => this.formatDate(p.data?.uploadedAtUtc)
    },
    {
      headerName: 'Acciones',
      minWidth: 160,
      sortable: false,
      filter: false,
      cellRenderer: () => '<button class="btn btn-outline btn-grid" data-action="detalle">Ver detalle</button>',
      onCellClicked: (params) => {
        const action = (params.event?.target as HTMLElement | null)?.getAttribute('data-action');
        if (action === 'detalle' && params.data) {
          this.router.navigate(['/incoming-nacha-command-center/ingestions', params.data.id]);
        }
      }
    }
  ];

  ngOnInit(): void {
    this.buscar();
  }

  buscar(): void {
    this.cargando = true;
    this.error = '';
    const raw = this.filtrosForm.getRawValue();
    const params: Record<string, string | number | boolean> = {
      page: raw.page ?? 1,
      pageSize: raw.pageSize ?? 20
    };

    if (raw.correlationId?.trim()) params['correlationId'] = raw.correlationId.trim();
    if (raw.fileName?.trim()) params['fileName'] = raw.fileName.trim();
    if (raw.ingestionStatus?.trim()) params['ingestionStatus'] = raw.ingestionStatus.trim();
    if (raw.parsingStatus?.trim()) params['parsingStatus'] = raw.parsingStatus.trim();

    this.api.getIngestions(params).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (result) => {
        this.items = result.items;
        this.totalItems = result.totalItems;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'No fue posible consultar ingestas inbound.';
      }
    });
  }

  limpiar(): void {
    this.filtrosForm.patchValue({
      correlationId: '',
      fileName: '',
      ingestionStatus: '',
      parsingStatus: '',
      page: 1,
      pageSize: 20
    });
    this.buscar();
  }

  irACola(): void {
    this.router.navigate(['/incoming-nacha-command-center/queue']);
  }

  irAObservabilidad(): void {
    this.router.navigate(['/incoming-nacha-command-center/observability']);
  }

  private formatDate(value?: string | null): string {
    if (!value) return '—';
    return new Date(value).toLocaleString('es-CO');
  }
}
