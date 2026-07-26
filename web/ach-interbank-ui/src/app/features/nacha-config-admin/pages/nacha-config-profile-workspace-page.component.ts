import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  NachaConfigLayoutVariant,
  NachaConfigProfileDetail,
  NachaConfigProfileRecord,
  NachaConfigValidationResult
} from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

@Component({
  selector: 'app-nacha-config-profile-workspace-page',
  templateUrl: './nacha-config-profile-workspace-page.component.html',
  styleUrls: ['./nacha-config-profile-workspace-page.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Default
})
export class NachaConfigProfileWorkspacePageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  readonly puedeGestionar = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);

  perfilId = 0;
  perfil: NachaConfigProfileDetail | null = null;
  cargando = false;
  errorCarga = false;
  validacion: NachaConfigValidationResult | null = null;
  guardando = false;
  clonando = false;
  validando = false;
  publicando = false;
  inactivando = false;
  archivando = false;
  modalAbierto = false;
  modalAccion: 'publicar' | 'inactivar' | 'archivar' | null = null;
  modalTitulo = '';
  modalMensaje = '';

  readonly editarForm = this.fb.group({
    nombreEs: ['', [Validators.required, Validators.minLength(3)]],
    descripcion: [''],
    contextPriority: [100, [Validators.required, Validators.min(1)]],
    effectiveFrom: ['', Validators.required],
    effectiveTo: [''],
    expectedRowVersion: ['']
  });

  readonly cloneForm = this.fb.group({
    nuevoProfileCode: ['', [Validators.required, Validators.minLength(6)]],
    nuevoNombreEs: ['', [Validators.required, Validators.minLength(3)]],
    effectiveFrom: ['', Validators.required],
    expectedRowVersion: ['']
  });

  readonly columnasRecords: ColDef<NachaConfigProfileRecord>[] = [
    { field: 'recordCode', headerName: 'Registro', minWidth: 100 },
    { field: 'sequence', headerName: 'Secuencia', minWidth: 110 },
    { field: 'isEnabled', headerName: 'Habilitado', minWidth: 120 },
    { field: 'minOccurs', headerName: 'Mínimo', minWidth: 110 },
    { field: 'maxOccurs', headerName: 'Máximo', minWidth: 110 },
    { field: 'sourceStrategy', headerName: 'Estrategia fuente', minWidth: 200 }
  ];

  readonly columnasVariantes: ColDef<NachaConfigLayoutVariant>[] = [
    { field: 'recordCode', headerName: 'Registro', minWidth: 90 },
    { field: 'variantCode', headerName: 'Variante', minWidth: 180 },
    { field: 'nombreEs', headerName: 'Nombre', minWidth: 220 },
    { field: 'priority', headerName: 'Prioridad', minWidth: 100 },
    { field: 'isDefaultForRecord', headerName: 'Predeterminado', minWidth: 120 },
    { field: 'totalLength', headerName: 'Longitud', minWidth: 100 },
    { headerName: 'Campos', minWidth: 100, valueGetter: (p) => p.data?.fields.length ?? 0 }
  ];

  get estadoNormalizado(): string {
    return this.perfil?.estado?.toUpperCase?.() ?? '';
  }

  get esBorrador(): boolean {
    return this.estadoNormalizado === 'BORRADOR';
  }

  get puedeEditarBorrador(): boolean {
    return this.puedeGestionar && this.esBorrador;
  }

  get puedePublicar(): boolean {
    return this.puedeGestionar && this.esBorrador;
  }

  get puedeInactivar(): boolean {
    return this.puedeGestionar && this.estadoNormalizado === 'PUBLICADO';
  }

  get puedeArchivar(): boolean {
    return this.puedeGestionar && ['PUBLICADO', 'INACTIVO'].includes(this.estadoNormalizado);
  }

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const nextProfileId = Number(params.get('id'));
        if (!nextProfileId) {
          void this.router.navigate(['/nacha-config-admin/perfiles']);
          return;
        }

        this.perfilId = nextProfileId;
        this.perfil = null;
        this.cargar();
      });
  }

  cargar(): void {
    this.cargando = true;
    this.errorCarga = false;

    this.query.detalle(this.perfilId)
      .pipe(finalize(() => {
        this.cargando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (perfil) => {
          this.perfil = perfil;
          this.sincronizarFormularios();
          this.validacion = null;
          this.cdr.markForCheck();
        },
        error: () => {
          this.errorCarga = true;
          this.notifications.error('No fue posible cargar el perfil NACHA administrativo.');
          this.cdr.markForCheck();
        }
      });
  }

  guardarBorrador(): void {
    if (!this.puedeEditarBorrador || !this.perfil || this.guardando) {
      return;
    }

    if (this.editarForm.invalid) {
      this.editarForm.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    const form = this.editarForm.getRawValue();
    this.guardando = true;

    this.command
      .editarBorrador(this.perfil.id, {
        nombreEs: (form.nombreEs ?? '').trim(),
        descripcion: (form.descripcion ?? '').trim() || null,
        contextPriority: Number(form.contextPriority ?? 100),
        effectiveFrom: form.effectiveFrom,
        effectiveTo: (form.effectiveTo ?? '').trim() || null,
        expectedRowVersion: form.expectedRowVersion ?? ''
      })
      .pipe(finalize(() => {
        this.guardando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Borrador actualizado correctamente.');
          this.cargar();
        },
        error: (error) => {
          this.notifications.error(this.errorMessage(error, 'No fue posible guardar el borrador.'));
        }
      });
  }

  cancelarEdicion(): void {
    this.sincronizarFormularios();
  }

  validarPerfil(): void {
    if (!this.perfil || this.validando) {
      return;
    }

    this.validando = true;
    this.command
      .validar(this.perfil.id)
      .pipe(finalize(() => {
        this.validando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (result) => {
          this.validacion = result;
          if (result.isValid) {
            this.notifications.success(`Perfil ${this.perfil?.profileCode} validado correctamente.`);
          } else {
          this.notifications.warning(`Perfil ${this.perfil?.profileCode} tiene observaciones de validación.`);
          }
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.notifications.error(this.errorMessage(error, 'No fue posible validar el perfil.'));
        }
      });
  }

  publicarPerfil(): void {
    if (!this.puedePublicar || !this.perfil) {
      return;
    }

    this.validarPerfilYPublicar();
  }

  inactivarPerfil(): void {
    if (!this.puedeInactivar || !this.perfil) {
      return;
    }

    this.abrirModal('inactivar');
  }

  archivarPerfil(): void {
    if (!this.puedeArchivar || !this.perfil) {
      return;
    }

    this.abrirModal('archivar');
  }

  clonarPerfil(): void {
    if (!this.puedeGestionar || !this.perfil || this.clonando) {
      return;
    }

    if (this.cloneForm.invalid) {
      this.cloneForm.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    const form = this.cloneForm.getRawValue();
    this.clonando = true;

    this.command
      .clonar(this.perfil.id, {
        nuevoProfileCode: (form.nuevoProfileCode ?? '').trim(),
        nuevoNombreEs: (form.nuevoNombreEs ?? '').trim(),
        effectiveFrom: form.effectiveFrom,
        expectedRowVersion: form.expectedRowVersion ?? ''
      })
      .pipe(finalize(() => {
        this.clonando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (created) => {
          this.notifications.success(`Perfil clonado como ${created.profileCode}.`);
          this.router.navigate(['/nacha-config-admin/perfiles', created.id]);
        },
        error: (error) => {
          this.notifications.error(this.errorMessage(error, 'No fue posible clonar el perfil.'));
        }
      });
  }

  cancelarClonacion(): void {
    this.sincronizarFormularios();
  }

  volver(): void {
    this.router.navigate(['/nacha-config-admin/perfiles']);
  }

  irARecords(): void {
    this.router.navigate(['/nacha-config-admin/records'], { queryParams: { profileId: this.perfilId } });
  }

  irAVariantsFields(): void {
    this.router.navigate(['/nacha-config-admin/variants-fields'], { queryParams: { profileId: this.perfilId } });
  }

  cerrarModal(): void {
    this.modalAbierto = false;
    this.modalAccion = null;
    this.modalTitulo = '';
    this.modalMensaje = '';
  }

  confirmarModal(): void {
    if (!this.modalAccion || !this.perfil || this.publicando || this.inactivando || this.archivando) {
      return;
    }

    const expectedRowVersion = this.editarForm.controls.expectedRowVersion.value || this.cloneForm.controls.expectedRowVersion.value || this.perfil.rowVersion;
    const accion = this.modalAccion;
    this.cerrarModal();

    if (accion === 'publicar') {
      this.publicando = true;
      this.command
        .publicar(this.perfil.id, expectedRowVersion ?? '')
        .pipe(finalize(() => {
          this.publicando = false;
          this.cdr.markForCheck();
        }))
        .subscribe({
          next: (result) => {
            if (result.publicado) {
              this.notifications.success(result.mensaje || 'Perfil publicado correctamente.');
            } else {
              this.notifications.warning(result.mensaje || 'La publicacion quedo bloqueada por validacion.');
            }
            this.cargar();
          },
          error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible publicar el perfil.'))
        });
      return;
    }

    if (accion === 'inactivar') {
      this.inactivando = true;
      this.command
        .inactivar(this.perfil.id, expectedRowVersion ?? '')
        .pipe(finalize(() => {
          this.inactivando = false;
          this.cdr.markForCheck();
        }))
        .subscribe({
          next: () => {
            this.notifications.success('Perfil inactivado correctamente.');
            this.cargar();
          },
          error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible inactivar el perfil.'))
        });
      return;
    }

    if (accion === 'archivar') {
      this.archivando = true;
      this.command
        .archivar(this.perfil.id, expectedRowVersion ?? '')
        .pipe(finalize(() => {
          this.archivando = false;
          this.cdr.markForCheck();
        }))
        .subscribe({
          next: () => {
            this.notifications.success('Perfil archivado correctamente.');
            this.cargar();
          },
          error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible archivar el perfil.'))
        });
    }
  }

  abrirModal(accion: 'publicar' | 'inactivar' | 'archivar'): void {
    if (!this.perfil) {
      return;
    }

    this.modalAccion = accion;
    if (accion === 'publicar') {
      this.modalTitulo = 'Publicar perfil NACHA-M';
      this.modalMensaje = 'Esta accion exige validacion previa. Si el backend detecta inconsistencias, la publicacion quedara bloqueada.';
    } else if (accion === 'inactivar') {
      this.modalTitulo = 'Inactivar perfil NACHA-M';
      this.modalMensaje = 'El perfil dejara de quedar activo para resolucion oficial.';
    } else {
      this.modalTitulo = 'Archivar perfil NACHA-M';
      this.modalMensaje = 'El perfil quedara archivado y solo se podra clonar como borrador.';
    }

    this.modalAbierto = true;
  }

  private validarPerfilYPublicar(): void {
    if (!this.perfil || this.publicando) {
      return;
    }

    this.validando = true;
    this.command
      .validar(this.perfil.id)
      .pipe(finalize(() => {
        this.validando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (result) => {
          this.validacion = result;
          if (!result.isValid) {
            this.notifications.warning('Publicacion bloqueada: primero corrige las observaciones de validacion.');
            return;
          }

          this.abrirModal('publicar');
          this.cdr.markForCheck();
        },
        error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible validar el perfil antes de publicar.'))
      });
  }

  private sincronizarFormularios(): void {
    if (!this.perfil) {
      return;
    }

    this.editarForm.reset(
      {
        nombreEs: this.perfil.nombreEs,
        descripcion: this.perfil.descripcion ?? '',
        contextPriority: this.perfil.contextPriority,
        effectiveFrom: this.dateInputValue(this.perfil.effectiveFrom),
        effectiveTo: this.dateInputValue(this.perfil.effectiveTo),
        expectedRowVersion: this.perfil.rowVersion
      },
      { emitEvent: false }
    );

    if (this.puedeEditarBorrador) {
      this.editarForm.enable({ emitEvent: false });
    } else {
      this.editarForm.disable({ emitEvent: false });
    }

    this.cloneForm.reset(
      {
        nuevoProfileCode: `${this.perfil.profileCode}-CLONE`,
        nuevoNombreEs: `${this.perfil.nombreEs} (copia)`,
        effectiveFrom: this.dateInputValue(this.perfil.effectiveFrom),
        expectedRowVersion: this.perfil.rowVersion
      },
      { emitEvent: false }
    );

    if (this.puedeGestionar) {
      this.cloneForm.enable({ emitEvent: false });
    } else {
      this.cloneForm.disable({ emitEvent: false });
    }
  }

  private dateInputValue(value?: string | null): string {
    return value ? new Date(value).toISOString().slice(0, 10) : '';
  }

  date(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (error && typeof error === 'object' && 'errorCode' in error) {
      const errorCode = String((error as { errorCode?: unknown }).errorCode ?? '');
      if (errorCode.includes('409') || errorCode === 'CONCURRENCY_CONFLICT') {
        return 'El perfil cambió mientras lo editabas. Recarga la página e intenta nuevamente.';
      }
      if (errorCode.includes('400') || errorCode === 'VALIDATION_ERROR') {
        return 'La operación contiene datos no válidos. Revisa el formulario e intenta nuevamente.';
      }
    }
    return fallback;
  }

}
