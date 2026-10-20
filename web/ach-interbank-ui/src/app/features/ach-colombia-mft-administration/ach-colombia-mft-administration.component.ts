import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize, forkJoin, map, Observable, of, switchMap, timeout, TimeoutError } from 'rxjs';
import { ManagedMftAdministration, ManagedMftConnectionTestResult } from '../../core/models/ach-colombia-file-exchange.model';
import {
  AchColombiaOperationalSchedule,
  AchColombiaOperationalScheduleAdministrationRow,
  SaveAchColombiaOperationalSchedule
} from '../../core/models/ach-colombia-operational-schedule.model';
import { AchColombiaFileExchangeService } from '../../core/services/ach-colombia-file-exchange.service';
import { AchColombiaOperationalSchedulesService } from '../../core/services/ach-colombia-operational-schedules.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

interface ScheduleEditorRow extends AchColombiaOperationalScheduleAdministrationRow {
  automaticTime: string;
  latestTime: string;
  active: boolean;
  baseline: string;
}

@Component({
  selector: 'app-ach-colombia-mft-administration',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './ach-colombia-mft-administration.component.html',
  styleUrl: './ach-colombia-mft-administration.component.scss'
})
export class AchColombiaMftAdministrationComponent implements OnInit {
  private readonly api = inject(AchColombiaFileExchangeService);
  private readonly schedulesApi = inject(AchColombiaOperationalSchedulesService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  readonly canManage = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);
  readonly scheduleColumns = ['cycle', 'window', 'automaticTime', 'latestTime', 'active'];
  readonly homologationStatus = 'Pendiente de homologación externa';

  model?: ManagedMftAdministration;
  schedules: ScheduleEditorRow[] = [];
  connectionType: 'ManagedFolder' | 'SftpEndpoint' = 'ManagedFolder';
  secret = '';
  masterAutomaticOutbound = false;
  masterAutomaticInbound = false;
  loading = false;
  refreshing = false;
  savingConnection = false;
  savingSchedules = false;
  testingConnection = false;
  error = '';
  connectionTest?: ManagedMftConnectionTestResult;

  ngOnInit(): void {
    this.load();
  }

  get isSftp(): boolean {
    return this.connectionType === 'SftpEndpoint';
  }

  get connectionTypeLabel(): string {
    return this.isSftp ? 'SFTP' : 'Carpeta compartida';
  }

  load(): void {
    if (this.loading || this.refreshing || this.savingConnection || this.savingSchedules || this.testingConnection) return;
    // Keep the editor mounted on refresh; a slow API must not blank the page.
    const isRefresh = !!this.model;
    this.loading = !isRefresh;
    this.refreshing = isRefresh;
    this.error = '';
    forkJoin({
      configuration: this.api.administration(),
      schedules: this.schedulesApi.administration()
    }).pipe(
      timeout({ first: 15000 }),
      finalize(() => { this.loading = false; this.refreshing = false; })
    ).subscribe({
      next: ({ configuration, schedules }) => {
        this.applyConfiguration(configuration);
        this.schedules = schedules.map(row => this.toEditorRow(row));
        if (isRefresh) this.notifications.info('Información actualizada.');
      },
      error: error => {
        this.error = error instanceof TimeoutError
          ? 'La actualización tardó demasiado. Puedes intentarlo nuevamente sin recargar el navegador.'
          : 'No fue posible actualizar la configuración. Puedes reintentar sin salir del formulario.';
      }
    });
  }

  saveConnection(): void {
    if (!this.model || !this.canManage || this.savingConnection || this.savingSchedules || this.refreshing) return;
    this.error = '';
    this.connectionTest = undefined;

    if (this.isSftp && this.model.profileEnabled) {
      if (!this.model.endpoint?.trim() || !this.model.port || !this.model.principal?.trim()) {
        this.notifications.error('Completa servidor, puerto y usuario para activar SFTP.');
        return;
      }
      if (!this.model.sftpHostKeyFingerprint?.trim()) {
        this.notifications.error('Antes de activar SFTP, solicita al equipo técnico la validación de identidad del servidor.');
        return;
      }
      if (!this.model.credentialConfigured && !this.secret.trim()) {
        this.notifications.error('Configura una credencial antes de activar la conexión SFTP.');
        return;
      }
    }

    const payload = this.connectionPayload(this.model);
    this.savingConnection = true;
    const request = this.secret.trim()
      ? this.api.setCredential('Password', this.secret, this.model.concurrencyToken).pipe(
          switchMap(credentialState => this.api.updateAdministration({
            ...payload,
            concurrencyToken: credentialState.concurrencyToken
          })))
      : this.api.updateAdministration(payload);

    request.pipe(finalize(() => this.savingConnection = false)).subscribe({
      next: result => {
        this.applyConfiguration(result);
        this.secret = '';
        this.notifications.success('Configuración guardada.');
      },
      error: error => this.handleEditError(error, 'No fue posible guardar la configuración.')
    });
  }

  testConnection(): void {
    if (!this.canManage || this.testingConnection || this.savingConnection || this.savingSchedules || this.refreshing) return;
    this.testingConnection = true;
    this.connectionTest = undefined;
    this.api.testConnection().pipe(finalize(() => this.testingConnection = false)).subscribe({
      next: result => {
        this.connectionTest = result;
        result.succeeded ? this.notifications.success('Conexión exitosa.') : this.notifications.error(this.connectionMessage(result));
      },
      error: () => {
        this.connectionTest = { succeeded: false, code: 'ACHCOL_MFT_CONNECTION_FAILED', message: 'No fue posible conectar.' };
        this.notifications.error('No fue posible conectar.');
      }
    });
  }

  saveSchedules(): void {
    if (!this.model || !this.canManage || this.savingConnection || this.savingSchedules || this.refreshing) return;

    const invalid = this.schedules.find(row => row.active && !row.automaticTime);
    if (invalid) {
      this.notifications.error(`Defina la hora automática para ${invalid.cycleName} antes de activarlo.`);
      return;
    }

    const changedRows = this.schedules.filter(row => this.isScheduleDirty(row) && (row.scheduleId || this.hasScheduleValues(row)));
    const masterChanged = this.masterAutomaticOutbound !== this.model.automaticOutboundEnabled
      || this.masterAutomaticInbound !== this.model.automaticInboundEnabled;
    if (!masterChanged && changedRows.length === 0) {
      this.notifications.info('No hay cambios de horarios para guardar.');
      return;
    }

    this.savingSchedules = true;
    const masterSave$ = masterChanged
      ? this.api.updateAdministration({ ...this.model,
          automaticOutboundEnabled: this.masterAutomaticOutbound,
          automaticInboundEnabled: this.masterAutomaticInbound }).pipe(
          map(updated => { this.applyConfiguration(updated, false); return updated; }))
      : of(this.model);

    masterSave$.pipe(
      switchMap(() => changedRows.length ? forkJoin(changedRows.map(row => this.saveScheduleRow(row))) : of([])),
      switchMap(() => this.schedulesApi.administration()),
      finalize(() => this.savingSchedules = false)
    ).subscribe({
      next: rows => {
        this.schedules = rows.map(row => this.toEditorRow(row));
        this.notifications.success('Horarios automáticos guardados.');
      },
      error: error => this.handleEditError(error, 'No fue posible guardar los horarios.')
    });
  }

  cycleLabel(row: ScheduleEditorRow): string {
    const match = row.cycleName.match(/(\d+)/);
    return match ? `C${match[1]}` : row.cycleName;
  }

  windowLabel(row: ScheduleEditorRow): string {
    return `${this.displayTime(row.regulatoryStartTime)} – ${this.displayTime(row.regulatoryCutoffTime)}`;
  }

