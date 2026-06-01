import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { forkJoin, finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { OpcionSelectorBuscable } from '../../../shared/components/ui/ui-selector-buscable.component';
import {
  NachaConfigFilterCatalogs,
  NachaConfigProfileReadModel,
  NachaConfigProfilesDashboardReadModel
} from '../models/nacha-config-admin.models';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

@Component({
  selector: 'app-nacha-config-profiles-page',
  templateUrl: './nacha-config-profiles-page.component.html',
  styleUrls: ['./nacha-config-profiles-page.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigProfilesPageComponent implements OnInit {
  private readonly query = inject(NachaConfigQueryService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  cargando = false;
  errorCarga = false;
  dashboard: NachaConfigProfilesDashboardReadModel | null = null;
  perfiles: NachaConfigProfileReadModel[] = [];
  visibles: NachaConfigProfileReadModel[] = [];
  catalogos: NachaConfigFilterCatalogs = { estados: [], camaras: [], flujos: [], direcciones: [], servicios: [] };

  readonly filtrosForm = this.fb.group({
    texto: [''],
    estado: ['TODOS'],
    camara: ['TODAS'],
    flujo: ['TODOS']
  });

  get opcionesEstado(): OpcionSelectorBuscable[] {
    return [
      { valor: 'TODOS', etiqueta: 'Todos' },
      ...this.catalogos.estados.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }))
    ];
  }

  get opcionesCamara(): OpcionSelectorBuscable[] {
    return [
      { valor: 'TODAS', etiqueta: 'Todas' },
      ...this.catalogos.camaras.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }))
    ];
  }

  get opcionesFlujo(): OpcionSelectorBuscable[] {
    return [
      { valor: 'TODOS', etiqueta: 'Todos' },
      ...this.catalogos.flujos.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }))
    ];
  }

  readonly columnas: ColDef<NachaConfigProfileReadModel>[] = [
    { field: 'profileCode', headerName: 'Codigo', minWidth: 160 },
    { field: 'profileName', headerName: 'Nombre', minWidth: 240 },
    { field: 'clearingHouseCode', headerName: 'Camara', minWidth: 120 },
    { field: 'status', headerName: 'Estado', minWidth: 130 },
    { field: 'version', headerName: 'Version', minWidth: 110 },
    { field: 'layoutVariantCount', headerName: 'Variants', minWidth: 100 },
    { field: 'fieldCount', headerName: 'Fields', minWidth: 100 },
    { headerName: 'Records', minWidth: 140, valueGetter: (p) => (p.data?.recordTypes ?? []).join(', ') },
    { headerName: 'Vigencia', minWidth: 220, valueGetter: (p) => `${this.date(p.data?.effectiveFrom)} a ${p.data?.effectiveTo ? this.date(p.data.effectiveTo) : 'abierta'}` },
    {
      headerName: 'Acciones',
      minWidth: 160,
      sortable: false,
      filter: false,
      cellRenderer: () => '<button class="btn btn-outline btn-grid" data-action="ver">Ver detalle</button>',
      onCellClicked: (params) => {
        const action = (params.event?.target as HTMLElement | null)?.getAttribute('data-action');
        if (action === 'ver' && params.data) {
          this.router.navigate(['/nacha-config-admin/perfiles', params.data.profileId]);
        }
      }
    }
  ];

  ngOnInit(): void {
    this.cargar();
    this.filtrosForm.valueChanges.subscribe(() => this.aplicarFiltros());
  }

  cargar(): void {
    this.cargando = true;
    this.errorCarga = false;

    forkJoin({
      catalogos: this.query.catalogosFiltro(),
      dashboard: this.query.dashboardReadOnly(),
      perfiles: this.query.perfilesReadOnly()
    }).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: ({ catalogos, dashboard, perfiles }) => {
        this.catalogos = catalogos;
        this.dashboard = dashboard;
        this.perfiles = perfiles;
        this.aplicarFiltros();
      },
      error: () => {
        this.errorCarga = true;
        this.notifications.error('No fue posible cargar los perfiles NACHA read-only.');
      }
    });
  }

  aplicarFiltros(): void {
    const filtro = this.filtrosForm.getRawValue();
    const texto = (filtro.texto ?? '').trim().toLowerCase();

    this.visibles = this.perfiles.filter((p) => {
      const textOk = !texto || `${p.profileCode} ${p.profileName}`.toLowerCase().includes(texto);
      const estadoOk = filtro.estado === 'TODOS' || p.status === filtro.estado;
      const camaraOk = filtro.camara === 'TODAS' || p.clearingHouseCode === filtro.camara;
      const flujoOk = filtro.flujo === 'TODOS' || p.flowType === filtro.flujo;
      return textOk && estadoOk && camaraOk && flujoOk;
    });
  }

  private date(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }
}
