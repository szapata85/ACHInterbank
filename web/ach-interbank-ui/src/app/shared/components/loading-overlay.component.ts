import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { LoadingService } from '../../core/services/loading.service';

@Component({
  selector: 'app-loading-overlay',
  template: `
    <div class="overlay" *ngIf="loadingService.isLoading$ | async">
      <div class="spinner"></div>
      <p>Cargando...</p>
    </div>
  `,
  styles: [
    `
      .overlay {
        position: fixed;
        inset: 0;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        background: rgba(255, 255, 255, 0.65);
        z-index: 9999;
        backdrop-filter: blur(2px);
        color: #111827;
        text-shadow: 0 1px 0 rgba(255, 255, 255, 0.8);
      }
      .spinner {
        width: 64px;
        height: 64px;
        border-radius: 50%;
        border: 6px solid #e5e7eb;
        border-top-color: #2563eb;
        animation: spin 1s linear infinite;
        margin-bottom: 0.75rem;
      }
      p {
        margin: 0;
        font-weight: 600;
        letter-spacing: 0.02em;
      }
      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingOverlayComponent {
  readonly loadingService = inject(LoadingService);
}
