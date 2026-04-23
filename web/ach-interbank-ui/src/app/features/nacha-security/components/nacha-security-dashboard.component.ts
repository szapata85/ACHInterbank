import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nacha-security-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="page-header">
      <h1>Dashboard de seguridad NACHA-M</h1>
      <p>Consola operativa para certificados, operaciones y sobre digital.</p>
      <ul>
        <li>Angular opera, backend cifra/descifra/valida.</li>
        <li>No se muestra plano si firma falla.</li>
        <li>La descarga requiere autorización por operationId.</li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaSecurityDashboardComponent {}
