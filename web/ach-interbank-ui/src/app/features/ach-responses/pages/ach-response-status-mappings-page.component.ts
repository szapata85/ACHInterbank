import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-ach-response-status-mappings-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h1>Homologaciones ACH</h1>
      <p>Consulta de mappings de estados y causales.</p>
      <p>Implementación vendrá en SPA 8.</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseStatusMappingsPageComponent {}
