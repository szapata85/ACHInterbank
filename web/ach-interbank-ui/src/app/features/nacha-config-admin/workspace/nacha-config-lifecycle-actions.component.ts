import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { finalize, firstValueFrom } from 'rxjs';
import { NachaConfigValidationResult, NachaConfigWorkspace } from '../../../core/models/nacha-config.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { OperationalConfirmDialogComponent } from '../../../shared/components/operational-confirm-dialog.component';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';
import { NachaConfigValidationSummaryComponent } from './nacha-config-validation-summary.component';

@Component({
  selector: 'app-nacha-config-lifecycle-actions',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatDialogModule, NachaConfigValidationSummaryComponent],
  templateUrl: './nacha-config-lifecycle-actions.component.html',
  styleUrls: ['./nacha-config-lifecycle-actions.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigLifecycleActionsComponent {
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  @Input({ required: true }) perfil!: NachaConfigWorkspace;
  @Output() recargar = new EventEmitter<void>();

  readonly puedeGestionar = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);
  validacion: NachaConfigValidationResult | null = null;
  validando = false;
  procesando = false;
  operationError = '';

  get estado(): string {
    return this.perfil?.estado?.toUpperCase?.() ?? '';
  }

  validar(): void {
    if (!this.perfil || this.validando) {
      return;
    }

    this.operationError = '';
    this.validando = true;
    this.command.validar(this.perfil.id).pipe(finalize(() => this.validando = false)).subscribe({
      next: (result) => {
        this.validacion = result;
        result.isValid
          ? this.notifications.success('La configuración pasó la validación.')
          : this.notifications.warning('La configuración requiere ajustes antes de publicarse.');
      },
      error: (error: unknown) => {
        this.operationError = nachaConfigUserMessage(error, 'No fue posible validar la configuración.');
        this.notifications.error(this.operationError);
      }
    });
  }

  inactivar(): void {
    if (!this.puedeGestionar || this.estado !== 'PUBLICADO' || this.procesando) {
      return;
    }
    this.confirmar('Inactivar configuración', 'La configuración dejará de estar activa para la resolución operativa.', 'Inactivar')
      .then((ok) => ok && this.ejecutarEstado('inactivar'));
  }

  archivar(): void {
    if (!this.puedeGestionar || !['PUBLICADO', 'INACTIVO'].includes(this.estado) || this.procesando) {
      return;
    }
    this.confirmar('Archivar configuración', 'La configuración quedará solo para consulta y podrá clonarse como borrador.', 'Archivar', true)
      .then((ok) => ok && this.ejecutarEstado('archivar'));
  }

  private ejecutarEstado(accion: 'inactivar' | 'archivar'): void {
    this.operationError = '';
    this.procesando = true;
    const request = accion === 'inactivar'
      ? this.command.inactivar(this.perfil.id, this.perfil.rowVersion)
      : this.command.archivar(this.perfil.id, this.perfil.rowVersion);

    request.pipe(finalize(() => this.procesando = false)).subscribe({
      next: () => {
        this.notifications.success(accion === 'inactivar' ? 'Configuración inactivada.' : 'Configuración archivada.');
        this.recargar.emit();
      },
      error: (error: unknown) => {
        const fallback = accion === 'inactivar' ? 'No fue posible inactivar la configuración.' : 'No fue posible archivar la configuración.';
        this.operationError = nachaConfigUserMessage(error, fallback);
        this.notifications.error(this.operationError);
      }
    });
  }

  private async confirmar(title: string, message: string, confirmLabel: string, destructive = false): Promise<boolean> {
    const result = await firstValueFrom(this.dialog.open(OperationalConfirmDialogComponent, {
      data: { title, message, confirmLabel, destructive },
      autoFocus: 'dialog',
      restoreFocus: true
    }).afterClosed());
    return result === true;
  }
}
