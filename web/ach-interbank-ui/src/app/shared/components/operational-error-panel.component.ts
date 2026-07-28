import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { OperationalErrorView } from '../../core/models/operational-error.model';

@Component({
  selector: 'app-operational-error-panel',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatExpansionModule, MatIconModule],
  template: `
    <section class="error-panel" role="alert" aria-live="assertive">
      <mat-icon fontSet="material-symbols-outlined" aria-hidden="true">error_outline</mat-icon>
      <div class="content">
        <h2>{{ error.title }}</h2>
        <p>{{ error.message }}</p>
        <p class="action">{{ error.action }}</p>

        <div class="actions">
          <button *ngIf="error.retryable" mat-stroked-button type="button" (click)="retry.emit()">
            <mat-icon fontSet="material-symbols-outlined">refresh</mat-icon>
            Volver a intentar
          </button>
          <a
            *ngIf="error.correctionRoute"
            mat-button
            [routerLink]="error.correctionRoute">
            {{ error.correctionLabel }}
          </a>
        </div>

        <mat-expansion-panel *ngIf="hasSupportInformation" class="support-panel">
          <mat-expansion-panel-header>
            <mat-panel-title>Información para soporte</mat-panel-title>
          </mat-expansion-panel-header>
          <dl>
            <div *ngIf="error.support.errorCode">
              <dt>Código del error</dt><dd data-technical-value="true">{{ error.support.errorCode }}</dd>
            </div>
            <div *ngIf="error.support.ruleId">
              <dt>Regla</dt><dd data-technical-value="true">{{ error.support.ruleId }}</dd>
            </div>
            <div *ngIf="error.support.recordType">
              <dt>Tipo de registro</dt><dd data-technical-value="true">{{ error.support.recordType }}</dd>
            </div>
            <div *ngIf="error.support.fieldCode">
              <dt>Campo técnico</dt><dd data-technical-value="true">{{ error.support.fieldCode }}</dd>
            </div>
            <div *ngIf="error.support.fieldDisplayName">
              <dt>Nombre funcional</dt><dd>{{ error.support.fieldDisplayName }}</dd>
            </div>
            <div *ngIf="error.support.startPosition !== undefined">
              <dt>Posición</dt><dd>{{ error.support.startPosition }}</dd>
            </div>
            <div *ngIf="error.support.expectedLength !== undefined">
              <dt>Longitud requerida</dt><dd>{{ error.support.expectedLength }}</dd>
            </div>
            <div *ngIf="error.support.reason">
              <dt>Motivo técnico</dt><dd>{{ error.support.reason }}</dd>
            </div>
            <div *ngIf="error.support.traceId">
              <dt>Identificador de soporte</dt><dd data-technical-value="true">{{ error.support.traceId }}</dd>
            </div>
          </dl>
        </mat-expansion-panel>
      </div>
    </section>
  `,
  styles: [`
    .error-panel{display:grid;grid-template-columns:auto minmax(0,1fr);gap:.85rem;padding:1rem;border:1px solid #e1a6a6;border-radius:var(--radius-md);background:#fff7f7;color:#711f1f}
    .error-panel>mat-icon{margin-top:.15rem}.content h2{margin:0;font-size:1.05rem}.content p{margin:.35rem 0;color:#5f2525}.content .action{font-weight:600}
    .actions{display:flex;align-items:center;gap:.5rem;flex-wrap:wrap;margin-top:.75rem}.support-panel{margin-top:.85rem;background:#fff;box-shadow:none!important}
    dl{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.65rem 1rem;margin:0}dl div{min-width:0}dt{color:var(--color-text-muted);font-size:.78rem}dd{margin:.15rem 0 0;color:var(--color-text);overflow-wrap:anywhere}
    [data-technical-value="true"]{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:.82rem}
    @media(max-width:600px){dl{grid-template-columns:1fr}.actions{align-items:stretch;flex-direction:column}.actions button,.actions a{width:100%}}
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OperationalErrorPanelComponent {
  @Input({ required: true }) error!: OperationalErrorView;
  @Output() retry = new EventEmitter<void>();

  get hasSupportInformation(): boolean {
    return Object.values(this.error.support).some(value => value !== undefined && value !== null && value !== '');
  }
}
