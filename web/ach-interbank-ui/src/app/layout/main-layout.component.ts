import { ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../core/services/auth.service';

interface Breadcrumb {
  label: string;
  url: string;
}

interface NavItem {
  label: string;
  icon?: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly authService = inject(AuthService);

  readonly user$ = this.authService.user$;
  readonly navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Usuarios y roles', route: '/users', icon: 'group' },
    { label: 'Alias y cuentas', route: '/aliases', icon: 'key' },
    { label: 'Ciclos ACH', route: '/ach-cycles', icon: 'schedule' },
    { label: 'Transacciones', route: '/transactions', icon: 'swap_horiz' },
    { label: 'Crear transacción', route: '/transactions/create', icon: 'add' },
    { label: 'Catálogos', route: '/catalogs', icon: 'inventory' }
  ];

  breadcrumbs: Breadcrumb[] = [];
  pageTitle = 'Inicio';
  isMenuOpen = false;

  private subscription?: Subscription;

  ngOnInit(): void {
    this.subscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.buildBreadcrumbs();
        this.handleNavigation();
        this.cdr.markForCheck();
      });

    this.buildBreadcrumbs();
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
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
    return typeof window !== 'undefined' && window.innerWidth < 992;
  }
}
