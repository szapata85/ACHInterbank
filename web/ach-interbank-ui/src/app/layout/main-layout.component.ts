import { ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { NavigationService } from '../core/services/navigation.service';
import { MenuItem } from '../core/models/menu.model';
import { SharedModule } from '../shared/shared.module';
import { RouterModule } from '@angular/router';

interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly authService = inject(AuthService);
  private readonly navigationService = inject(NavigationService);

  readonly user$ = this.authService.user$;
  menuItems: MenuItem[] = [];

  breadcrumbs: Breadcrumb[] = [];
  pageTitle = 'Inicio';
  isMenuOpen = false;

  private subscription?: Subscription;
  private menuSubscription?: Subscription;

  ngOnInit(): void {
    this.subscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.buildBreadcrumbs();
        this.handleNavigation();
        this.cdr.markForCheck();
      });

    this.buildBreadcrumbs();

    this.menuSubscription = this.navigationService.getMenu().subscribe((items) => {
      this.menuItems = items;
      this.cdr.markForCheck();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.menuSubscription?.unsubscribe();
  }

  logout(): void {
    this.authService.logout();
  }

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
    this.cdr.markForCheck();
  }

  closeMenu(): void {
    if (this.isMenuOpen) {
      this.isMenuOpen = false;
      this.cdr.markForCheck();
    }
  }

  onNavItemSelected(): void {
    if (this.isMobileView()) {
      this.closeMenu();
    }
  }

  onLogoutSelected(): void {
    this.logout();
    this.onNavItemSelected();
  }

  @HostListener('window:resize')
  onResize(): void {
    if (!this.isMobileView() && this.isMenuOpen) {
      this.closeMenu();
    }
  }

  private buildBreadcrumbs(): void {
    const breadcrumbs: Breadcrumb[] = [];
    let currentRoute: ActivatedRoute | null = this.route.root;
    let url = '';

    while (currentRoute) {
      const routeSnapshot = currentRoute.snapshot;
      const routeConfig = routeSnapshot.routeConfig;

      if (routeConfig && routeConfig.path) {
        const path = routeConfig.path.startsWith('/') ? routeConfig.path : `${url}/${routeConfig.path}`;
        url = path;
      }

      const label = routeSnapshot.data['breadcrumb'] as string | undefined;
      const title = routeSnapshot.data['title'] as string | undefined;

      if (label) {
        breadcrumbs.push({ label, url });
      }

      if (title) {
        this.pageTitle = title;
      }

      currentRoute = currentRoute.firstChild;
    }

    this.breadcrumbs = breadcrumbs;
  }

  private handleNavigation(): void {
    if (this.isMobileView()) {
      this.closeMenu();
    }
  }

  private isMobileView(): boolean {
    return typeof window !== 'undefined' && window.innerWidth < 960;
  }
}
