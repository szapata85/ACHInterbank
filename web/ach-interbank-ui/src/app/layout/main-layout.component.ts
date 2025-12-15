import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostBinding,
  HostListener,
  OnDestroy,
  OnInit,
  inject
} from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { NavigationService } from '../core/services/navigation.service';
import { MenuItem } from '../core/models/menu.model';
import { SharedModule } from '../shared/shared.module';
import { RouterModule } from '@angular/router';
import { BrandingService } from '../core/services/branding.service';

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
  private readonly brandingService = inject(BrandingService);

  @HostBinding('style.--private-bg')
  public privateBackground: string | null = this.brandingService.getBrandingSnapshot().privateBackground ?? null;
  @HostBinding('style.--button-color')
  public buttonColor: string | null = this.brandingService.getBrandingSnapshot().buttonColor ?? null;

  readonly user$ = this.authService.user$;
  readonly branding$ = this.brandingService.branding$;
  menuItems: MenuItem[] = [];
  expandedItems = new Set<number>();

  breadcrumbs: Breadcrumb[] = [];
  pageTitle = 'Inicio';
  isMenuOpen = false;

  private subscription?: Subscription;
  private menuSubscription?: Subscription;
  private brandingSubscription?: Subscription;

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
      this.syncExpandedItems();
      this.cdr.markForCheck();
    });

    this.brandingSubscription = this.brandingService.branding$.subscribe((branding) => {
      this.privateBackground = branding.privateBackground ?? null;
      this.buttonColor = branding.buttonColor ?? null;
      this.cdr.markForCheck();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.menuSubscription?.unsubscribe();
    this.brandingSubscription?.unsubscribe();
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

  toggleSubmenu(item: MenuItem, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    const key = item.id;

    if (this.expandedItems.has(key)) {
      this.expandedItems.delete(key);
    } else {
      this.expandedItems.add(key);
    }

    this.cdr.markForCheck();
  }

  isItemExpanded(item: MenuItem): boolean {
    return this.expandedItems.has(item.id);
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

    this.syncExpandedItems();
  }

  private isMobileView(): boolean {
    return typeof window !== 'undefined' && window.innerWidth < 960;
  }

  private syncExpandedItems(): void {
    const currentUrl = this.router.url;
    const expandedItems = new Set<number>();

    const markExpanded = (items: MenuItem[]): boolean => {
      return items.some((item) => {
        const hasActiveChild = item.children?.length ? markExpanded(item.children) : false;
        const isActive = this.isRouteActive(currentUrl, item.route, item.exact);

        if (hasActiveChild) {
          expandedItems.add(item.id);
        }

        if (isActive && item.children?.length) {
          expandedItems.add(item.id);
        }

        return isActive || hasActiveChild;
      });
    };

    markExpanded(this.menuItems);
    this.expandedItems = expandedItems;
  }

  private isRouteActive(currentUrl: string, route: string, exact?: boolean): boolean {
    if (!route) {
      return false;
    }

    if (exact) {
      return currentUrl === route;
    }

    return currentUrl === route || currentUrl.startsWith(`${route}/`);
  }
}
