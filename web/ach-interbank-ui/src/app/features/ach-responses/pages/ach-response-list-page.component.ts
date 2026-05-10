import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-ach-response-list-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h1>Command Center Respuestas ACH</h1>
      <p>Bandeja operativa de respuestas ACH.</p>
      <p>Implementación de grilla/filtros vendrá en SPA 4.</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseListPageComponent {}
