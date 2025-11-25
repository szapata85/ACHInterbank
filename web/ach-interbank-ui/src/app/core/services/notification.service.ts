import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type NotificationType = 'success' | 'error' | 'info' | 'warning';

export interface NotificationMessage {
  id: number;
  type: NotificationType;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private counter = 0;
  private readonly messagesSubject = new BehaviorSubject<NotificationMessage[]>([]);
  readonly messages$: Observable<NotificationMessage[]> = this.messagesSubject.asObservable();

  show(type: NotificationType, text: string): void {
    const id = ++this.counter;
    const message: NotificationMessage = { id, type, text };
    const current = this.messagesSubject.value;
    this.messagesSubject.next([...current, message]);
    setTimeout(() => this.dismiss(id), 4000);
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
    this.messagesSubject.next(this.messagesSubject.value.filter((x) => x.id !== id));
  }
}
