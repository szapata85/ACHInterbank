import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nacha-security-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="page-header">
      <h1>Panel de seguridad NACHA-M</h1>
      <p>Consola operativa para certificados, operaciones y sobre digital.</p>
      <ul>
        <li>Angular opera, backend cifra/descifra/valida.</li>
        <li>No se muestra el archivo plano si falla la firma.</li>
        <li>La descarga requiere autorización por operationId.</li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaSecurityDashboardComponent {}
