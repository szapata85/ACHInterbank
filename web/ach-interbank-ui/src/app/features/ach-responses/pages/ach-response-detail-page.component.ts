import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-ach-response-detail-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h1>Detalle respuesta ACH</h1>
      <p>Vista de trazabilidad y homologación.</p>
      <p>Implementación vendrá en SPA 5.</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseDetailPageComponent {}
