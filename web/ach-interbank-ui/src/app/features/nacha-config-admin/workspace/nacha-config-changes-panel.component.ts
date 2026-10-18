import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize, firstValueFrom, switchMap } from 'rxjs';
import {
  NachaConfigApiError,
  NachaConfigChangeReview,
  NachaConfigOperationalTestResult,
  NachaConfigPublicationResult,
  NachaConfigWorkspace
} from '../../../core/models/nacha-config.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { OperationalConfirmDialogComponent } from '../../../shared/components/operational-confirm-dialog.component';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';

@Component({
  selector: 'app-nacha-config-changes-panel',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatChipsModule, MatDialogModule, MatProgressSpinnerModule],
  templateUrl: './nacha-config-changes-panel.component.html',
  styleUrls: ['./nacha-config-changes-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigChangesPanelComponent implements OnChanges {
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  @Input({ required: true }) perfil!: NachaConfigWorkspace;
  @Output() recargar = new EventEmitter<void>();

  review: NachaConfigChangeReview | null = null;
  publicationResult: NachaConfigPublicationResult | null = null;
  loading = false;
  publishing = false;
  errorMessage = '';
  validation: NachaConfigOperationalTestResult | null = null;

  readonly canManage = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['perfil']?.currentValue?.id) {
      this.load();
    }
  }

  load(): void {
    if (!this.perfil?.id || this.loading) {
      return;
    }
    this.loading = true;
    this.errorMessage = '';
    this.query.cambios(this.perfil.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: (review) => this.review = review,
      error: (error: unknown) => {
        this.review = null;
        this.errorMessage = nachaConfigUserMessage(
          error,
          'No fue posible revisar los cambios. La publicación permanece bloqueada.'
        );
      }
    });
  }

  publicar(): void {
    if (!this.canPublish || this.publishing) {
      return;
    }

    this.publishing = true;
    this.errorMessage = '';
    this.command.probar(this.perfil.id).pipe(
      switchMap((validation) => {
        this.validation = validation;
        if (!validation.canPublish) {
          throw { errorCode: 'VALIDATION_BLOCKED', message: 'La configuración tiene errores bloqueantes. Corrígelos y vuelve a probarla.' } as NachaConfigApiError;
        }
        return this.query.cambios(this.perfil.id);
      })
    ).subscribe({
      next: (freshReview) => {
        this.review = freshReview;
        void this.confirmAndPublish(freshReview);
      },
      error: (error: NachaConfigApiError) => {
        this.publishing = false;
        this.errorMessage = error.errorCode === 'VALIDATION_BLOCKED'
          ? 'La configuración tiene errores bloqueantes. Corrígelos desde “Probar” antes de publicar.'
          : nachaConfigUserMessage(error, 'No fue posible preparar la publicación. La publicación permanece bloqueada.');
        error.errorCode === 'VALIDATION_BLOCKED'
          ? this.notifications.warning(this.errorMessage)
          : this.notifications.error(this.errorMessage);
      }
    });
  }

  get canPublish(): boolean {
    return !!this.review
      && this.review.isEditable
      && this.perfil?.estado?.toUpperCase() === 'BORRADOR'
      && this.canManage
      && !this.loading;
  }

  private async confirmAndPublish(review: NachaConfigChangeReview): Promise<void> {
    const context = [review.camaraNombreEs, review.flujoNombreEs, review.direccionNombreEs, review.servicioNombreEs]
      .filter((value): value is string => !!value)
      .join(' · ');
    const validity = review.effectiveTo
      ? `${this.dateOnly(review.effectiveFrom)} a ${this.dateOnly(review.effectiveTo)}`
      : `desde ${this.dateOnly(review.effectiveFrom)}`;
    const confirmed = await firstValueFrom(this.dialog.open(OperationalConfirmDialogComponent, {
      data: {
        title: 'Publicar nueva versión',
        message: `${review.changeCount} cambios reales en ${review.changedFieldsCount} campos. Versión actual ${review.currentVersion}; nueva versión ${review.nextVersion}. Vigencia ${validity}. Contexto: ${context}. La versión publicada quedará protegida y no podrá editarse directamente.`,
        confirmLabel: 'Publicar nueva versión'
      },
      autoFocus: 'dialog',
      restoreFocus: true
    }).afterClosed());

    if (confirmed !== true) {
      this.publishing = false;
      return;
    }

    this.publishing = true;
    this.command.publicar(this.perfil.id, review.rowVersion).pipe(finalize(() => this.publishing = false)).subscribe({
      next: (result) => {
        this.publicationResult = result;
        this.notifications.success(result.mensaje || 'Configuración publicada correctamente.');
        this.recargar.emit();
      },
      error: (error: NachaConfigApiError) => {
        this.errorMessage = nachaConfigUserMessage(
          error,
          'No fue posible publicar la configuración. La versión anterior permanece vigente.'
        );
        this.notifications.error(this.errorMessage);
      }
    });
  }

  private dateOnly(value: string): string {
    return value?.slice(0, 10) || 'sin fecha';
  }
}
