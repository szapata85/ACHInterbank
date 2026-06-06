import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { ColDef, GridApi, ICellRendererParams } from 'ag-grid-community';
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

type NavigationRow = NavigationMenuItem & {
  label: string;
  status: string;
  rolesText: string;
  permissionsText: string;
};

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
  private readonly cdr = inject(ChangeDetectorRef);

  menuItems: NavigationMenuItem[] = [];
  flatItems: FlatMenuItem[] = [];
  roles: RoleSummary[] = [];
  permissions: Permission[] = [];
  private roleLookup = new Map<string, string>();
  private permissionLookup = new Map<string, string>();
  private gridApi?: GridApi;
  loading = false;
  saving = false;
  hasLoaded = false;
  loadError: string | null = null;
  deletingId: number | null = null;
  iconMenuOpen = false;

  @ViewChild('iconSelectRoot', { static: false }) iconSelectRoot?: ElementRef<HTMLElement>;

  readonly gridContext = { component: this };

  readonly defaultColDef: ColDef = {
    resizable: true,
    sortable: true,
    filter: true,
    flex: 1,
    minWidth: 140
  };

  readonly columnDefs: ColDef[] = [
    { field: 'label', headerName: 'Etiqueta', flex: 1.2 },
    { field: 'route', headerName: 'Ruta' },
    { field: 'order', headerName: 'Orden', type: 'numericColumn', cellClass: 'ag-right-aligned-cell', width: 110 },
    { field: 'status', headerName: 'Estado', width: 120 },
    { field: 'rolesText', headerName: 'Roles', flex: 1.1 },
    { field: 'permissionsText', headerName: 'Permisos', flex: 1.2 },
    {
      headerName: 'Acciones',
      colId: 'actions',
      cellRenderer: (params) => this.renderActions(params),
      width: 190,
      maxWidth: 220,
      suppressMovable: true,
      filter: false,
      sortable: false
    }
  ];

  readonly iconOptions: string[] = [
    '',
    'dashboard',
    'home',
    'settings',
    'group',
    'manage_accounts',
    'list',
    'menu',
    'credit_card',
    'receipt_long',
    'account_balance',
    'payments',
    'sync',
    'lock',
    'visibility',
    'assignment',
    'folder',
    'upload',
    'download',
    'analytics',
    'support',
    'help'
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
    this.hasLoaded = false;
    this.loadError = null;
    this.cdr.markForCheck();
    this.navigationService.getMenuItems().subscribe({
      next: (items) => {
        this.menuItems = Array.isArray(items) ? items : [];
        this.flatItems = this.flatten(this.menuItems);
        this.gridApi?.setGridOption('rowData', this.tableData);
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadError = 'No fue posible cargar el menú de navegación';
        this.notificationService.error(this.loadError);
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      }
    });
  }

  loadRoles(): void {
    this.rolesApi.getRoles().subscribe((roles) => {
      this.roles = Array.isArray(roles) ? roles : [];
      this.roleLookup = new Map(this.roles.map((role) => [role.id, role.name]));
      this.cdr.markForCheck();
    });
  }

  loadPermissions(): void {
    this.permissionsService.getPermissions().subscribe((permissions) => {
      this.permissions = Array.isArray(permissions) ? permissions : [];
      this.permissionLookup = new Map(this.permissions.map((permission) => [permission.id, permission.name]));
      this.cdr.markForCheck();
    });
  }

  toggleIconMenu(event?: Event): void {
    event?.stopPropagation();
    this.iconMenuOpen = !this.iconMenuOpen;
  }

  selectIcon(icon: string, event?: Event): void {
    event?.preventDefault();
    this.form.get('icon')?.setValue(icon);
    this.iconMenuOpen = false;
  }

  @HostListener('document:click', ['$event'])
  closeIconMenu(event: Event): void {
    if (!this.iconMenuOpen) {
      return;
    }

    const target = event.target as Node;
    if (this.iconSelectRoot?.nativeElement.contains(target)) {
      return;
    }

    this.iconMenuOpen = false;
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

  onGridReady(api: GridApi): void {
    this.gridApi = api;
    this.gridApi.setGridOption('rowData', this.tableData);
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
        this.cdr.markForCheck();
      },
      error: () => {
        this.notificationService.error('No fue posible guardar el elemento del menú');
        this.saving = false;
        this.cdr.markForCheck();
      }
    });
  }

  confirmDelete(item: NavigationMenuItem): void {
    if (!confirm(`¿Eliminar "${item.label}"?`)) {
      return;
    }

    this.deletingId = item.id;
    this.refreshActionCells();
    this.navigationService.deleteMenuItem(item.id).subscribe({
      next: () => {
        this.notificationService.success('Elemento eliminado');
        this.deletingId = null;
        this.refreshActionCells();
        this.loadMenuItems();
        this.cdr.markForCheck();
      },
      error: () => {
        this.notificationService.error('No fue posible eliminar el elemento');
        this.deletingId = null;
        this.refreshActionCells();
        this.cdr.markForCheck();
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

  get tableData(): NavigationRow[] {
    return this.flatItems.map(({ item, depth }) => ({
      ...item,
      label: `${depth ? '— '.repeat(depth) : ''}${item.label}`,
      status: item.isActive ? 'Activo' : 'Inactivo',
      rolesText: this.getRolesText(item),
      permissionsText: this.getPermissionsText(item)
    }));
  }

  private renderActions(params: ICellRendererParams): HTMLElement {
    const component = params.context?.component as NavigationMenuComponent;
    const row = params.data as NavigationRow | undefined;
    const container = document.createElement('div');
    container.classList.add('actions');

    if (!row) {
      return container;
    }

    const editButton = document.createElement('button');
    editButton.type = 'button';
    editButton.textContent = 'Editar';
    editButton.addEventListener('click', () => component.editItem(row));

    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.textContent = component.deletingId === row.id ? 'Eliminando...' : 'Eliminar';
    deleteButton.classList.add('danger');
    deleteButton.disabled = component.deletingId === row.id;
    deleteButton.addEventListener('click', () => component.confirmDelete(row));

    container.append(editButton, deleteButton);
    return container;
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

  private refreshActionCells(): void {
    this.gridApi?.refreshCells({ columns: ['actions'], force: true });
  }
}
