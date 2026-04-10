import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, TemplateRef } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ subtitle || description }}</p>
        <h2>{{ title }}</h2>
      </div>
      <div class="actions" *ngIf="actionsTemplate">
        <ng-container *ngTemplateOutlet="actionsTemplate"></ng-container>
      </div>
    </header>
  `,
  styles: [
    `
      .page-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1rem;
      }
      h2 {
        margin: 0;
        font-size: 1.5rem;
        color: #111827;
      }
      .eyebrow {
        margin: 0;
        color: #6b7280;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        font-size: 0.75rem;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
        align-items: center;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PageHeaderComponent {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() description = '';
  @Input() actionsTemplate?: TemplateRef<any> | null;
}
