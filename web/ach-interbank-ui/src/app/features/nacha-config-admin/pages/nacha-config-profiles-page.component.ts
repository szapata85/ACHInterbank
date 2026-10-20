import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
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
} from '../../../core/models/nacha-config.model';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';

type CatalogosEstado = 'pendientes' | 'cargando' | 'reintentando' | 'disponibles' | 'recuperados' | 'vacios' | 'fallidos';

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
  dashboardError = false;
  perfilesError = false;
  catalogosEstado: CatalogosEstado = 'pendientes';
  catalogosErrorMessage = '';
  catalogosReintentos = 0;
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
    { field: 'status', headerName: 'Estado', minWidth: 150, valueGetter: (p) => this.estadoLabel(p.data?.status) },
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
    this.bloquearCreacionPorCatalogos();
    this.cargar();
    this.crearForm.controls.camaraCode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.cargarCatalogos());
    this.crearForm.controls.flujoCode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.cargarCatalogos());
    this.filtrosForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.aplicarFiltros();
    });
  }

  cargar(): void {
    this.cargando = true;
    this.errorCarga = false;
    this.dashboardError = false;
    this.perfilesError = false;
    this.dashboard = null;
    this.perfiles = [];
    this.visibles = [];
    this.cargarCatalogos();

    forkJoin({
      dashboard: this.query.dashboardReadOnly().pipe(catchError(() => of(null))),
      perfiles: this.query.perfilesReadOnly().pipe(catchError(() => of(null)))
    })
      .pipe(finalize(() => {
        this.cargando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: ({ dashboard, perfiles }) => {
          this.dashboardError = !dashboard;
          this.perfilesError = !perfiles;
          this.errorCarga = this.perfilesError;
          this.dashboard = dashboard;
          this.perfiles = perfiles ?? [];
          this.aplicarFiltros();
          if (this.dashboardError) {
            this.notifications.warning('El resumen de configuración NACHA-M no está disponible. El listado puede seguir utilizándose.');
          }
          if (this.perfilesError) {
            this.notifications.warning('El listado de perfiles de configuración NACHA-M no está disponible.');
          }
          this.cdr.markForCheck();
        },
        error: () => {
          this.errorCarga = true;
          this.notifications.error('No fue posible cargar la información de configuración NACHA-M.');
          this.cdr.markForCheck();
        }
      });
  }

  cargarCatalogos(esReintentoManual = false): void {
    if (this.catalogosCargando) {
      return;
    }

    const seRecuperaDeFallo = esReintentoManual || this.catalogosEstado === 'fallidos';
    const camaraCode = this.crearForm.controls.camaraCode.value?.trim() || null;
    const flujoCode = this.crearForm.controls.flujoCode.value?.trim() || null;
    this.catalogosEstado = 'cargando';
    this.catalogosErrorMessage = '';
    this.catalogosReintentos = 0;
    this.bloquearCreacionPorCatalogos(!(camaraCode || flujoCode || esReintentoManual));

    this.query
      .catalogosOperativos(camaraCode, flujoCode, (event) => {
        this.catalogosEstado = 'reintentando';
        this.catalogosReintentos = event.retryNumber;
        this.cdr.detectChanges();
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.cdr.markForCheck())
      )
      .subscribe({
        next: (catalogos) => {
          this.catalogos = catalogos;
          this.actualizarOpciones();

          if (!this.catalogosUtilizables(catalogos)) {
            this.catalogosEstado = 'vacios';
            this.catalogosErrorMessage =
              'No existen cámaras, flujos y direcciones activas para crear perfiles. Puedes reintentar la carga.';
            this.bloquearCreacionPorCatalogos(false);
            return;
          }

          this.catalogosEstado =
            seRecuperaDeFallo || this.catalogosReintentos > 0 ? 'recuperados' : 'disponibles';
          this.crearForm.enable({ emitEvent: false });
          this.actualizarDisponibilidadContextual();
          if (this.catalogosEstado === 'recuperados') {
            this.notifications.success('Los catálogos de configuración NACHA-M se recuperaron correctamente.');
          }
        },
        error: (error: unknown) => {
          this.catalogosEstado = 'fallidos';
          this.catalogosErrorMessage = this.mensajeErrorCatalogos(error);
          this.bloquearCreacionPorCatalogos(false);
          console.warn('[NACHA Config] Catálogos no disponibles después de los reintentos.', {
            status: error instanceof HttpErrorResponse ? error.status : 0,
            retryAfter: error instanceof HttpErrorResponse ? error.headers.get('Retry-After') : null
          });
        }
      });
  }

  reintentarCatalogos(): void {
    this.cargarCatalogos(true);
  }

  get catalogosDisponibles(): boolean {
    return this.catalogosEstado === 'disponibles' || this.catalogosEstado === 'recuperados';
  }

  get catalogosCargando(): boolean {
    return this.catalogosEstado === 'cargando' || this.catalogosEstado === 'reintentando';
  }

  crearBorrador(): void {
    if (!this.puedeGestionar || this.creando || !this.catalogosDisponibles || this.crearForm.disabled) {
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
        this.cdr.markForCheck();
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

  validarPerfil(perfil: NachaConfigProfileReadModel): void {
    if (!this.puedeGestionar || this.validando) {
      return;
    }

    this.validando = true;
    this.command
      .validar(perfil.profileId)
      .pipe(finalize(() => {
        this.validando = false;
        this.cdr.markForCheck();
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
    return nachaConfigUserMessage(error, fallback);
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

    this.limpiarSeleccionInvalida('camaraCode', this.opcionesCamara.slice(1));
    this.limpiarSeleccionInvalida('flujoCode', this.opcionesFlujo.slice(1));
    this.limpiarSeleccionInvalida('direccionCode', this.opcionesDireccion);
    if (this.crearForm.controls.flujoCode.value && !this.crearForm.controls.direccionCode.value && this.opcionesDireccion.length === 1) {
      this.crearForm.controls.direccionCode.setValue(String(this.opcionesDireccion[0].valor), { emitEvent: false });
    }
    if (!this.opcionesServicio.some((opcion) => opcion.valor === this.crearForm.controls.servicioCode.value)) {
      this.crearForm.controls.servicioCode.setValue('', { emitEvent: false });
    }
  }

  private actualizarDisponibilidadContextual(): void {
    if (this.crearForm.controls.flujoCode.value) {
      this.crearForm.controls.direccionCode.enable({ emitEvent: false });
    } else {
      this.crearForm.controls.direccionCode.setValue('', { emitEvent: false });
      this.crearForm.controls.direccionCode.disable({ emitEvent: false });
    }

    if (this.crearForm.controls.camaraCode.value) {
      this.crearForm.controls.servicioCode.enable({ emitEvent: false });
    } else {
      this.crearForm.controls.servicioCode.setValue('', { emitEvent: false });
      this.crearForm.controls.servicioCode.disable({ emitEvent: false });
    }
  }

  private bloquearCreacionPorCatalogos(limpiar = true): void {
    this.crearForm.disable({ emitEvent: false });
    if (!limpiar) {
      return;
    }

    this.catalogos = { estados: [], camaras: [], flujos: [], direcciones: [], servicios: [] };
    this.opcionesEstado = [{ valor: 'TODOS', etiqueta: 'Todos' }];
    this.opcionesCamara = [{ valor: 'TODAS', etiqueta: 'Todas' }];
    this.opcionesFlujo = [{ valor: 'TODOS', etiqueta: 'Todos' }];
    this.opcionesDireccion = [];
    this.opcionesServicio = [];
    this.crearForm.patchValue(
      { camaraCode: '', flujoCode: '', direccionCode: '', servicioCode: '' },
      { emitEvent: false }
    );
    this.filtrosForm.patchValue({ estado: 'TODOS', camara: 'TODAS', flujo: 'TODOS' }, { emitEvent: false });
  }

  private catalogosUtilizables(catalogos: NachaConfigFilterCatalogs): boolean {
    return catalogos.camaras.length > 0 && catalogos.flujos.length > 0 && catalogos.direcciones.length > 0;
  }

  private mensajeErrorCatalogos(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 429) {
        return 'El sistema está procesando varias solicitudes. Espera el tiempo recomendado o reintenta la carga de catálogos.';
      }
      if (error.status === 0) {
        return 'No fue posible conectar con el servidor para cargar los catálogos.';
      }
      if (error.status === 401 || error.status === 403) {
        return 'Tu sesión no permite consultar los catálogos de configuración NACHA-M.';
      }
      if (error.status >= 500) {
        return 'El servidor no pudo cargar los catálogos de configuración NACHA-M.';
      }
    }

    return 'Los catálogos de configuración NACHA-M no están disponibles. Reintenta la carga.';
  }


  estadoLabel(code?: string | null): string {
    if (!code) {
      return '-';
    }

    const catalogo = this.catalogos.estados.find((item) => item.code === code);
    return catalogo?.labelEs || code;
  }

  camaraLabel(code?: string | null): string {
    if (!code) {
      return '-';
    }

    const catalogo = this.catalogos.camaras.find((item) => item.code === code);
    return catalogo ? `${catalogo.code} - ${catalogo.labelEs}` : code;
  }

  private limpiarSeleccionInvalida(
    controlName: 'camaraCode' | 'flujoCode' | 'direccionCode',
    opciones: OpcionSelectorBuscable[]
  ): void {
    const control = this.crearForm.controls[controlName];
    if (control.value && !opciones.some((opcion) => opcion.valor === control.value)) {
      control.setValue('', { emitEvent: false });
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
