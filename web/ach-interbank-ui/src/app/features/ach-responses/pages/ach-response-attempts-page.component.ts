import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-ach-response-attempts-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h1>Intentos de notificación ACH</h1>
      <p>Seguimiento de intentos públicos de notificación.</p>
      <p>Implementación vendrá en SPA 6.</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseAttemptsPageComponent {}
