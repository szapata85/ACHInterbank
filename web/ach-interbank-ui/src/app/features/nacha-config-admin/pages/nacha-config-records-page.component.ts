import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
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
  saveIssues: NachaConfigValidationIssue[] = [];

  ngOnInit(): void {
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
          this.profilesError = 'No fue posible cargar perfiles oficiales NACHA Config.';
          this.notifications.error('No fue posible cargar perfiles oficiales NACHA Config.');
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
          this.cdr.markForCheck();
        },
        error: () => {
          this.selectedProfile = null;
          this.records = [];
          this.detailError = 'No fue posible cargar el detalle del perfil NACHA Config.';
          this.notifications.error('No fue posible cargar el detalle del perfil NACHA Config.');
          this.cdr.markForCheck();
        }
      });
  }

  guardarSecuencia(): void {
    if (!this.puedeEditarSecuencia || !this.selectedProfile || this.saving) {
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
          this.notifications.success('Secuencia de records actualizada correctamente.');
          this.loadSelectedProfile(this.selectedProfile!.id);
        },
        error: (error) => {
          const apiError = error as Partial<NachaConfigApiError> | null;
          this.saveError = apiError?.message?.trim() || 'No fue posible guardar la secuencia.';
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
    this.cdr.markForCheck();
  }

  irADetallePerfil(): void {
    if (!this.selectedProfileId) {
      return;
    }

    void this.router.navigate(['/nacha-config-admin/perfiles', this.selectedProfileId]);
  }

  irAVariantsFields(): void {
    void this.router.navigate(['/nacha-config-admin/variants-fields']);
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
}
