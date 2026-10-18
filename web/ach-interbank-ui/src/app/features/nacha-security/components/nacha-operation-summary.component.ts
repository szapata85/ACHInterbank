import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';
import {
  nachaOperationErrorMessage,
  nachaOperationStatusClass,
  nachaOperationStatusLabel
} from '../presentation/nacha-operation-presentation';

@Component({
  selector: 'app-nacha-operation-summary',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatChipsModule, MatIconModule],
  template: `
    <mat-card class="operation-result" appearance="outlined">
      <mat-card-header>
        <mat-card-title>{{ title }}</mat-card-title>
        <mat-chip [ngClass]="statusClass" aria-label="Estado de la operación">
          {{ statusLabel }}
        </mat-chip>
      </mat-card-header>

      <mat-card-content>
        <dl class="result-grid">
          <div>
            <dt>Identificador de operación</dt>
            <dd class="technical-value">{{ operation.operationId }}</dd>
          </div>
          <div>
            <dt>Estado</dt>
            <dd>{{ statusLabel }}</dd>
          </div>
          @if (operation.artifact.externalFileName) {
            <div>
              <dt>Archivo</dt>
              <dd>{{ operation.artifact.externalFileName }}</dd>
            </div>
          }
          @if (showDownloadAvailability) {
            <div>
              <dt>Descarga</dt>
              <dd>{{ operation.artifact.downloadAvailable ? 'Disponible' : 'No disponible' }}</dd>
            </div>
          }
          @if (showHashes && operation.artifact.plainHashSha256) {
            <div>
              <dt>Huella SHA-256 del archivo</dt>
              <dd class="technical-value">{{ operation.artifact.plainHashSha256 }}</dd>
            </div>
          }
          @if (showHashes && operation.artifact.envelopeHashSha256) {
            <div>
              <dt>Huella SHA-256 del sobre</dt>
              <dd class="technical-value">{{ operation.artifact.envelopeHashSha256 }}</dd>
            </div>
          }
        </dl>

        @if (operation.error) {
          <section class="operation-error" role="alert">
            <mat-icon aria-hidden="true">error</mat-icon>
            <div>
              <strong>No fue posible completar la operación.</strong>
              <p>{{ errorMessage }}</p>
              <small>Código para soporte: {{ operation.error.code }}</small>
            </div>
          </section>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    :host { display:block; }
    .operation-result { margin-top:var(--space-4); border-color:var(--color-border); background:var(--color-surface); }
    mat-card-header { display:flex; align-items:flex-start; justify-content:space-between; gap:var(--space-3); }
    mat-card-title { color:var(--color-text); }
    mat-card-content { padding-top:var(--space-3); }
    .result-grid { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:var(--space-3); margin:0; }
    .result-grid div { min-width:0; padding:var(--space-3); border:1px solid var(--color-border); border-radius:var(--radius-md); background:var(--color-surface-muted); }
    dt { color:var(--color-text-muted); font-size:var(--font-xs); font-weight:700; }
    dd { margin:var(--space-1) 0 0; color:var(--color-text); overflow-wrap:anywhere; }
    .technical-value { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:.82rem; }
    mat-chip.status-success { background:var(--color-success-surface); color:var(--color-success-text); }
    mat-chip.status-warning { background:var(--color-warning-surface); color:var(--color-warning-text); }
    mat-chip.status-error { background:var(--color-error-surface); color:var(--color-error-text); }
    mat-chip.status-neutral { background:var(--color-surface-muted); color:var(--color-text-muted); }
    .operation-error { display:flex; gap:var(--space-2); margin-top:var(--space-3); padding:var(--space-3); border:1px solid var(--color-error-border); border-radius:var(--radius-md); background:var(--color-error-surface); color:var(--color-error-text); }
    .operation-error p { margin:.2rem 0; }
    .operation-error small { display:block; margin-top:.35rem; }
    @media (max-width:640px) { .result-grid { grid-template-columns:1fr; } mat-card-header { flex-direction:column; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaOperationSummaryComponent {
  @Input({ required: true }) operation!: NachaSecurityOperationResponse;
  @Input() title = 'Resultado de la operación';
  @Input() showHashes = false;
  @Input() showDownloadAvailability = false;

  get statusLabel(): string {
    return nachaOperationStatusLabel(this.operation.status);
  }

  get statusClass(): string {
    return nachaOperationStatusClass(this.operation.status);
  }

  get errorMessage(): string {
    return this.operation.error ? nachaOperationErrorMessage(this.operation.error) : '';
  }
}
