import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { NotificationMessage, NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-notification-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="notifications" aria-live="polite" aria-atomic="true">
      <article *ngFor="let message of messages" class="toast" [class]="'toast ' + message.type">
        <span class="type">{{ translate(message.type) }}</span>
        <p>{{ message.text }}</p>
        <button type="button" aria-label="Cerrar" (click)="dismiss(message.id)">×</button>
      </article>
    </section>
  `,
  styles: [
    `
      .notifications {
        position: fixed;
        right: 1rem;
        bottom: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        z-index: 1200;
      }
      .toast {
        min-width: 260px;
        max-width: 360px;
        background: #fff;
        border-radius: 8px;
        padding: 0.75rem 0.9rem;
        box-shadow: 0 10px 25px rgba(0, 0, 0, 0.08);
        border-left: 4px solid #2563eb;
        position: relative;
      }
      .toast.success {
        border-color: #16a34a;
      }
      .toast.error {
        border-color: #dc2626;
      }
      .toast.warning {
        border-color: #f59e0b;
      }
      .type {
        display: inline-block;
        font-size: 0.75rem;
        color: #6b7280;
      }
      p {
        margin: 0.15rem 0 0;
        color: #111827;
      }
      button {
        position: absolute;
        top: 0.3rem;
        right: 0.35rem;
        border: none;
        background: transparent;
        cursor: pointer;
        font-size: 1rem;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationContainerComponent implements OnInit, OnDestroy {
  private readonly notificationService = inject(NotificationService);
  private subscription?: Subscription;

  messages: NotificationMessage[] = [];

  ngOnInit(): void {
    this.subscription = this.notificationService.messages$.subscribe((messages) => (this.messages = messages));
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  dismiss(id: number): void {
    this.notificationService.dismiss(id);
  }

  translate(type: NotificationMessage['type']): string {
    switch (type) {
      case 'success':
        return 'Éxito';
      case 'error':
        return 'Error';
      case 'warning':
        return 'Alerta';
      default:
        return 'Info';
    }
  }
}
