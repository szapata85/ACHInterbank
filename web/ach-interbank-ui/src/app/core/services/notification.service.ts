import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type NotificationType = 'success' | 'error' | 'info' | 'warning';

export interface NotificationMessage {
  id: number;
  type: NotificationType;
  text: string;
}

interface NotificationOptions {
  autoCloseMs?: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private static readonly defaultAutoCloseMs = 4000;
  private counter = 0;
  private readonly messagesSubject = new BehaviorSubject<NotificationMessage[]>([]);
  readonly messages$: Observable<NotificationMessage[]> = this.messagesSubject.asObservable();
  private readonly dismissTimers = new Map<number, ReturnType<typeof setTimeout>>();

  show(type: NotificationType, text: string, options?: NotificationOptions): void {
    const trimmedText = text?.trim() ?? '';
    const autoCloseMs = options?.autoCloseMs ?? NotificationService.defaultAutoCloseMs;

    const current = this.messagesSubject.value;
    const existing = current.find((message) => message.type === type && message.text === trimmedText);

    if (existing) {
      this.scheduleDismiss(existing.id, autoCloseMs);
      return;
    }

    const id = ++this.counter;
    const message: NotificationMessage = { id, type, text: trimmedText };
    this.messagesSubject.next([...current, message]);
    this.scheduleDismiss(id, autoCloseMs);
  }

  success(text: string): void {
    this.show('success', text);
  }

  error(text: string): void {
    this.show('error', text);
  }

  info(text: string): void {
    this.show('info', text);
  }

  warning(text: string): void {
    this.show('warning', text);
  }

  dismiss(id: number): void {
    this.clearTimer(id);
    this.messagesSubject.next(this.messagesSubject.value.filter((x) => x.id !== id));
  }

  private scheduleDismiss(id: number, autoCloseMs: number): void {
    this.clearTimer(id);
    if (autoCloseMs <= 0) {
      return;
    }

    const timer = setTimeout(() => {
      this.dismiss(id);
    }, autoCloseMs);

    this.dismissTimers.set(id, timer);
  }

  private clearTimer(id: number): void {
    const timer = this.dismissTimers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.dismissTimers.delete(id);
    }
  }
}
