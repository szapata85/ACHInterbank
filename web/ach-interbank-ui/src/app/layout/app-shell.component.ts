import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-shell',
  templateUrl: './app-shell.component.html',
  styleUrls: ['./app-shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppShellComponent {
  private readonly authService = inject(AuthService);
  readonly user$ = this.authService.user$;

  logout(): void {
    this.authService.logout();
  }
}
