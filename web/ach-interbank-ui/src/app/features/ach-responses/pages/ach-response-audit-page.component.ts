import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseAuditModel } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';

@Component({
  selector: 'app-ach-response-audit-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SharedModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  template: `
    <app-page-header title="Historial de respuestas" description="Consulta inmutable por entidad"></app-page-header>

    <form class="filtros" [formGroup]="form" (ngSubmit)="load()">
      <mat-form-field appearance="outline">
        <mat-label>Entidad</mat-label>
        <mat-select formControlName="entityType">
          <mat-option value="response">Respuesta</mat-option>
          <mat-option value="mapping">Homologación</mat-option>
        </mat-select>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Identificador</mat-label>
        <input matInput type="text" formControlName="entityId" />
      </mat-form-field>

      <div class="acciones">
        <button mat-flat-button color="primary" type="submit" [disabled]="loading">Consultar</button>
      </div>
    </form>

    <ui-estado-carga *ngIf="loading" [mensaje]="'Cargando auditoría de respuestas ACH...'"></ui-estado-carga>

    <ui-estado-error
      *ngIf="!loading && error"
      [mensaje]="'No fue posible cargar la auditoría.'"
      (reintentar)="load()"
    ></ui-estado-error>

    <ui-estado-vacio
      *ngIf="!loading && !error && loaded && entries.length === 0"
      [titulo]="'Sin eventos de auditoría'"
      [mensaje]="'No hay eventos de auditoría para la entidad.'"
      [sugerencia]="''"
    ></ui-estado-vacio>

    <section *ngIf="!loading && !error && entries.length > 0" class="eventos" aria-label="Eventos de auditoría">
      <article *ngFor="let item of entries" class="nota-operativa">
        <strong>{{ item.action }}</strong> · {{ item.occurredAtUtc | date:'medium' }}
        <div>{{ item.previousState || '—' }} → {{ item.newState || '—' }}</div>
        <div>Actor: {{ item.actor }} · Motivo: {{ item.reason }}</div>
        <details *ngIf="item.sanitizedMetadata">
          <summary>Detalle técnico</summary>
          <pre>{{ item.sanitizedMetadata }}</pre>
        </details>
      </article>
    </section>
  `,
  styles: [`
    .filtros{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:.75rem;margin-bottom:1rem}
    mat-form-field{width:100%}
    .acciones{display:flex;align-items:center;align-self:center}
    .eventos{display:grid;gap:.75rem}
    .nota-operativa{border:1px solid var(--color-border);border-radius:var(--radius-md);padding:.75rem;background:var(--color-surface)}
    .nota-operativa div{margin-top:.25rem;color:var(--color-text-soft)}
    details{margin-top:.5rem}
    pre{white-space:pre-wrap;overflow-wrap:anywhere;color:var(--color-text-soft)}
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseAuditPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AchResponsesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.group({ entityType: ['response', Validators.required], entityId: ['', Validators.required] });
  entries: AchResponseAuditModel[] = [];
  loading = false;
  loaded = false;
  error = false;

  load(): void {
    if (this.form.invalid) {
      this.notifications.error('Indique el identificador de la entidad.');
      return;
    }

    const raw = this.form.getRawValue();
    this.loading = true;
    this.error = false;

    const call = raw.entityType === 'mapping'
      ? this.api.getMappingAudit(Number(raw.entityId))
      : this.api.getResponseAudit(raw.entityId!);

    call.pipe(finalize(() => {
      this.loading = false;
      this.loaded = true;
      this.cdr.markForCheck();
    })).subscribe({
      next: (items) => {
        this.entries = items ?? [];
      },
      error: () => {
        this.entries = [];
        this.error = true;
        this.notifications.error('No fue posible cargar la auditoría.');
      }
    });
  }
}
