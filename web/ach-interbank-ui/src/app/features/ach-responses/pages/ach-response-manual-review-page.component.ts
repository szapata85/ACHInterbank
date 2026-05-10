import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-ach-response-manual-review-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h1>Revisión manual de respuestas ACH</h1>
      <p>Bandeja para respuestas no homologadas, errores funcionales o pendientes de reintento.</p>
      <p>Implementación vendrá en SPA 7.</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseManualReviewPageComponent {}
