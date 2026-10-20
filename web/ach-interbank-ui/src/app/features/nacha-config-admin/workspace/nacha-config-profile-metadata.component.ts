import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { NachaConfigWorkspace } from '../../../core/models/nacha-config.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';

@Component({
  selector: 'app-nacha-config-profile-metadata',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule],
  templateUrl: './nacha-config-profile-metadata.component.html',
  styleUrls: ['./nacha-config-profile-metadata.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigProfileMetadataComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  @Input({ required: true }) perfil!: NachaConfigWorkspace;
  @Output() recargar = new EventEmitter<void>();
  @Output() perfilCreado = new EventEmitter<number>();

  readonly puedeGestionar = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);
  guardando = false;
  clonando = false;
  operationError = '';

  readonly editarForm = this.fb.group({
    nombreEs: ['', [Validators.required, Validators.minLength(3)]],
    descripcion: [''],
    contextPriority: [100, [Validators.required, Validators.min(1)]],
    effectiveFrom: ['', Validators.required],
    effectiveTo: ['']
  });

  readonly cloneForm = this.fb.group({
    nuevoProfileCode: ['', [Validators.required, Validators.minLength(6)]],
    nuevoNombreEs: ['', [Validators.required, Validators.minLength(3)]],
    effectiveFrom: ['', Validators.required]
  });

  get puedeEditar(): boolean {
    return this.puedeGestionar && !!this.perfil?.isEditable;
  }

  ngOnChanges(): void {
    if (!this.perfil) {
      return;
    }

    this.editarForm.reset({
      nombreEs: this.perfil.nombreEs,
      descripcion: this.perfil.descripcion ?? '',
      contextPriority: this.perfil.contextPriority,
      effectiveFrom: this.dateInputValue(this.perfil.effectiveFrom),
      effectiveTo: this.dateInputValue(this.perfil.effectiveTo)
    }, { emitEvent: false });

    if (this.puedeEditar) {
      this.editarForm.enable({ emitEvent: false });
    } else {
      this.editarForm.disable({ emitEvent: false });
    }

    this.cloneForm.reset({
      nuevoProfileCode: `${this.perfil.profileCode}-CLONE`,
      nuevoNombreEs: `${this.perfil.nombreEs} (copia)`,
      effectiveFrom: this.dateInputValue(this.perfil.effectiveFrom)
    }, { emitEvent: false });

    if (this.puedeGestionar) {
      this.cloneForm.enable({ emitEvent: false });
    } else {
      this.cloneForm.disable({ emitEvent: false });
    }
  }

  guardar(): void {
    if (!this.puedeEditar || this.guardando || this.editarForm.invalid) {
      this.editarForm.markAllAsTouched();
      return;
    }

    const form = this.editarForm.getRawValue();
    this.operationError = '';
    this.guardando = true;
    this.command.editarBorrador(this.perfil.id, {
      nombreEs: (form.nombreEs ?? '').trim(),
      descripcion: (form.descripcion ?? '').trim() || null,
      contextPriority: Number(form.contextPriority ?? 100),
      effectiveFrom: form.effectiveFrom,
      effectiveTo: (form.effectiveTo ?? '').trim() || null,
      expectedRowVersion: this.perfil.rowVersion
    }).pipe(finalize(() => this.guardando = false)).subscribe({
      next: () => {
        this.notifications.success('Borrador actualizado correctamente.');
        this.recargar.emit();
      },
      error: (error: unknown) => {
        this.operationError = nachaConfigUserMessage(error, 'No fue posible guardar el borrador. La configuración se conserva sin cambios.');
        this.notifications.error(this.operationError);
      }
    });
  }

  cancelar(): void {
    this.ngOnChanges();
  }

  clonar(): void {
    if (!this.puedeGestionar || this.clonando || this.cloneForm.invalid) {
      this.cloneForm.markAllAsTouched();
      return;
    }

    const form = this.cloneForm.getRawValue();
    this.operationError = '';
    this.clonando = true;
    this.command.clonar(this.perfil.id, {
      nuevoProfileCode: (form.nuevoProfileCode ?? '').trim(),
      nuevoNombreEs: (form.nuevoNombreEs ?? '').trim(),
      effectiveFrom: form.effectiveFrom,
      expectedRowVersion: this.perfil.rowVersion
    }).pipe(finalize(() => this.clonando = false)).subscribe({
      next: (created) => {
        this.notifications.success(`Perfil clonado como ${created.profileCode}.`);
        this.perfilCreado.emit(created.id);
      },
      error: (error: unknown) => {
        this.operationError = nachaConfigUserMessage(error, 'No fue posible crear el borrador a partir de esta configuración.');
        this.notifications.error(this.operationError);
      }
    });
  }

  private dateInputValue(value?: string | null): string {
    return value ? new Date(value).toISOString().slice(0, 10) : '';
  }

}
