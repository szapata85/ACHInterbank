import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterModule, Router } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="status-page">
      <div class="code">403</div>
      <h1>No autorizado</h1>
      <p>No tienes permisos para acceder a este recurso.</p>
      <div class="actions">
        <button type="button" (click)="back()">Volver</button>
        <a routerLink="/dashboard">Ir al inicio</a>
      </div>
    </div>
  `,
  styles: [
    `
      .status-page {
        max-width: 480px;
        margin: 4rem auto;
        text-align: center;
        padding: 2rem;
        background: #fff;
        border-radius: 12px;
        box-shadow: 0 10px 35px rgba(0, 0, 0, 0.05);
      }
      .code {
        font-size: 4rem;
        font-weight: 700;
        color: #f59e0b;
      }
      h1 {
        margin: 0.5rem 0;
      }
      p {
        color: #6b7280;
      }
      .actions {
        margin-top: 1.5rem;
        display: flex;
        gap: 1rem;
        justify-content: center;
        align-items: center;
      }
      button,
      a {
        border: 1px solid #d1d5db;
        padding: 0.65rem 1.2rem;
        border-radius: 8px;
        background: #fff;
        cursor: pointer;
        text-decoration: none;
        color: #111827;
      }
      button:hover,
      a:hover {
        background: #f3f4f6;
      }
    `
  ]
})
export class UnauthorizedComponent {
  private readonly router = inject(Router);

  back(): void {
    if (window.history.length > 1) {
      this.router.navigateByUrl(this.router.url, { skipLocationChange: true }).then(() => window.history.back());
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}
