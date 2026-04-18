import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaConfigProfileListItem } from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

@Component({
  selector: 'app-nacha-config-profiles-page',
  templateUrl: './nacha-config-profiles-page.component.html',
  styleUrls: ['./nacha-config-profiles-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigProfilesPageComponent implements OnInit {
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  cargando = false;
  guardando = false;
  errorCarga = false;
  perfiles: NachaConfigProfileListItem[] = [];
  visibles: NachaConfigProfileListItem[] = [];

  readonly filtrosForm = this.fb.group({
    texto: [''],
    estado: ['TODOS'],
    camara: ['TODAS'],
    flujo: ['TODOS'],
    direccion: ['TODAS']
  });

  readonly crearForm = this.fb.group({
    profileCode: [''],
    nombreEs: [''],
    descripcion: [''],
    camaraCode: ['ACH'],
    flujoCode: ['ORIGINAL'],
    direccionCode: ['SALIDA'],
    servicioCode: ['PPD'],
    effectiveFrom: [this.today()]
  });

  readonly columnas: ColDef<NachaConfigProfileListItem>[] = [
    { field: 'profileCode', headerName: 'Código', minWidth: 120 },
    { field: 'nombreEs', headerName: 'Nombre', minWidth: 220 },
    { field: 'estado', headerName: 'Estado', minWidth: 130 },
    { field: 'camara', headerName: 'Cámara', minWidth: 120 },
    { field: 'flujo', headerName: 'Flujo', minWidth: 140 },
    { field: 'direccion', headerName: 'Dirección', minWidth: 120 },
    { headerName: 'Vigencia', minWidth: 220, valueGetter: (p) => `${this.date(p.data?.effectiveFrom)} a ${p.data?.effectiveTo ? this.date(p.data.effectiveTo) : 'abierta'}` },
    { headerName: 'Versión', minWidth: 120, valueGetter: (p) => `v${p.data?.versionMajor}.${p.data?.versionMinor}` },
    {
      headerName: 'Acciones', minWidth: 180, sortable: false, filter: false,
      cellRenderer: () => '<button class="btn btn-outline btn-grid" data-action="ver">Ver detalle</button>',
      onCellClicked: (params) => {
        const action = (params.event?.target as HTMLElement | null)?.getAttribute('data-action');
        if (action === 'ver' && params.data) {
          this.router.navigate(['/nacha-config-admin/perfiles', params.data.id]);
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
    this.query.perfiles().pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (rows) => {
        this.perfiles = rows;
        this.aplicarFiltros();
      },
      error: () => {
        this.errorCarga = true;
        this.notifications.error('No fue posible cargar los perfiles NACHA.');
      }
    });
  }

  aplicarFiltros(): void {
    const filtro = this.filtrosForm.getRawValue();
    const texto = (filtro.texto ?? '').trim().toLowerCase();
    this.visibles = this.perfiles.filter((p) => {
      const textOk = !texto || `${p.profileCode} ${p.nombreEs}`.toLowerCase().includes(texto);
      const estadoOk = filtro.estado === 'TODOS' || p.estado === filtro.estado;
      const camaraOk = filtro.camara === 'TODAS' || p.camara === filtro.camara;
      const flujoOk = filtro.flujo === 'TODOS' || p.flujo === filtro.flujo;
      const dirOk = filtro.direccion === 'TODAS' || p.direccion === filtro.direccion;
      return textOk && estadoOk && camaraOk && flujoOk && dirOk;
    });
  }

  crearBorrador(): void {
    if (this.guardando) {
      return;
    }

    this.guardando = true;
    this.command.crearBorrador(this.crearForm.getRawValue() as unknown as Record<string, unknown>)
      .pipe(finalize(() => {
        this.guardando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (perfil) => {
          this.notifications.success('Borrador creado correctamente.');
          this.router.navigate(['/nacha-config-admin/perfiles', perfil.id]);
        },
        error: (error) => {
          this.notifications.error(error?.message ?? 'No fue posible crear el borrador.');
        }
      });
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private date(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '—';
  }
}
