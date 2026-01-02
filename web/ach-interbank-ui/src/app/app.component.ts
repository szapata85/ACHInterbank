import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

// Ajusta las rutas si cambian, pero la idea es esta:
import { NotificationContainerComponent } from './shared/components/notification-container.component';
import { LoadingOverlayComponent } from './shared/components/loading-overlay.component';
import { SessionTimeoutWarningService } from './core/services/session-timeout-warning.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    NotificationContainerComponent,
    LoadingOverlayComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'ACH Interbank SPA';

  readonly sessionWarning$;
  readonly remainingSeconds$;

  constructor(private readonly sessionTimeoutWarningService: SessionTimeoutWarningService) {
    this.sessionWarning$ = this.sessionTimeoutWarningService.warning$;
    this.remainingSeconds$ = this.sessionTimeoutWarningService.remainingSeconds$;
  }

  formatRemaining(seconds: number | null): string {
    if (seconds === null) {
      return '';
    }

    const safe = Math.max(0, seconds);
    const minutes = Math.floor(safe / 60);
    const remaining = safe % 60;
    return `${minutes.toString().padStart(2, '0')}:${remaining.toString().padStart(2, '0')}`;
  }

  extendSession(): void {
    this.sessionTimeoutWarningService.extendSession();
  }

  logout(): void {
    this.sessionTimeoutWarningService.logout();
  }
}