  private saveScheduleRow(row: ScheduleEditorRow): Observable<AchColombiaOperationalSchedule> {
    const payload: SaveAchColombiaOperationalSchedule = {
      clearingHouseId: row.clearingHouseId,
      clearingHouseCycleConfigId: row.clearingHouseCycleConfigId,
      automaticOutboundEnabled: row.active,
      automaticOutboundStartTime: this.apiTime(row.automaticTime),
      automaticOutboundLatestStartTime: this.apiTime(row.latestTime),
      outboundExecutionStrategy: row.latestTime ? 2 : 1,
      effectiveTimeZoneId: row.effectiveTimeZoneId || 'America/Bogota',
      isActive: row.active && row.regulatoryCycleActive,
      expectedRowVersion: row.rowVersion ?? undefined
    };

    return row.scheduleId
      ? this.schedulesApi.update(row.scheduleId, payload)
      : this.schedulesApi.create(payload);
  }

  private applyConfiguration(configuration: ManagedMftAdministration, resetMaster = true): void {
    this.model = { ...configuration };
    this.connectionType = configuration.provider === 'SftpEndpoint' ? 'SftpEndpoint' : 'ManagedFolder';
    if (resetMaster) {
      this.masterAutomaticOutbound = configuration.automaticOutboundEnabled;
      this.masterAutomaticInbound = configuration.automaticInboundEnabled;
    }
  }

  private connectionPayload(model: ManagedMftAdministration): ManagedMftAdministration {
    return {
      ...model,
      provider: this.isSftp ? 'SftpEndpoint' : 'ManagedFolder',
      protocol: this.isSftp ? 'Sftp' : 'ManagedFile',
      credentialType: model.credentialType ?? 'Password',
      sftpTimeoutSeconds: model.sftpTimeoutSeconds || 30
    };
  }

  private toEditorRow(row: AchColombiaOperationalScheduleAdministrationRow): ScheduleEditorRow {
    const editor: ScheduleEditorRow = {
      ...row,
      automaticTime: this.inputTime(row.automaticOutboundStartTime),
      latestTime: this.inputTime(row.automaticOutboundLatestStartTime),
      active: row.automaticOutboundEnabled && row.isActive,
      baseline: ''
    };
    editor.baseline = this.scheduleSignature(editor);
    return editor;
  }

  private hasScheduleValues(row: ScheduleEditorRow): boolean {
    return row.active || !!row.automaticTime || !!row.latestTime;
  }

  private isScheduleDirty(row: ScheduleEditorRow): boolean {
    return row.baseline !== this.scheduleSignature(row);
  }

  private scheduleSignature(row: ScheduleEditorRow): string {
    return JSON.stringify([row.automaticTime || '', row.latestTime || '', row.active]);
  }

  private inputTime(value?: string | null): string {
    return value ? value.slice(0, 5) : '';
  }

  private apiTime(value: string): string | null {
    return value ? `${value}:00` : null;
  }

  private displayTime(value?: string | null): string {
    return value ? value.slice(0, 5) : 'Pendiente';
  }

  connectionMessage(result: ManagedMftConnectionTestResult): string {
    switch (result.code) {
      case 'ACHCOL_MFT_SFTP_AUTHENTICATION_FAILED': return 'Usuario o credencial incorrecta.';
      case 'ACHCOL_MFT_SFTP_HOST_KEY_REJECTED': return 'No se pudo verificar la identidad del servidor SFTP. Contacta al equipo de infraestructura.';
      case 'ACHCOL_MFT_SFTP_ACCESS_DENIED':
      case 'ACHCOL_MFT_SFTP_PATH_NOT_FOUND':
      case 'ACHCOL_MFT_FOLDER_ACCESS_DENIED':
      case 'ACHCOL_MFT_FOLDER_PATH_NOT_FOUND': return 'No hay acceso a la carpeta.';
      default: return result.succeeded ? 'Conexión exitosa.' : 'No fue posible conectar.';
    }
  }

  private handleEditError(error: unknown, fallback: string): void {
    const http = error as HttpErrorResponse;
    if (http?.status === 409) {
      const message = 'La configuración fue modificada por otro usuario. Pulsa Actualizar e inténtalo nuevamente.';
      this.error = message;
      this.notifications.error(message);
      return;
    }
    this.notifications.error(fallback);
  }
}
