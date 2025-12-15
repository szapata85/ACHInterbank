import { ChangeDetectionStrategy, Component, HostBinding, OnDestroy, inject } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { RouterModule } from '@angular/router';
import { BrandingService } from '../core/services/branding.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-login-layout',
  templateUrl: './login-layout.component.html',
  styleUrls: ['./login-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class LoginLayoutComponent implements OnDestroy {
  private readonly brandingService = inject(BrandingService);

  @HostBinding('style.--public-bg')
  private publicBackground = this.brandingService.getBrandingSnapshot().publicBackground ?? null;

  readonly branding$ = this.brandingService.branding$;

  private brandingSubscription: Subscription = this.brandingService.branding$.subscribe((branding) => {
    this.publicBackground = branding.publicBackground ?? null;
  });

  ngOnDestroy(): void {
    this.brandingSubscription.unsubscribe();
  }
}
