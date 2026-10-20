import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { NachaConfigValidationResult } from '../../../core/models/nacha-config.model';

@Component({
  selector: 'app-nacha-config-validation-summary',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatChipsModule],
  template: `
    <mat-card *ngIf="result" data-testid="nacha-config-validation-summary">
      <mat-card-header>
        <mat-card-title>Última validación</mat-card-title>
        <mat-card-subtitle>{{ result.resumen }}</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <mat-chip-set aria-label="Resultado de validación">
          <mat-chip>{{ result.isValid ? 'Apto para publicar' : 'Requiere ajustes' }}</mat-chip>
          <mat-chip>Errores: {{ result.erroresBloqueantes }}</mat-chip>
          <mat-chip>Advertencias: {{ result.advertencias }}</mat-chip>
        </mat-chip-set>
        <ul *ngIf="result.issues.length">
          <li *ngFor="let issue of result.issues">{{ issue.mensaje }}</li>
        </ul>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`ul { margin: 1rem 0 0; padding-inline-start: 1.2rem; }`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigValidationSummaryComponent {
  @Input() result: NachaConfigValidationResult | null = null;
}
