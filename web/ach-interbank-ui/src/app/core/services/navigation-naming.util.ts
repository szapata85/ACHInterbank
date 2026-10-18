import type { MenuItem } from '../models/menu.model';

export interface NavigationBreadcrumb {
  label: string;
  url?: string;
}

export interface RouteNamingEntry {
  url: string;
  breadcrumb?: string;
  title?: string;
}

export interface NavigationNaming {
  pageTitle: string;
  breadcrumbs: NavigationBreadcrumb[];
  menuBacked: boolean;
}

interface IndexedMenuItem {
  item: MenuItem;
  ancestors: MenuItem[];
  normalizedRoute: string;
}

export function resolveNavigationNaming(
  menuItems: MenuItem[],
  currentUrl: string,
  routeEntries: RouteNamingEntry[]
): NavigationNaming {
  const currentPath = normalizePath(currentUrl);
  const indexed = indexMenu(menuItems);
  const exact = indexed.find((entry) => entry.normalizedRoute === currentPath);
  const nearestAncestor = exact ?? findNearestMenuAncestor(indexed, currentPath);
  const deepestContextTitle = [...routeEntries]
    .reverse()
    .find((entry) => Boolean(entry.title?.trim()))
    ?.title?.trim();

  if (!nearestAncestor) {
    return {
      pageTitle: deepestContextTitle || 'Inicio',
      breadcrumbs: routeEntries
        .filter((entry) => Boolean(entry.breadcrumb?.trim()))
        .map((entry) => ({
          label: entry.breadcrumb!.trim(),
          url: entry.url || undefined
        })),
      menuBacked: false
    };
  }

  const canonicalTrail = [...nearestAncestor.ancestors, nearestAncestor.item];
  const activatedUrls = new Set(routeEntries.map((entry) => normalizePath(entry.url)));
  const canonicalRoutes = new Set(canonicalTrail.map((item) => normalizePath(item.route)));
  const breadcrumbs: NavigationBreadcrumb[] = canonicalTrail.map((item) => ({
    label: item.label,
    url: activatedUrls.has(normalizePath(item.route)) ? item.route : undefined
  }));

  if (!exact) {
    for (const entry of routeEntries) {
      const label = entry.breadcrumb?.trim();
      if (!label) {
        continue;
      }

      const entryPath = normalizePath(entry.url);
      if (canonicalRoutes.has(entryPath)) {
        continue;
      }

      breadcrumbs.push({ label, url: entry.url || undefined });
    }
  }

  return {
    pageTitle: exact ? exact.item.label : (deepestContextTitle || nearestAncestor.item.label),
    breadcrumbs,
    menuBacked: Boolean(exact)
  };
}

export function canonicalLabelForRoute(
  menuItems: MenuItem[],
  route: string | undefined,
  fallback: string
): string {
  if (!route) {
    return fallback;
  }

  const normalizedRoute = normalizePath(route);
  return indexMenu(menuItems)
    .find((entry) => entry.normalizedRoute === normalizedRoute)
    ?.item.label ?? fallback;
}

function findNearestMenuAncestor(
  indexed: IndexedMenuItem[],
  currentPath: string
): IndexedMenuItem | undefined {
  return indexed
    .filter((entry) => isPathAncestor(entry.normalizedRoute, currentPath))
    .sort((a, b) => b.normalizedRoute.length - a.normalizedRoute.length)[0];
}

function isPathAncestor(candidate: string, currentPath: string): boolean {
  if (candidate === '/' || candidate === currentPath) {
    return candidate === currentPath;
  }

  return currentPath.startsWith(`${candidate}/`);
}

function indexMenu(items: MenuItem[], ancestors: MenuItem[] = []): IndexedMenuItem[] {
  const indexed: IndexedMenuItem[] = [];

  for (const item of items) {
    indexed.push({
      item,
      ancestors,
      normalizedRoute: normalizePath(item.route)
    });

    if (item.children?.length) {
      indexed.push(...indexMenu(item.children, [...ancestors, item]));
    }
  }

  return indexed;
}

function normalizePath(route: string): string {
  const withoutFragment = (route || '/').split('#', 1)[0];
  const withoutQuery = withoutFragment.split('?', 1)[0].trim();
  const withLeadingSlash = withoutQuery.startsWith('/') ? withoutQuery : `/${withoutQuery}`;
  const collapsed = withLeadingSlash.replace(/\/{2,}/g, '/');

  if (collapsed.length > 1 && collapsed.endsWith('/')) {
    return collapsed.slice(0, -1);
  }

  return collapsed || '/';
}
