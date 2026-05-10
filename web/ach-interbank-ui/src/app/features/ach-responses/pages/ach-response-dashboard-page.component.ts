import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-ach-response-dashboard-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h1>Dashboard operativo ACH</h1>
      <p>Indicadores de respuestas ACH.</p>
      <p>Implementación vendrá en SPA 9.</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseDashboardPageComponent {}
