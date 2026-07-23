import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseAuditModel } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';

@Component({
  selector: 'app-ach-response-audit-page', standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  template: `
    <app-page-header title="Auditoría de respuestas ACH" description="Consulta inmutable por entidad"></app-page-header>
    <form class="filtros" [formGroup]="form">
      <div class="campo"><label>Entidad</label><select formControlName="entityType"><option value="response">Respuesta</option><option value="mapping">Mapping</option></select></div>
      <div class="campo"><label>Identificador</label><input type="text" formControlName="entityId" /></div>
      <ui-boton texto="Consultar" variante="primario" [deshabilitado]="loading" (accion)="load()"></ui-boton>
    </form>
    <p *ngIf="loading">Cargando auditoría...</p>
    <p *ngIf="!loading && loaded && entries.length === 0">No hay eventos de auditoría para la entidad.</p>
    <article *ngFor="let item of entries" class="nota-operativa">
      <strong>{{ item.action }}</strong> · {{ item.occurredAtUtc | date:'medium' }}
      <div>{{ item.previousState || '—' }} → {{ item.newState || '—' }}</div>
      <div>Actor: {{ item.actor }} · Motivo: {{ item.reason }}</div>
      <details *ngIf="item.sanitizedMetadata"><summary>Detalle técnico</summary><pre>{{ item.sanitizedMetadata }}</pre></details>
    </article>
  `,
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

  load(): void {
    if (this.form.invalid) { this.notifications.error('Indique el identificador de la entidad.'); return; }
    const raw = this.form.getRawValue();
    this.loading = true;
    const call = raw.entityType === 'mapping'
      ? this.api.getMappingAudit(Number(raw.entityId))
      : this.api.getResponseAudit(raw.entityId!);
    call.pipe(finalize(() => { this.loading = false; this.loaded = true; this.cdr.markForCheck(); }))
      .subscribe({ next: (items) => { this.entries = items ?? []; }, error: () => {
        this.entries = []; this.notifications.error('No fue posible cargar la auditoría.');
      }});
  }
}
