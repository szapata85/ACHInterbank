export interface MenuItem {
  id: number;
  label: string;
  route: string;
  icon?: string;
  exact?: boolean;
  order?: number;
  children?: MenuItem[];
}
