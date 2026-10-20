import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  Renderer2,
  ViewChild,
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
import { Title } from '@angular/platform-browser';
import { AuthService } from '../core/services/auth.service';
import { BrandingService } from '../core/services/branding.service';
import { NavigationNamingService, NavigationBreadcrumb } from '../core/services/navigation-naming.service';
import { MenuRouteSurfaceService } from '../core/services/menu-route-surface.service';
import { MenuItem } from '../core/models/menu.model';
import { SharedModule } from '../shared/shared.module';

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
  private readonly renderer = inject(Renderer2);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly authService = inject(AuthService);
  private readonly navigationNaming = inject(NavigationNamingService);
  private readonly documentTitle = inject(Title);
  private readonly menuRouteSurface = inject(MenuRouteSurfaceService);
  private readonly brandingService = inject(BrandingService);

  @ViewChild('navigationToggle', { read: ElementRef })
  private navigationToggle?: ElementRef<HTMLButtonElement>;

  readonly user$ = this.authService.user$;
  readonly branding$ = this.brandingService.branding$;
  readonly productName = this.brandingService.productName;
  readonly exactRouteMatch = EXACT_ROUTE_MATCH;
  readonly subsetRouteMatch = SUBSET_ROUTE_MATCH;

  menuItems: MenuItem[] = [];
  expandedItems = new Set<number>();
  activeItemIds = new Set<number>();
  navigableParentIds = new Set<number>();
  menuLoadError: string | null = null;

  breadcrumbs: NavigationBreadcrumb[] = [];
  pageTitle = 'Inicio';
  isMobileViewport = false;
  isMobileDrawerOpen = false;
  isDesktopCompact = false;

  get drawerOpened(): boolean {
    return this.isMobileViewport ? this.isMobileDrawerOpen : true;
  }

  get navigationToggleLabel(): string {
    if (this.isMobileViewport) {
      return this.isMobileDrawerOpen ? 'Cerrar navegación' : 'Abrir navegación';
    }

    return this.isDesktopCompact ? 'Expandir navegación' : 'Contraer navegación';
  }

  get isNavigationExpanded(): boolean {
    return this.isMobileViewport ? this.isMobileDrawerOpen : !this.isDesktopCompact;
  }

  ngOnInit(): void {
    const removeEscapeListener = this.renderer.listen(
      'document',
      'keydown',
      (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
          this.onEscapeKey(event);
        }
      },
      { capture: true }
    );
    this.destroyRef.onDestroy(removeEscapeListener);

    this.breakpointObserver
      .observe(MOBILE_NAVIGATION_QUERY)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ matches }) => {
        if (this.isMobileViewport !== matches) {
          this.isMobileDrawerOpen = false;
        }

        this.isMobileViewport = matches;
        this.cdr.markForCheck();
      });

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.syncRouteNaming();
        this.handleNavigation();
        this.cdr.markForCheck();
      });

    this.syncRouteNaming();

    this.navigationNaming
      .menu$
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
        this.syncRouteNaming();
        void this.syncParentNavigability(items);
        this.syncActiveNavigation();
        this.cdr.markForCheck();
      });

  }

  logout(): void {
    this.authService.logout();
  }

  toggleNavigation(): void {
    if (this.isMobileViewport) {
      this.isMobileDrawerOpen = !this.isMobileDrawerOpen;
    } else {
      this.isDesktopCompact = !this.isDesktopCompact;
    }

    this.cdr.markForCheck();
  }

  closeMobileNavigation(): void {
    if (!this.isMobileViewport || !this.isMobileDrawerOpen) {
      return;
    }

    this.isMobileDrawerOpen = false;
    this.cdr.markForCheck();
  }

  onEscapeKey(event: KeyboardEvent): void {
    if (!this.isMobileViewport || !this.isMobileDrawerOpen) {
      return;
    }

    this.isMobileDrawerOpen = false;
    this.cdr.markForCheck();
    queueMicrotask(() => {
      const toggle = this.navigationToggle?.nativeElement;
      if (toggle) {
        toggle.focus();
      }
    });
  }

  onDrawerOpenedChange(opened: boolean): void {
    if (this.isMobileViewport && this.isMobileDrawerOpen !== opened) {
      this.isMobileDrawerOpen = opened;
      this.cdr.markForCheck();
    }
  }

  onDrawerClosed(): void {
    if (this.isMobileViewport && this.isMobileDrawerOpen) {
      this.isMobileDrawerOpen = false;
      this.cdr.markForCheck();
    }
  }

  toggleSubmenu(item: MenuItem): void {
    if (!this.isMobileViewport && this.isDesktopCompact) {
      this.isDesktopCompact = false;
      this.expandedItems.add(item.id);
      this.cdr.markForCheck();
      return;
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

  isNavigableParent(item: MenuItem): boolean {
    return Boolean(item.children?.length) && this.navigableParentIds.has(item.id);
  }

  onNavItemSelected(): void {
    if (this.isMobileViewport) {
      this.closeMobileNavigation();
    }
  }

  trackByMenuItem(_index: number, item: MenuItem): number {
    return item.id;
  }

  private parentNavigabilityGeneration = 0;

  private async syncParentNavigability(items: MenuItem[]): Promise<void> {
    const generation = ++this.parentNavigabilityGeneration;
    const parents: MenuItem[] = [];

    const collectParents = (nodes: MenuItem[]): void => {
      for (const item of nodes) {
        if (item.children?.length) {
          parents.push(item);
          collectParents(item.children);
        }
      }
    };

    collectParents(items);
    this.navigableParentIds = new Set<number>();

    const classifications = await Promise.all(
      parents.map(async (item) => ({
        id: item.id,
        functional: await this.menuRouteSurface.hasFunctionalSurface(item.route)
      }))
    );

    if (this.destroyRef.destroyed || generation !== this.parentNavigabilityGeneration) {
      return;
    }

    this.navigableParentIds = new Set(
      classifications
        .filter((classification) => classification.functional)
        .map((classification) => classification.id)
    );
    this.cdr.markForCheck();
  }

  private syncRouteNaming(): void {
    const naming = this.navigationNaming.resolve(
      this.menuItems,
      this.router.url,
      this.route.root.snapshot
    );

    this.pageTitle = naming.pageTitle;
    this.breadcrumbs = naming.breadcrumbs;
    this.documentTitle.setTitle(`${this.productName} - ${this.pageTitle}`);
  }

  private handleNavigation(): void {
    if (this.isMobileViewport) {
      this.closeMobileNavigation();
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
