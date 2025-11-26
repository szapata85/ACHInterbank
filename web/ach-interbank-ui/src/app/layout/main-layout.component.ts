import { ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Observable, Subscription, map, filter } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { UserSession } from '../core/models/auth.models';
import { SharedModule } from '../shared/shared.module';
import { RouterModule } from '@angular/router';
import { NAV_ITEMS, NavItem } from './navigation.config';

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

  readonly user$ = this.authService.user$;
  readonly navItems: NavItem[] = NAV_ITEMS;
  readonly menuItems$: Observable<NavItem[]> = this.authService.user$.pipe(
    map((user) => this.filterNavItems(this.navItems, user))
  );

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

  private filterNavItems(items: NavItem[], user: UserSession | null): NavItem[] {
    return items.filter((item) => this.canDisplay(item, user));
  }

  private canDisplay(item: NavItem, user: UserSession | null): boolean {
    if (!item.roles && !item.permissions) {
      return true;
    }

    if (!user) {
      return false;
    }

    const userRoles = (user.roles ?? []).map((role) => role.toLowerCase());
    const userPermissions = (user.permissions ?? []).map((permission) => permission.toLowerCase());

    const hasRequiredRole = !item.roles || item.roles.some((role) => userRoles.includes(role.toLowerCase()));
    const hasRequiredPermission =
      !item.permissions || item.permissions.some((permission) => userPermissions.includes(permission.toLowerCase()));

    return hasRequiredRole && hasRequiredPermission;
  }
}
