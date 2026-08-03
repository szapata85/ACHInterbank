import { CommonModule } from '@angular/common';
import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { LoadingService } from '../../core/services/loading.service';

@Component({
  selector: 'app-loading-overlay',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="overlay" *ngIf="loadingService.isLoading()">
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
        color: var(--color-text);
        text-shadow: 0 1px 0 rgba(255, 255, 255, 0.8);
      }
      .spinner {
        width: 64px;
        height: 64px;
        border-radius: 50%;
        border: 6px solid var(--color-border);
        border-top-color: var(--color-primary);
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
