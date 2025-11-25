import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-not-found',
  template: `
    <div class="status-page">
      <div class="code">404</div>
      <h1>Página no encontrada</h1>
      <p>La ruta solicitada no existe o fue movida.</p>
      <a routerLink="/dashboard">Ir al inicio</a>
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
        color: #ef4444;
      }
      h1 {
        margin: 0.5rem 0;
      }
      p {
        color: #6b7280;
      }
      a {
        display: inline-block;
        margin-top: 1.5rem;
        border: 1px solid #d1d5db;
        padding: 0.65rem 1.2rem;
        border-radius: 8px;
        background: #fff;
        text-decoration: none;
        color: #111827;
      }
      a:hover {
        background: #f3f4f6;
      }
    `
  ]
})
export class NotFoundComponent {}
