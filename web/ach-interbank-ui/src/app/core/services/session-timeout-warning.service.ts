import { Injectable, OnDestroy, inject } from '@angular/core';
import { BehaviorSubject, Subscription, timer } from 'rxjs';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';

@Injectable({ providedIn: 'root' })
export class SessionTimeoutWarningService implements OnDestroy {
  private static readonly warningWindowMs = 2 * 60 * 1000;

  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  private readonly warningSubject = new BehaviorSubject<boolean>(false);
  readonly warning$ = this.warningSubject.asObservable();

  private sessionSubscription?: Subscription;
  private timerSubscription?: Subscription;
  private lastNotifiedExpiry?: number;

  constructor() {
    this.sessionSubscription = this.authService.user$.subscribe((session) => {
      this.timerSubscription?.unsubscribe();
      this.timerSubscription = undefined;
      this.warningSubject.next(false);

      if (!session?.expiresAt) {
        this.lastNotifiedExpiry = undefined;
        return;
      }

      const expiresAt = session.expiresAt.getTime();
      const now = Date.now();
      if (expiresAt <= now) {
        this.lastNotifiedExpiry = undefined;
        return;
      }

      const msUntilWarning = expiresAt - now - SessionTimeoutWarningService.warningWindowMs;
      if (msUntilWarning <= 0) {
        this.notifyOnce(expiresAt);
        return;
      }

      this.timerSubscription = timer(msUntilWarning).subscribe(() => {
        this.notifyOnce(expiresAt);
      });
    });
  }

  ngOnDestroy(): void {
    this.sessionSubscription?.unsubscribe();
    this.timerSubscription?.unsubscribe();
  }

  private notifyOnce(expiryTimestamp: number): void {
    if (this.lastNotifiedExpiry === expiryTimestamp) {
      return;
    }

    this.lastNotifiedExpiry = expiryTimestamp;
    this.warningSubject.next(true);
    this.notifications.warning('Tu sesión expirará pronto. Guarda tu trabajo o inicia sesión nuevamente.');
  }

  extendSession(): void {
    this.warningSubject.next(false);
    this.authService.refreshSession().subscribe({
      error: () => this.authService.logout()
    });
  }

  logout(): void {
    this.warningSubject.next(false);
    this.authService.logout();
  }
}
