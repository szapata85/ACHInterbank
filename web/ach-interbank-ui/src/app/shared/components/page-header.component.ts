import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, TemplateRef } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="page-header">
      <div class="page-header__copy">
        <p class="eyebrow" *ngIf="subtitle || description">{{ subtitle || description }}</p>
        <h2>{{ title }}</h2>
      </div>
      <div class="actions" *ngIf="actionsTemplate">
        <ng-container *ngTemplateOutlet="actionsTemplate"></ng-container>
      </div>
    </header>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .page-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--space-4);
        margin-bottom: var(--space-4);
      }

      .page-header__copy {
        min-width: 0;
      }

      h2 {
        margin: 0;
        color: var(--color-text);
        font-size: 1.5rem;
        line-height: 1.25;
      }

      .eyebrow {
        margin: 0 0 var(--space-1);
        color: var(--color-text-muted);
        font-size: var(--font-xs);
        font-weight: 700;
        letter-spacing: 0.05em;
        text-transform: uppercase;
      }

      .actions {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: var(--space-2);
        flex-wrap: wrap;
      }

      @media (max-width: 640px) {
        .page-header {
          flex-direction: column;
        }

        .actions {
          width: 100%;
          justify-content: flex-start;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PageHeaderComponent {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() description = '';
  @Input() actionsTemplate?: TemplateRef<unknown> | null;
}
