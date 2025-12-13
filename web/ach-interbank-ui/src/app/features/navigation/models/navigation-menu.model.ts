export interface NavigationMenuItem {
  id: number;
  parentId?: number | null;
  label: string;
  route: string;
  icon?: string | null;
  order: number;
  exact: boolean;
  isActive: boolean;
  roleIds: string[];
  permissionIds: string[];
  children?: NavigationMenuItem[];
}

export interface SaveNavigationMenuItem {
  label: string;
  route: string;
  icon?: string | null;
  order: number;
  exact: boolean;
  isActive: boolean;
  parentId?: number | null;
  roleIds: string[];
  permissionIds: string[];
}
