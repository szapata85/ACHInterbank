import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { RouterModule } from '@angular/router';
import { BrandingService } from '../core/services/branding.service';

@Component({
  selector: 'app-login-layout',
  templateUrl: './login-layout.component.html',
  styleUrls: ['./login-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class LoginLayoutComponent {
  private readonly brandingService = inject(BrandingService);
  readonly branding$ = this.brandingService.branding$;
  readonly productName = this.brandingService.productName;
}
