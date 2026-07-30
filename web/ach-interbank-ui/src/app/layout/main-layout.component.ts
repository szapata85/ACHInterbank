import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostBinding,
  OnInit,
  inject
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  ActivatedRoute,
  IsActiveMatchOptions,
  NavigationEnd,
  Router,
  RouterModule
} from '@angular/router';
import { catchError, filter, of } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { BrandingService } from '../core/services/branding.service';
import { NavigationService } from '../core/services/navigation.service';
import { MenuItem } from '../core/models/menu.model';
import { SharedModule } from '../shared/shared.module';

interface Breadcrumb {
  label: string;
  url: string;
}

const MOBILE_NAVIGATION_QUERY = '(max-width: 959.98px)';

const EXACT_ROUTE_MATCH: IsActiveMatchOptions = {
  paths: 'exact',
  queryParams: 'ignored',
  matrixParams: 'ignored',
  fragment: 'ignored'
};

const SUBSET_ROUTE_MATCH: IsActiveMatchOptions = {
  paths: 'subset',
  queryParams: 'ignored',
  matrixParams: 'ignored',
  fragment: 'ignored'
};

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    SharedModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    MatTooltipModule
  ]
})
export class MainLayoutComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly authService = inject(AuthService);
  private readonly navigationService = inject(NavigationService);
  private readonly brandingService = inject(BrandingService);

  @HostBinding('style.--private-bg')
  public privateBackground: string | null =
    this.brandingService.getBrandingSnapshot().privateBackground ?? null;

  @HostBinding('style.--sidebar-bg')
  public sidebarBackground: string | null =
    this.brandingService.getBrandingSnapshot().sidebarBackground ?? null;

  readonly user$ = this.authService.user$;
  readonly branding$ = this.brandingService.branding$;
  readonly exactRouteMatch = EXACT_ROUTE_MATCH;
  readonly subsetRouteMatch = SUBSET_ROUTE_MATCH;

  menuItems: MenuItem[] = [];
  expandedItems = new Set<number>();
  activeItemIds = new Set<number>();
  menuLoadError: string | null = null;

  breadcrumbs: Breadcrumb[] = [];
  pageTitle = 'Inicio';
  isMobile = false;
  isMenuOpen = false;
  isSidebarCollapsed = false;

  get drawerOpened(): boolean {
    return this.isMobile ? this.isMenuOpen : true;
  }

  get menuToggleLabel(): string {
    if (this.isMobile) {
      return this.isMenuOpen ? 'Cerrar menú principal' : 'Abrir menú principal';
    }

    return this.isSidebarCollapsed ? 'Expandir menú principal' : 'Contraer menú principal';
  }

  get isMenuExpanded(): boolean {
    return this.isMobile ? this.isMenuOpen : !this.isSidebarCollapsed;
  }

  ngOnInit(): void {
    this.breakpointObserver
      .observe(MOBILE_NAVIGATION_QUERY)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ matches }) => {
        this.isMobile = matches;
        this.isMenuOpen = false;

        if (matches) {
          this.isSidebarCollapsed = false;
        }

        this.cdr.markForCheck();
      });

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.buildBreadcrumbs();
        this.handleNavigation();
        this.cdr.markForCheck();
      });

    this.buildBreadcrumbs();

    this.navigationService
      .getMenu()
      .pipe(
        catchError(() => {
          this.menuLoadError = 'No fue posible cargar el menú principal.';
          this.cdr.markForCheck();
          return of([] as MenuItem[]);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((items) => {
        this.menuItems = items;
        this.syncActiveNavigation();
        this.cdr.markForCheck();
      });

    this.brandingService.branding$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((branding) => {
        this.privateBackground = branding.privateBackground ?? null;
        this.sidebarBackground = branding.sidebarBackground ?? null;
        this.cdr.markForCheck();
      });
  }

  logout(): void {
    this.authService.logout();
  }

  toggleMenu(): void {
    if (this.isMobile) {
      this.isMenuOpen = !this.isMenuOpen;
    } else {
      this.isSidebarCollapsed = !this.isSidebarCollapsed;
    }

    this.cdr.markForCheck();
  }

  closeMenu(): void {
    if (!this.isMobile || !this.isMenuOpen) {
      return;
    }

    this.isMenuOpen = false;
    this.cdr.markForCheck();
  }

  onDrawerOpenedChange(opened: boolean): void {
    if (this.isMobile && this.isMenuOpen !== opened) {
      this.isMenuOpen = opened;
      this.cdr.markForCheck();
    }
  }

  onDrawerClosed(): void {
    if (this.isMobile && this.isMenuOpen) {
      this.isMenuOpen = false;
      this.cdr.markForCheck();
    }
  }

  toggleSubmenu(item: MenuItem): void {
    if (!this.isMobile && this.isSidebarCollapsed) {
      this.isSidebarCollapsed = false;
    }

    if (this.expandedItems.has(item.id)) {
      this.expandedItems.delete(item.id);
    } else {
      this.expandedItems.add(item.id);
    }

    this.cdr.markForCheck();
  }

  isItemExpanded(item: MenuItem): boolean {
    return this.expandedItems.has(item.id);
  }

  onNavItemSelected(): void {
    if (this.isMobile) {
      this.closeMenu();
    }
  }

  trackByMenuItem(_index: number, item: MenuItem): number {
    return item.id;
  }

  private buildBreadcrumbs(): void {
    const breadcrumbs: Breadcrumb[] = [];
    let currentRoute: ActivatedRoute | null = this.route.root;
    let url = '';
    this.pageTitle = 'Inicio';

    while (currentRoute) {
      const routeSnapshot = currentRoute.snapshot;
      const routeConfig = routeSnapshot.routeConfig;

      if (routeConfig?.path) {
        url = routeConfig.path.startsWith('/') ? routeConfig.path : `${url}/${routeConfig.path}`;
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
    if (this.isMobile) {
      this.closeMenu();
    }

    this.syncActiveNavigation();
  }

  private syncActiveNavigation(): void {
    const validExpandedIds = new Set<number>();

    const retainValidExpandedItems = (items: MenuItem[]): void => {
      for (const item of items) {
        if (this.expandedItems.has(item.id)) {
          validExpandedIds.add(item.id);
        }

        if (item.children?.length) {
          retainValidExpandedItems(item.children);
        }
      }
    };

    retainValidExpandedItems(this.menuItems);

    const activeIds = new Set<number>();

    const markActiveItems = (items: MenuItem[]): boolean => {
      let branchIsActive = false;

      for (const item of items) {
        const hasActiveChild = item.children?.length ? markActiveItems(item.children) : false;
        const isActive = this.isRouteActive(item);

        if (isActive || hasActiveChild) {
          activeIds.add(item.id);
          branchIsActive = true;
        }

        if (item.children?.length && (isActive || hasActiveChild)) {
          validExpandedIds.add(item.id);
        }
      }

      return branchIsActive;
    };

    markActiveItems(this.menuItems);
    this.activeItemIds = activeIds;
    this.expandedItems = validExpandedIds;
  }

  private isRouteActive(item: MenuItem): boolean {
    if (!item.route) {
      return false;
    }

    try {
      return this.router.isActive(
        this.router.parseUrl(item.route),
        item.exact ? EXACT_ROUTE_MATCH : SUBSET_ROUTE_MATCH
      );
    } catch {
      return false;
    }
  }
}
