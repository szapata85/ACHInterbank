import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { IncomingNachaQueueListItem } from '../models/incoming-nacha-command-center.models';
import { IncomingNachaCommandCenterApiService } from '../services/incoming-nacha-command-center-api.service';
import { supportActionsLabel, supportStatusLabel } from '../presentation/incoming-nacha-support-presentation';

@Component({
  selector: 'app-incoming-nacha-queue-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './incoming-nacha-queue-page.component.html',
  styleUrls: ['./incoming-nacha-queue-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IncomingNachaQueuePageComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterApiService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  cargando = false;
  error = '';
  totalItems = 0;
  items: IncomingNachaQueueListItem[] = [];

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/dashboard' },
    { etiqueta: 'Seguimiento de archivos NACHA-M', ruta: '/incoming-nacha-command-center' },
    { etiqueta: 'Cola de procesamiento' }
  ];

  readonly filtrosForm = this.fb.group({
    ingestionId: [''],
    queueStatus: [''],
    achTransactionId: [''],
    correlationId: [''],
    page: [1],
    pageSize: [20]
  });

  readonly columnas: ColDef<IncomingNachaQueueListItem>[] = [
    { field: 'achTransactionId', headerName: 'Tx', minWidth: 100 },
    { headerName: 'Estado del procesamiento', minWidth: 190, valueGetter: (p) => supportStatusLabel(p.data?.queueStatus) },
    { field: 'priority', headerName: 'Prioridad', minWidth: 110 },
    { field: 'attemptCount', headerName: 'Intentos', minWidth: 100 },
    { headerName: 'Próximo intento', minWidth: 170, valueGetter: (p) => this.formatDate(p.data?.nextAttemptAtUtc) },
    { headerName: 'Último error', minWidth: 260, valueGetter: (p) => this.composeError(p.data) },
    { headerName: 'Acciones autorizadas', minWidth: 230, valueGetter: (p) => this.composeAllowedActions(p.data) },
    {
      headerName: 'Acciones',
      minWidth: 150,
      sortable: false,
      filter: false,
      cellRenderer: () => '<button class="btn btn-outline btn-grid" data-action="detalle">Ver detalle</button>',
      onCellClicked: (params) => {
        const action = (params.event?.target as HTMLElement | null)?.getAttribute('data-action');
        if (action === 'detalle' && params.data) {
          this.router.navigate(['/incoming-nacha-command-center/queue', params.data.id]);
        }
      }
    }
  ];

  ngOnInit(): void {
    const queryIngestionId = this.route.snapshot.queryParamMap.get('ingestionId');
    if (queryIngestionId) {
      this.filtrosForm.patchValue({ ingestionId: queryIngestionId });
    }
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

    if (raw.ingestionId?.trim()) params['ingestionId'] = raw.ingestionId.trim();
    if (raw.queueStatus?.trim()) params['queueStatus'] = raw.queueStatus.trim();
    if (raw.achTransactionId?.trim()) params['achTransactionId'] = raw.achTransactionId.trim();
    if (raw.correlationId?.trim()) params['correlationId'] = raw.correlationId.trim();

    this.api.getQueue(params).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (result) => {
        this.items = result.items;
        this.totalItems = result.totalItems;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'No fue posible consultar la programación del procesamiento.';
      }
    });
  }

  limpiar(): void {
    this.filtrosForm.patchValue({
      ingestionId: '',
      queueStatus: '',
      achTransactionId: '',
      correlationId: '',
      page: 1,
      pageSize: 20
    });
    this.buscar();
  }

  volverIngestas(): void {
    this.router.navigate(['/incoming-nacha-command-center']);
  }

  private composeAllowedActions(row?: IncomingNachaQueueListItem | null): string {
    return supportActionsLabel(row?.allowedActions?.allowedActions);
  }

  private composeError(row?: IncomingNachaQueueListItem | null): string {
    if (!row) return '—';
    const error = `${row.lastErrorCode ?? ''} ${row.lastErrorMessage ?? ''}`.trim();
    return error || '—';
  }

  private formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString('es-CO') : '—';
  }
}
