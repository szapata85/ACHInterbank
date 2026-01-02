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
    RouterOutlet,
    NotificationContainerComponent,
    LoadingOverlayComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'ACH Interbank SPA';

  constructor(private readonly sessionTimeoutWarningService: SessionTimeoutWarningService) {}
}
