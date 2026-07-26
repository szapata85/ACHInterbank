import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  NachaConfigApiError,
  NachaConfigProfileDetail,
  NachaConfigProfileReadModel,
  NachaConfigProfileRecord,
  NachaConfigValidationIssue
} from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

interface EditableRecordRow extends NachaConfigProfileRecord {
  position: number;
}

@Component({
  selector: 'app-nacha-config-records-page',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './nacha-config-records-page.component.html',
  styleUrls: ['./nacha-config-records-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigRecordsPageComponent implements OnInit {
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly puedeGestionar = this.auth.hasPermission('CanManageAch');

  profiles: NachaConfigProfileReadModel[] = [];
  selectedProfileId: number | null = null;
  selectedProfile: NachaConfigProfileDetail | null = null;
  records: EditableRecordRow[] = [];

  loadingProfiles = false;
  loadingDetail = false;
  saving = false;

  profilesError = '';
  detailError = '';
  saveError = '';
  sequenceError = '';
  saveIssues: NachaConfigValidationIssue[] = [];
  sequencesDirty = false;
  private originalSequences = new Map<number, number>();

  ngOnInit(): void {
    const requestedProfileId = Number(this.route.snapshot.queryParamMap.get('profileId'));
    this.selectedProfileId = Number.isFinite(requestedProfileId) && requestedProfileId > 0 ? requestedProfileId : null;
    this.loadProfiles();
  }

  get estadoSeleccionado(): string {
    return this.selectedProfile?.estado?.toUpperCase?.() ?? '';
  }

  get puedeEditarSecuencia(): boolean {
    return this.puedeGestionar && this.estadoSeleccionado === 'BORRADOR';
  }

  loadProfiles(): void {
    this.loadingProfiles = true;
    this.profilesError = '';

    this.query.perfilesReadOnly()
      .pipe(finalize(() => {
        this.loadingProfiles = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (profiles) => {
          this.profiles = profiles ?? [];
          if (this.profiles.length === 0) {
            this.selectedProfileId = null;
            this.selectedProfile = null;
            this.records = [];
            this.cdr.markForCheck();
            return;
          }

          const exists = this.selectedProfileId !== null && this.profiles.some((profile) => profile.profileId === this.selectedProfileId);
          const nextProfileId = exists ? this.selectedProfileId! : this.profiles[0].profileId;
          this.selectedProfileId = nextProfileId;
          this.loadSelectedProfile(nextProfileId);
          this.cdr.markForCheck();
        },
        error: () => {
          this.profiles = [];
          this.selectedProfileId = null;
          this.selectedProfile = null;
          this.records = [];
          this.profilesError = 'No fue posible cargar los perfiles oficiales de configuración NACHA-M.';
          this.notifications.error(this.profilesError);
          this.cdr.markForCheck();
        }
      });
  }

  onProfileChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (!Number.isFinite(value) || value <= 0) {
      return;
    }

    this.selectedProfileId = value;
    this.loadSelectedProfile(value);
  }

  loadSelectedProfile(profileId: number): void {
    this.loadingDetail = true;
    this.detailError = '';
    this.saveError = '';
    this.saveIssues = [];

    this.query.detalle(profileId)
      .pipe(finalize(() => {
        this.loadingDetail = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (detail) => {
          this.selectedProfile = detail;
          this.selectedProfileId = detail.id;
          this.records = [...(detail.records ?? [])]
            .sort((a, b) => a.sequence - b.sequence || a.recordCode.localeCompare(b.recordCode))
            .map((record, index) => ({ ...record, position: index + 1 }));
          this.originalSequences = new Map(this.records.map((record) => [record.id, record.sequence]));
          this.sequencesDirty = false;
          this.sequenceError = '';
          this.syncContextUrl();
          this.cdr.markForCheck();
        },
        error: () => {
          this.selectedProfile = null;
          this.records = [];
          this.detailError = 'No fue posible cargar el detalle del perfil de configuración NACHA-M.';
          this.notifications.error(this.detailError);
          this.cdr.markForCheck();
        }
      });
  }

  guardarSecuencia(): void {
    if (!this.puedeEditarSecuencia || !this.selectedProfile || this.saving || !this.sequencesDirty) {
      return;
    }

    this.validateSequences();
    if (this.sequenceError) {
      return;
    }

    this.saveError = '';
    this.saveIssues = [];
    this.saving = true;

    const payload = {
      expectedRowVersion: this.selectedProfile.rowVersion,
      records: this.records.map((record) => ({
        profileRecordId: record.id,
        sequence: Number(record.sequence)
      }))
    };

    this.command.actualizarSecuencia(this.selectedProfile.id, payload)
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Secuencia de registros actualizada correctamente.');
          this.loadSelectedProfile(this.selectedProfile!.id);
        },
        error: (error) => {
          const apiError = error as Partial<NachaConfigApiError> | null;
          this.saveError = this.friendlySaveError(apiError);
          this.saveIssues = apiError?.issues ?? [];
          this.notifications.error(this.saveError);
          this.cdr.markForCheck();
        }
      });
  }

  onSequenceChange(record: EditableRecordRow, event: Event): void {
    const rawValue = (event.target as HTMLInputElement).value;
    if (rawValue === '') {
      return;
    }

    const value = Number(rawValue);
    record.sequence = Number.isFinite(value) ? Math.trunc(value) : record.sequence;
    this.sequencesDirty = this.records.some((item) => this.originalSequences.get(item.id) !== item.sequence);
    this.validateSequences();
    this.cdr.markForCheck();
  }

  cancelarCambios(): void {
    for (const record of this.records) {
      record.sequence = this.originalSequences.get(record.id) ?? record.sequence;
    }
    this.sequencesDirty = false;
    this.sequenceError = '';
    this.saveError = '';
    this.saveIssues = [];
    this.cdr.markForCheck();
  }

  irADetallePerfil(): void {
    if (!this.selectedProfileId) {
      return;
    }

    void this.router.navigate(['/nacha-config-admin/perfiles', this.selectedProfileId]);
  }

  irAVariantsFields(): void {
    void this.router.navigate(['/nacha-config-admin/variants-fields'], {
      queryParams: { profileId: this.selectedProfileId }
    });
  }

  trackByRecordId(_: number, record: EditableRecordRow): number {
    return record.id;
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }

  estadoBadgeClass(): string {
    return `estado estado-${this.estadoSeleccionado.toLowerCase() || 'desconocido'}`;
  }

  recargarDetalle(): void {
    if (this.selectedProfileId) {
      this.loadSelectedProfile(this.selectedProfileId);
    }
  }

  recordDescription(code: string): string {
    const names: Record<string, string> = {
      '1': 'Cabecera de archivo',
      '5': 'Cabecera de lote',
      '6': 'Detalle de transacción',
      '7': 'Información adicional',
      '8': 'Control de lote',
      '9': 'Control de archivo'
    };
    return names[code] ?? 'Registro configurado';
  }

  sourceStrategyLabel(value: string): string {
    return value === 'TABLE_DRIVEN' ? 'Configuración parametrizada' : value;
  }

  private validateSequences(): void {
    if (this.records.some((record) => !Number.isInteger(record.sequence) || record.sequence < 0)) {
      this.sequenceError = 'Cada secuencia debe ser un número entero mayor o igual a cero.';
      return;
    }
    if (new Set(this.records.map((record) => record.sequence)).size !== this.records.length) {
      this.sequenceError = 'Cada registro debe tener una secuencia diferente.';
      return;
    }
    this.sequenceError = '';
  }

  private syncContextUrl(): void {
    if (!this.selectedProfileId) {
      return;
    }
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { profileId: this.selectedProfileId },
      replaceUrl: true
    });
  }

  private friendlySaveError(error: Partial<NachaConfigApiError> | null): string {
    if (error?.errorCode === 'CONCURRENCY_CONFLICT' || error?.errorCode?.includes('409')) {
      return 'El perfil cambió mientras lo editabas. Recarga los datos e intenta nuevamente.';
    }
    if (error?.issues?.length) {
      return 'La secuencia contiene datos no válidos. Revisa las observaciones e intenta nuevamente.';
    }
    return 'No fue posible guardar la secuencia. Revisa los datos e intenta nuevamente.';
  }
}
