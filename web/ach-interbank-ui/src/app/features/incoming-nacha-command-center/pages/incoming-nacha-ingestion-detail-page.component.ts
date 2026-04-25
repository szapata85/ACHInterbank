import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { IncomingNachaIngestionDetail, IncomingNachaProcessingEvent, IncomingNachaQueueListItem } from '../models/incoming-nacha-command-center.models';
import { IncomingNachaCommandCenterApiService } from '../services/incoming-nacha-command-center-api.service';

@Component({
  selector: 'app-incoming-nacha-ingestion-detail-page',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
  templateUrl: './incoming-nacha-ingestion-detail-page.component.html',
  styleUrls: ['./incoming-nacha-ingestion-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IncomingNachaIngestionDetailPageComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  cargando = false;
  error = '';
  detalle?: IncomingNachaIngestionDetail;

  readonly columnasQueue: ColDef<IncomingNachaQueueListItem>[] = [
    { field: 'achTransactionId', headerName: 'Tx', minWidth: 90 },
    { field: 'queueStatus', headerName: 'Estado', minWidth: 130 },
    { field: 'priority', headerName: 'Prioridad', minWidth: 110 },
    { field: 'attemptCount', headerName: 'Intentos', minWidth: 100 },
    { headerName: 'Error', minWidth: 200, valueGetter: (p) => `${p.data?.lastErrorCode ?? ''} ${p.data?.lastErrorMessage ?? ''}`.trim() || '—' },
    { headerName: 'AllowedActions', minWidth: 190, valueGetter: (p) => p.data?.allowedActions?.allowedActions?.join(', ') || '—' },
    {
      headerName: 'Acciones',
      minWidth: 150,
      sortable: false,
      filter: false,
      cellRenderer: () => '<button class="btn btn-outline btn-grid" data-action="ver-queue">Abrir item</button>',
      onCellClicked: (params) => {
        const action = (params.event?.target as HTMLElement | null)?.getAttribute('data-action');
        if (action === 'ver-queue' && params.data) {
          this.router.navigate(['/incoming-nacha-command-center/queue', params.data.id]);
        }
      }
    }
  ];

  readonly columnasEvents: ColDef<IncomingNachaProcessingEvent>[] = [
    { field: 'eventType', headerName: 'Evento', minWidth: 160 },
    { field: 'eventStatus', headerName: 'Estado', minWidth: 120 },
    { field: 'raisedBy', headerName: 'Actor', minWidth: 150 },
    { headerName: 'Fecha', minWidth: 170, valueGetter: (p) => this.formatDate(p.data?.occurredAtUtc) },
    { field: 'message', headerName: 'Mensaje', minWidth: 420 }
  ];

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'No se recibió identificador de ingesta.';
      return;
    }

    this.cargando = true;
    this.error = '';
    this.api.getIngestionDetail(id).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (detalle) => {
        this.detalle = detalle;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'No fue posible cargar el detalle de ingesta.';
      }
    });
  }

  volver(): void {
    this.router.navigate(['/incoming-nacha-command-center']);
  }

  irACola(): void {
    this.router.navigate(['/incoming-nacha-command-center/queue'], {
      queryParams: this.detalle ? { ingestionId: this.detalle.id } : undefined
    });
  }

  private formatDate(value?: string | null): string {
    if (!value) return '—';
    return new Date(value).toLocaleString('es-CO');
  }
}
