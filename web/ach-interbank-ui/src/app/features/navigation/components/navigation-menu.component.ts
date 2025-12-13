import { ChangeDetectionStrategy, Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { TableColumn } from '../../../shared/components/table.component';
import { NavigationMenuItem, SaveNavigationMenuItem } from '../models/navigation-menu.model';
import { NavigationMenuService } from '../services/navigation-menu.service';
import { RolesApiService } from '../../admin/services/users-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RoleSummary } from '../../admin/models/user.model';
import { Permission } from '../../../core/models/permission.model';
import { PermissionsService } from '../../../core/services/permissions.service';
import { NgIf, NgFor } from '@angular/common';

interface FlatMenuItem {
  item: NavigationMenuItem;
  depth: number;
}

@Component({
  selector: 'app-navigation-menu',
  standalone: true,
  imports: [SharedModule, NgIf, NgFor],
  templateUrl: './navigation-menu.component.html',
  styleUrls: ['./navigation-menu.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavigationMenuComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly navigationService = inject(NavigationMenuService);
  private readonly notificationService = inject(NotificationService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly permissionsService = inject(PermissionsService);

  @ViewChild('rowActions', { static: true }) rowActionsTemplate!: TemplateRef<any>;

  menuItems: NavigationMenuItem[] = [];
  flatItems: FlatMenuItem[] = [];
  roles: RoleSummary[] = [];
  permissions: Permission[] = [];
  private roleLookup = new Map<string, string>();
  private permissionLookup = new Map<string, string>();
  loading = false;
  saving = false;
  deletingId: number | null = null;

  readonly columns: TableColumn[] = [
    { key: 'label', label: 'Etiqueta' },
    { key: 'route', label: 'Ruta' },
    { key: 'order', label: 'Orden', align: 'end' },
    { key: 'status', label: 'Estado' },
    { key: 'rolesText', label: 'Roles' },
    { key: 'permissionsText', label: 'Permisos' }
  ];

  readonly form = this.fb.group({
    id: [null as number | null],
    label: ['', Validators.required],
    route: ['', Validators.required],
    icon: [''],
    order: [1, Validators.required],
    exact: [false],
    isActive: [true],
    parentId: [null as number | null],
    roleIds: [[] as string[]],
    permissionIds: [[] as string[]]
  });

  ngOnInit(): void {
    this.loadRoles();
    this.loadPermissions();
    this.loadMenuItems();
  }

  loadMenuItems(): void {
    this.loading = true;
    this.navigationService.getMenuItems().subscribe({
      next: (items) => {
        this.menuItems = items;
        this.flatItems = this.flatten(items);
        this.loading = false;
      },
      error: () => {
        this.notificationService.error('No fue posible cargar el menú de navegación');
        this.loading = false;
      }
    });
  }

  loadRoles(): void {
    this.rolesApi.getRoles().subscribe((roles) => {
      this.roles = roles;
      this.roleLookup = new Map(roles.map((role) => [role.id, role.name]));
    });
  }

  loadPermissions(): void {
    this.permissionsService.getPermissions().subscribe((permissions) => {
      this.permissions = permissions;
      this.permissionLookup = new Map(permissions.map((permission) => [permission.id, permission.name]));
    });
  }

  startCreate(): void {
    this.form.reset({
      id: null,
      label: '',
      route: '',
      icon: '',
      order: (this.flatItems.length || 0) + 1,
      exact: false,
      isActive: true,
      parentId: null,
      roleIds: [],
      permissionIds: []
    });
  }

  editItem(item: NavigationMenuItem): void {
    this.form.patchValue({
      id: item.id,
      label: item.label,
      route: item.route,
      icon: item.icon,
      order: item.order,
      exact: item.exact,
      isActive: item.isActive,
      parentId: item.parentId ?? null,
      roleIds: item.roleIds,
      permissionIds: item.permissionIds
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { id, ...rest } = this.form.value;
    const payload: SaveNavigationMenuItem = {
      label: rest.label ?? '',
      route: rest.route ?? '',
      icon: rest.icon ?? '',
      order: Number(rest.order ?? 0),
      exact: !!rest.exact,
      isActive: !!rest.isActive,
      parentId: rest.parentId ?? null,
      roleIds: rest.roleIds ?? [],
      permissionIds: rest.permissionIds ?? []
    };

    this.saving = true;
    const request$ = id ? this.navigationService.updateMenuItem(id, payload) : this.navigationService.createMenuItem(payload);

    request$.subscribe({
      next: () => {
        this.notificationService.success('Menú actualizado');
        this.saving = false;
        this.startCreate();
        this.loadMenuItems();
      },
      error: () => {
        this.notificationService.error('No fue posible guardar el elemento del menú');
        this.saving = false;
      }
    });
  }

  confirmDelete(item: NavigationMenuItem): void {
    if (!confirm(`¿Eliminar "${item.label}"?`)) {
      return;
    }

    this.deletingId = item.id;
    this.navigationService.deleteMenuItem(item.id).subscribe({
      next: () => {
        this.notificationService.success('Elemento eliminado');
        this.deletingId = null;
        this.loadMenuItems();
      },
      error: () => {
        this.notificationService.error('No fue posible eliminar el elemento');
        this.deletingId = null;
      }
    });
  }

  trackById(_: number, item: FlatMenuItem): number {
    return item.item.id;
  }

  get parentOptions(): FlatMenuItem[] {
    const currentId = this.form.value.id;
    return this.flatItems.filter((option) => option.item.id !== currentId);
  }

  getRolesText(item: NavigationMenuItem): string {
    if (!item.roleIds?.length) {
      return 'Todos';
    }
    const names = item.roleIds.map((id) => this.roleLookup.get(id) ?? id);
    return names.join(', ');
  }

  getPermissionsText(item: NavigationMenuItem): string {
    if (!item.permissionIds?.length) {
      return 'Ninguno';
    }
    const names = item.permissionIds.map((id) => this.permissionLookup.get(id) ?? id);
    return names.join(', ');
  }

  get tableData(): any[] {
    return this.flatItems.map(({ item, depth }) => ({
      ...item,
      label: `${depth ? '— '.repeat(depth) : ''}${item.label}`,
      status: item.isActive ? 'Activo' : 'Inactivo',
      rolesText: this.getRolesText(item),
      permissionsText: this.getPermissionsText(item)
    }));
  }

  private flatten(items: NavigationMenuItem[], depth = 0): FlatMenuItem[] {
    const result: FlatMenuItem[] = [];
    for (const item of items) {
      result.push({ item, depth });
      if (item.children?.length) {
        result.push(...this.flatten(item.children, depth + 1));
      }
    }
    return result;
  }
}
