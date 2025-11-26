export interface NavItem {
  label: string;
  icon?: string;
  route: string;
  exact?: boolean;
  roles?: string[];
  permissions?: string[];
}

export const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
  {
    label: 'Usuarios',
    route: '/users',
    icon: 'group',
    roles: ['Admin'],
    permissions: ['CanManageUsers']
  },
  { label: 'Alias', route: '/aliases', icon: 'key', permissions: ['CanReadAliases'] },
  { label: 'Ciclos ACH', route: '/ach-cycles', icon: 'schedule', permissions: ['CanReadAch'] },
  { label: 'Catálogos', route: '/catalogs', icon: 'inventory', permissions: ['CanReadCatalogs'] },
  {
    label: 'Transacciones',
    route: '/transactions',
    icon: 'swap_horiz',
    roles: ['Admin', 'ACH.Operator'],
    permissions: ['CanManageAch', 'CanReadAch']
  },
  {
    label: 'Crear transacción',
    route: '/transactions/create',
    icon: 'swap_horiz',
    exact: true,
    roles: ['Admin', 'ACH.Operator'],
    permissions: ['CanManageAch', 'CanReadAch']
  }
];
