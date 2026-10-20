import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';

@Component({
  selector: 'app-nacha-security-dashboard',
  standalone: true,
  imports: [MatCardModule, MatIconModule, PageHeaderComponent],
  template: `
    <app-page-header
      title="Seguridad NACHA-M"
      subtitle="Operación de certificados, generación de archivos y sobre digital."
    ></app-page-header>

    <mat-card appearance="outlined" class="security-principles">
      <mat-card-header>
        <mat-card-title>Controles de seguridad</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <ul>
          <li><mat-icon aria-hidden="true">verified_user</mat-icon><span>El servidor realiza el cifrado, descifrado y la validación criptográfica.</span></li>
          <li><mat-icon aria-hidden="true">policy</mat-icon><span>El archivo plano no se habilita cuando falla la validación de firma.</span></li>
          <li><mat-icon aria-hidden="true">download_for_offline</mat-icon><span>La descarga requiere autorización previa asociada al identificador de operación.</span></li>
        </ul>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .security-principles { border-color:var(--color-border); background:var(--color-surface); }
    ul { display:grid; gap:var(--space-3); margin:0; padding:0; list-style:none; }
    li { display:flex; align-items:flex-start; gap:var(--space-2); color:var(--color-text); }
    mat-icon { color:var(--color-primary); }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaSecurityDashboardComponent {}
