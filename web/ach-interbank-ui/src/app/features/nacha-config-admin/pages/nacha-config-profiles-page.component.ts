import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { catchError, forkJoin, finalize, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { OpcionSelectorBuscable } from '../../../shared/components/ui/ui-selector-buscable.component';
import {
  NachaConfigFilterCatalogs,
  NachaConfigProfileReadModel,
  NachaConfigProfilesDashboardReadModel,
  NachaConfigValidationResult
} from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

@Component({
  selector: 'app-nacha-config-profiles-page',
  templateUrl: './nacha-config-profiles-page.component.html',
  styleUrls: ['./nacha-config-profiles-page.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Default
})
export class NachaConfigProfilesPageComponent implements OnInit {
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  readonly puedeGestionar = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);

  cargando = false;
  creando = false;
  validando = false;
  errorCarga = false;
  dashboard: NachaConfigProfilesDashboardReadModel | null = null;
  perfiles: NachaConfigProfileReadModel[] = [];
  visibles: NachaConfigProfileReadModel[] = [];
  catalogos: NachaConfigFilterCatalogs = { estados: [], camaras: [], flujos: [], direcciones: [], servicios: [] };
  opcionesEstado: OpcionSelectorBuscable[] = [{ valor: 'TODOS', etiqueta: 'Todos' }];
  opcionesCamara: OpcionSelectorBuscable[] = [{ valor: 'TODAS', etiqueta: 'Todas' }];
  opcionesFlujo: OpcionSelectorBuscable[] = [{ valor: 'TODOS', etiqueta: 'Todos' }];
  opcionesDireccion: OpcionSelectorBuscable[] = [];
  opcionesServicio: OpcionSelectorBuscable[] = [];
  validationResult: NachaConfigValidationResult | null = null;
  validationProfile: NachaConfigProfileReadModel | null = null;

  readonly crearForm = this.fb.group({
    profileCode: ['', [Validators.required, Validators.minLength(6)]],
    nombreEs: ['', [Validators.required, Validators.minLength(3)]],
    descripcion: [''],
    camaraCode: ['', Validators.required],
    flujoCode: ['', Validators.required],
    direccionCode: ['', Validators.required],
    servicioCode: [''],
    effectiveFrom: [this.todayIsoDate(), Validators.required]
  });

  readonly filtrosForm = this.fb.group({
    texto: [''],
    estado: ['TODOS'],
    camara: ['TODAS'],
    flujo: ['TODOS']
  });

  readonly columnas: ColDef<NachaConfigProfileReadModel>[] = [
    { field: 'profileCode', headerName: 'Código', minWidth: 160 },
    { field: 'profileName', headerName: 'Nombre', minWidth: 240 },
    { field: 'clearingHouseCode', headerName: 'Cámara', minWidth: 190, valueGetter: (p) => this.camaraLabel(p.data?.clearingHouseCode) },
    { field: 'status', headerName: 'Estado', minWidth: 130 },
    { field: 'version', headerName: 'Versión', minWidth: 110 },
    { field: 'layoutVariantCount', headerName: 'Variantes', minWidth: 110 },
    { field: 'fieldCount', headerName: 'Campos', minWidth: 100 },
    { headerName: 'Registros', minWidth: 140, valueGetter: (p) => (p.data?.recordTypes ?? []).join(', ') },
    {
      headerName: 'Vigencia',
      minWidth: 220,
      valueGetter: (p) => `${this.date(p.data?.effectiveFrom)} a ${p.data?.effectiveTo ? this.date(p.data.effectiveTo) : 'abierta'}`
    },
    {
      headerName: 'Acciones',
      minWidth: 220,
      sortable: false,
      filter: false,
      cellRenderer: (params: { data?: NachaConfigProfileReadModel | null }) => this.renderAccionesPerfil(params.data),
      onCellClicked: (params) => this.onAccionPerfil(params)
    }
  ];

  ngOnInit(): void {
    this.cargar();
    this.filtrosForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.aplicarFiltros();
    });
  }

  cargar(): void {
    this.cargando = true;
    this.errorCarga = false;
    this.dashboard = null;
    this.perfiles = [];
    this.visibles = [];

    forkJoin({
      catalogos: this.query.catalogosFiltro().pipe(catchError(() => of(null))),
      dashboard: this.query.dashboardReadOnly().pipe(catchError(() => of(null))),
      perfiles: this.query.perfilesReadOnly().pipe(catchError(() => of(null)))
    })
      .pipe(finalize(() => {
        this.cargando = false;
      }))
      .subscribe({
        next: ({ catalogos, dashboard, perfiles }) => {
          this.errorCarga = !catalogos || !dashboard || !perfiles;
          if (catalogos) {
            this.catalogos = catalogos;
            this.actualizarOpciones();
          }
          this.dashboard = dashboard;
          this.perfiles = perfiles ?? [];
          this.aplicarFiltros();
          if (this.errorCarga) {
            this.notifications.warning('Parte de la informacion NACHA Config no pudo cargarse. Las acciones disponibles siguen operativas.');
          }
        },
        error: () => {
          this.errorCarga = true;
          this.notifications.error('No fue posible cargar la informacion NACHA Config.');
        }
      });
  }

  crearBorrador(): void {
    if (!this.puedeGestionar || this.creando) {
      return;
    }

    if (this.crearForm.invalid) {
      this.crearForm.markAllAsTouched();
      return;
    }

    const form = this.crearForm.getRawValue();
    const payload = {
      profileCode: (form.profileCode ?? '').trim(),
      nombreEs: (form.nombreEs ?? '').trim(),
      descripcion: (form.descripcion ?? '').trim() || null,
      camaraCode: form.camaraCode ?? '',
      flujoCode: form.flujoCode ?? '',
      direccionCode: form.direccionCode ?? '',
      servicioCode: (form.servicioCode ?? '').trim() || null,
      effectiveFrom: form.effectiveFrom
    };

    this.creando = true;
    this.command
      .crearBorrador(payload)
      .pipe(finalize(() => {
        this.creando = false;
      }))
      .subscribe({
        next: (perfil) => {
          this.notifications.success(`Perfil borrador ${perfil.profileCode} creado correctamente.`);
          this.router.navigate(['/nacha-config-admin/perfiles', perfil.id]);
        },
        error: (error) => {
          this.notifications.error(this.errorMessage(error, 'No fue posible crear el borrador.'));
        }
      });
  }

  validarPrimerPerfil(): void {
    const perfil = this.visibles[0];
    if (!perfil) {
      return;
    }

    this.validarPerfil(perfil);
  }

  validarPerfil(perfil: NachaConfigProfileReadModel): void {
    if (!this.puedeGestionar || this.validando) {
      return;
    }

    this.validando = true;
    this.command
      .validar(perfil.profileId)
      .pipe(finalize(() => {
        this.validando = false;
      }))
      .subscribe({
        next: (result) => {
          this.validationResult = result;
          this.validationProfile = perfil;
          if (result.isValid) {
            this.notifications.success(`Validación completa para ${perfil.profileCode}.`);
          } else {
            this.notifications.warning(`Validación con observaciones para ${perfil.profileCode}.`);
          }
          this.cargar();
        },
        error: (error) => {
          this.notifications.error(this.errorMessage(error, 'No fue posible validar el perfil.'));
        }
      });
  }

  irADetalle(perfil: NachaConfigProfileReadModel): void {
    this.router.navigate(['/nacha-config-admin/perfiles', perfil.profileId]);
  }

  renderAccionesPerfil(perfil?: NachaConfigProfileReadModel | null): string {
    if (!perfil) {
      return '';
    }

    const acciones = [
      '<button type="button" class="btn btn-outline btn-grid" data-action="ver">Ver detalle</button>'
    ];

    if (this.puedeGestionar) {
      acciones.push('<button type="button" class="btn btn-outline btn-grid" data-action="validar">Validar</button>');
    }

    return `<div class="acciones-grid">${acciones.join('')}</div>`;
  }

  onAccionPerfil(params: { data?: NachaConfigProfileReadModel | null; event?: Event | null }): void {
    const target = params.event?.target as HTMLElement | null;
    const boton = target?.closest('[data-action]') as HTMLElement | null;
    const accion = boton?.getAttribute('data-action');
    const perfil = params.data ?? null;

    if (!accion || !perfil) {
      return;
    }

    if (accion === 'ver') {
      this.irADetalle(perfil);
      return;
    }

    if (accion === 'validar') {
      this.validarPerfil(perfil);
    }
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

  private errorMessage(error: unknown, fallback: string): string {
    if (error && typeof error === 'object' && 'message' in error && typeof (error as { message?: unknown }).message === 'string') {
      return (error as { message: string }).message;
    }
    return fallback;
  }

  private date(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }

  private actualizarOpciones(): void {
    this.opcionesEstado = [
      { valor: 'TODOS', etiqueta: 'Todos' },
      ...this.catalogos.estados.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }))
    ];
    this.opcionesCamara = [
      { valor: 'TODAS', etiqueta: 'Todas' },
      ...this.catalogos.camaras.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }))
    ];
    this.opcionesFlujo = [
      { valor: 'TODOS', etiqueta: 'Todos' },
      ...this.catalogos.flujos.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }))
    ];
    this.opcionesDireccion = this.catalogos.direcciones.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }));
    this.opcionesServicio = this.catalogos.servicios.map((x) => ({ valor: x.code, etiqueta: `${x.code} - ${x.labelEs}` }));

    this.seleccionarPrimeraOpcionDisponible('camaraCode', this.opcionesCamara.slice(1));
    this.seleccionarPrimeraOpcionDisponible('flujoCode', this.opcionesFlujo.slice(1));
    this.seleccionarPrimeraOpcionDisponible('direccionCode', this.opcionesDireccion);
  }

  camaraLabel(code?: string | null): string {
    if (!code) {
      return '-';
    }

    const catalogo = this.catalogos.camaras.find((item) => item.code === code);
    return catalogo ? `${catalogo.code} - ${catalogo.labelEs}` : code;
  }

  private seleccionarPrimeraOpcionDisponible(
    controlName: 'camaraCode' | 'flujoCode' | 'direccionCode',
    opciones: OpcionSelectorBuscable[]
  ): void {
    const control = this.crearForm.controls[controlName];
    if (!opciones.some((opcion) => opcion.valor === control.value)) {
      control.setValue(opciones[0] ? String(opciones[0].valor) : '');
    }
  }

  private todayIsoDate(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = `${now.getMonth() + 1}`.padStart(2, '0');
    const day = `${now.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
