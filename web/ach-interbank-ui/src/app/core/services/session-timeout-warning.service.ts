import { Injectable, OnDestroy, inject } from '@angular/core';
import { BehaviorSubject, Subscription, interval, timer } from 'rxjs';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';

@Injectable({ providedIn: 'root' })
export class SessionTimeoutWarningService implements OnDestroy {
  private static readonly fallbackWarningWindowMs = 2 * 60 * 1000;

  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  private readonly warningSubject = new BehaviorSubject<boolean>(false);
  readonly warning$ = this.warningSubject.asObservable();
  private readonly remainingSecondsSubject = new BehaviorSubject<number | null>(null);
  readonly remainingSeconds$ = this.remainingSecondsSubject.asObservable();

  private sessionSubscription?: Subscription;
  private timerSubscription?: Subscription;
  private countdownSubscription?: Subscription;
  private lastNotifiedExpiry?: number;

  constructor() {
    this.sessionSubscription = this.authService.user$.subscribe((session) => {
      this.timerSubscription?.unsubscribe();
      this.timerSubscription = undefined;
      this.countdownSubscription?.unsubscribe();
      this.countdownSubscription = undefined;
      this.warningSubject.next(false);
      this.remainingSecondsSubject.next(null);

      if (!session?.expiresAt) {
        this.lastNotifiedExpiry = undefined;
        return;
      }

      const expiresAt = session.expiresAt.getTime();
      const now = Date.now();
      if (expiresAt <= now) {
        this.lastNotifiedExpiry = undefined;
        this.remainingSecondsSubject.next(null);
        return;
      }

      const warningAt = this.resolveWarningAt(session, expiresAt);
      const msUntilWarning = warningAt - now;
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
    this.startCountdown(expiryTimestamp);
    this.notifications.warning('Tu sesión expirará pronto. Guarda tu trabajo o inicia sesión nuevamente.');
  }

  extendSession(): void {
    this.warningSubject.next(false);
    this.remainingSecondsSubject.next(null);
    this.countdownSubscription?.unsubscribe();
    this.countdownSubscription = undefined;
    this.authService.refreshSession().subscribe({
      error: () => this.authService.logout()
    });
  }

  logout(): void {
    this.warningSubject.next(false);
    this.remainingSecondsSubject.next(null);
    this.countdownSubscription?.unsubscribe();
    this.countdownSubscription = undefined;
    this.authService.logout();
  }

  private resolveWarningAt(session: { issuedAt?: Date }, expiresAt: number): number {
    if (session.issuedAt) {
      const issuedAt = session.issuedAt.getTime();
      const total = expiresAt - issuedAt;
      if (total > 0) {
        return expiresAt - total * 0.2;
      }
    }

    return expiresAt - SessionTimeoutWarningService.fallbackWarningWindowMs;
  }

  private startCountdown(expiryTimestamp: number): void {
    this.countdownSubscription?.unsubscribe();
    this.countdownSubscription = interval(1000).subscribe(() => {
      const remainingMs = expiryTimestamp - Date.now();
      const remainingSeconds = Math.max(0, Math.ceil(remainingMs / 1000));
      this.remainingSecondsSubject.next(remainingSeconds);
      if (remainingSeconds <= 0) {
        this.countdownSubscription?.unsubscribe();
        this.countdownSubscription = undefined;
      }
    });
  }
}
