import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Inject,
  Injector,
  OnDestroy,
  OnInit,
  ViewChild,
  afterNextRender,
  inject
} from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTree, MatTreeModule } from '@angular/material/tree';
import { finalize, filter, forkJoin, map, Subject, switchMap, takeUntil } from 'rxjs';
import { Permission } from '../../../core/models/permission.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { SharedModule } from '../../../shared/shared.module';
import { RoleSummary } from '../../admin/models/user.model';
import { RolesApiService } from '../../admin/services/users-api.service';
import { NavigationMenuItem, SaveNavigationMenuItem } from '../models/navigation-menu.model';
import { NavigationMenuService } from '../services/navigation-menu.service';

interface FlatMenuItem {
  item: NavigationMenuItem;
  depth: number;
}

type EditorMode = 'none' | 'create' | 'edit';
type StatusFilter = 'all' | 'active' | 'inactive';
type LevelFilter = 'all' | 'root' | 'child';

interface DeleteDialogData {
  item: NavigationMenuItem;
}

@Component({
  selector: 'app-navigation-menu-delete-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Eliminar opción</h2>
    <mat-dialog-content>
      <div class="navigation-delete-dialog__warning" aria-hidden="true">
        <span aria-hidden="true">!</span>
      </div>
      <p>
        Vas a eliminar <strong>{{ data.item.label }}</strong>. Esta acción no se puede deshacer.
      </p>
      <p *ngIf="data.item.children?.length" class="navigation-delete-dialog__blocked">
        La opción tiene hijos. Reasígnalos o elimínalos antes de continuar.
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close(false)">Cancelar</button>
      <button
        mat-flat-button
        type="button"
        class="navigation-delete-dialog__confirm"
        [disabled]="!!data.item.children?.length"
        (click)="dialogRef.close(true)"
      >
        Eliminar opción
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .navigation-delete-dialog__warning {
      align-items: center;
      background: #fff3e0;
      border-radius: 50%;
      color: #ad4f00;
      display: flex;
      height: 48px;
      justify-content: center;
      margin-bottom: 16px;
      width: 48px;
    }

    .navigation-delete-dialog__blocked {
      color: #a61b1b;
      font-weight: 600;
    }

    .navigation-delete-dialog__confirm {
      background: #b42318;
      color: #fff;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavigationMenuDeleteDialogComponent {
  constructor(
    @Inject(MAT_DIALOG_DATA) readonly data: DeleteDialogData,
    readonly dialogRef: MatDialogRef<NavigationMenuDeleteDialogComponent>
  ) {}
}

@Component({
  selector: 'app-navigation-menu',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatMenuModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTooltipModule,
    MatTreeModule
  ],
  templateUrl: './navigation-menu.component.html',
  styleUrls: ['./navigation-menu.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavigationMenuComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly navigationService = inject(NavigationMenuService);
  private readonly notificationService = inject(NotificationService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly permissionsService = inject(PermissionsService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly injector = inject(Injector);
  private readonly destroy$ = new Subject<void>();

  @ViewChild(MatTree) private tree?: MatTree<NavigationMenuItem>;

  menuItems: NavigationMenuItem[] = [];
  filteredMenuItems: NavigationMenuItem[] = [];
  flatItems: FlatMenuItem[] = [];
  roles: RoleSummary[] = [];
  permissions: Permission[] = [];
  selectedItem: NavigationMenuItem | null = null;
  mode: EditorMode = 'none';
  loading = false;
  saving = false;
  hasLoaded = false;
  loadError: string | null = null;
  deletingId: number | null = null;
  quickActionId: number | null = null;
  filteredCount = 0;

  private roleLookup = new Map<string, string>();
  private permissionLookup = new Map<string, string>();
  private initialPayload: SaveNavigationMenuItem | null = null;

  readonly canManage = this.authService.hasRole('Admin') && this.authService.hasPermission('CanManageUsers');
  readonly childrenAccessor = (node: NavigationMenuItem): NavigationMenuItem[] => node.children ?? [];
  readonly expansionKey = (node: NavigationMenuItem): number => node.id;

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

  readonly filtersForm = this.fb.nonNullable.group({
    search: [''],
    status: ['all' as StatusFilter],
    level: ['all' as LevelFilter]
  });

  readonly form = this.fb.group({
    id: [null as number | null],
    label: ['', [Validators.required, Validators.maxLength(200)]],
    route: [
      '',
      [
        Validators.required,
        Validators.maxLength(400),
        Validators.pattern(/^\/(?:[A-Za-z0-9][A-Za-z0-9\-_/.:?=&%]*)?$/),
        (control: AbstractControl): ValidationErrors | null => this.duplicateRouteValidator(control)
      ]
    ],
    icon: [
      '',
      [
        Validators.maxLength(100),
        Validators.pattern(/^[a-z0-9_]*$/)
      ]
    ],
    order: [
      1,
      [
        Validators.required,
        Validators.min(0),
        (control: AbstractControl): ValidationErrors | null => (
          Number.isInteger(Number(control.value)) ? null : { integer: true }
        )
      ]
    ],
    exact: [false],
    isActive: [true],
    parentId: [
      null as number | null,
      [(control: AbstractControl): ValidationErrors | null => this.parentValidator(control)]
    ],
    roleIds: [[] as string[]],
    permissionIds: [[] as string[]]
  });

  get isEditing(): boolean {
    return this.mode === 'edit';
  }

  get isCreating(): boolean {
    return this.mode === 'create';
  }

  get formTitle(): string {
    if (this.isEditing) {
      return `Editar “${this.selectedItem?.label ?? 'opción'}”`;
    }

    if (this.isCreating && this.form.value.parentId) {
      return 'Nueva opción hija';
    }

    return 'Nueva opción';
  }

  get formSubtitle(): string {
    if (this.isEditing) {
      return 'Actualiza la ubicación, visibilidad y acceso de la opción seleccionada.';
    }

    if (this.isCreating && this.form.value.parentId) {
      return `Se creará dentro de “${this.parentLabel(this.form.value.parentId)}”.`;
    }

    return 'Define una opción raíz para la navegación de la SPA.';
  }

  get submitText(): string {
    if (this.saving) {
      return 'Guardando...';
    }

    return this.isEditing ? 'Guardar cambios' : 'Crear opción';
  }

  get totalCount(): number {
    return this.flatItems.length;
  }

  get activeCount(): number {
    return this.flatItems.filter(({ item }) => item.isActive).length;
  }

  get inactiveCount(): number {
    return this.totalCount - this.activeCount;
  }

  get hasActiveFilters(): boolean {
    const { search, status, level } = this.filtersForm.getRawValue();
    return !!search.trim() || status !== 'all' || level !== 'all';
  }

  get hasChanges(): boolean {
    if (this.mode === 'none' || !this.initialPayload) {
      return false;
    }

    return this.payloadKey(this.toPayload()) !== this.payloadKey(this.initialPayload);
  }

  get canSave(): boolean {
    return this.canManage
      && this.mode !== 'none'
      && this.form.valid
      && this.hasChanges
      && !this.saving
      && this.quickActionId === null
      && this.deletingId === null;
  }

  get parentOptions(): FlatMenuItem[] {
    const currentId = this.form.value.id;
    const forbidden = currentId ? this.getDescendantIds(currentId) : new Set<number>();
    if (currentId) {
      forbidden.add(currentId);
    }

    return this.flatItems.filter(({ item }) => !forbidden.has(item.id));
  }

  ngOnInit(): void {
    this.filtersForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.applyFilters());

    this.loadInitialData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadInitialData(): void {
    this.loading = true;
    this.hasLoaded = false;
    this.loadError = null;

    forkJoin({
      items: this.navigationService.getMenuItems(),
      roles: this.rolesApi.getRoles(),
      permissions: this.permissionsService.getPermissions()
    })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.hasLoaded = true;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: ({ items, roles, permissions }) => {
          this.roles = Array.isArray(roles) ? roles : [];
          this.permissions = Array.isArray(permissions) ? permissions : [];
          this.roleLookup = new Map(this.roles.map((role) => [role.id, role.name]));
          this.permissionLookup = new Map(this.permissions.map((permission) => [permission.id, permission.name]));
          this.applyLoadedItems(items);
        },
        error: (error: unknown) => {
          this.loadError = this.errorMessage(error, 'No fue posible cargar la administración del menú.');
          this.notificationService.error(this.loadError);
        }
      });
  }

  loadMenuItems(): void {
    if (this.loading) {
      return;
    }

    const selectedId = this.selectedItem?.id ?? null;
    const editingId = this.isEditing ? this.form.value.id ?? null : null;
    this.loading = true;
    this.loadError = null;

    this.navigationService.getMenuItems()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.hasLoaded = true;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.applyLoadedItems(items);
          const restored = selectedId ? this.findById(selectedId) : null;
          this.selectedItem = restored;
          if (editingId && restored?.id === editingId) {
            this.openEditor(restored);
          }
        },
        error: (error: unknown) => {
          this.loadError = this.errorMessage(error, 'No fue posible actualizar la jerarquía.');
          this.notificationService.error(this.loadError);
        }
      });
  }

  selectItem(item: NavigationMenuItem): void {
    this.selectedItem = this.findById(item.id) ?? item;
    this.cdr.markForCheck();
  }

  startCreate(parent: NavigationMenuItem | null = null): void {
    if (!this.ensureCanManage()) {
      return;
    }

    if (parent) {
      this.selectedItem = this.findById(parent.id) ?? parent;
    }

    this.mode = 'create';
    const payload = this.emptyPayload(parent?.id ?? null);
    this.resetForm(null, payload);
    this.initialPayload = this.clonePayload(payload);
    this.cdr.markForCheck();
  }

  editItem(item: NavigationMenuItem): void {
    if (!this.ensureCanManage()) {
      return;
    }

    const persisted = this.findById(item.id) ?? item;
    this.selectedItem = persisted;
    this.openEditor(persisted);
  }

  cancelEditing(): void {
    if (this.saving) {
      return;
    }

    if (this.initialPayload) {
      this.resetForm(this.isEditing ? this.selectedItem?.id ?? null : null, this.initialPayload);
    }
    this.mode = 'none';
    this.initialPayload = null;
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.saving || !this.canManage) {
      return;
    }

    this.form.updateValueAndValidity();
    if (this.form.invalid || !this.hasChanges || this.mode === 'none') {
      this.form.markAllAsTouched();
      return;
    }

    const id = this.form.value.id;
    const payload = this.toPayload();
    this.saving = true;
    this.form.disable({ emitEvent: false });

    const request$ = id
      ? this.navigationService.updateMenuItem(id, payload)
      : this.navigationService.createMenuItem(payload);

    request$
      .pipe(
        switchMap((saved) => this.navigationService.getMenuItems().pipe(map((items) => ({ saved, items })))),
        takeUntil(this.destroy$),
        finalize(() => {
          this.saving = false;
          this.form.enable({ emitEvent: false });
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: ({ saved, items }) => {
          this.applyLoadedItems(items);
          const persisted = this.findById(saved.id);
          if (persisted) {
            this.selectedItem = persisted;
            this.openEditor(persisted);
          }
          this.notificationService.success(id ? 'Cambios guardados.' : 'Opción creada.');
        },
        error: (error: unknown) => {
          this.notificationService.error(this.errorMessage(error, 'No fue posible guardar la opción.'));
        }
      });
  }

  toggleActive(item: NavigationMenuItem): void {
    if (!this.ensureCanManage() || this.quickActionId !== null || this.saving) {
      return;
    }

    this.quickActionId = item.id;
    const payload = this.itemPayload(item);
    payload.isActive = !item.isActive;

    this.navigationService.updateMenuItem(item.id, payload)
      .pipe(
        switchMap(() => this.navigationService.getMenuItems()),
        takeUntil(this.destroy$),
        finalize(() => {
          this.quickActionId = null;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.applyLoadedItems(items);
          this.selectedItem = this.findById(item.id);
          this.notificationService.success(payload.isActive ? 'Opción activada.' : 'Opción desactivada.');
        },
        error: (error: unknown) => {
          this.notificationService.error(this.errorMessage(error, 'No fue posible cambiar el estado.'));
        }
      });
  }

  confirmDelete(item: NavigationMenuItem): void {
    if (!this.ensureCanManage() || this.deletingId !== null || item.children?.length) {
      if (item.children?.length) {
        this.notificationService.error('La opción tiene hijos. Reasígnalos o elimínalos primero.');
      }
      return;
    }

    const dialogRef = this.dialog.open(NavigationMenuDeleteDialogComponent, {
      width: '440px',
      maxWidth: 'calc(100vw - 32px)',
      restoreFocus: true,
      autoFocus: 'dialog',
      data: { item } satisfies DeleteDialogData
    });

    dialogRef.afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        switchMap(() => {
          this.deletingId = item.id;
          this.cdr.markForCheck();
          return this.navigationService.deleteMenuItem(item.id);
        }),
        switchMap(() => this.navigationService.getMenuItems()),
        takeUntil(this.destroy$),
        finalize(() => {
          this.deletingId = null;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          if (this.selectedItem?.id === item.id) {
            this.selectedItem = null;
            this.mode = 'none';
            this.initialPayload = null;
          }
          this.applyLoadedItems(items);
          this.notificationService.success('Opción eliminada.');
        },
        error: (error: unknown) => {
          this.notificationService.error(this.errorMessage(error, 'No fue posible eliminar la opción.'));
        }
      });
  }

  clearFilters(): void {
    this.filtersForm.reset({ search: '', status: 'all', level: 'all' });
  }

  expandAll(): void {
    this.tree?.expandAll();
  }

  collapseAll(): void {
    this.tree?.collapseAll();
  }

  isExpanded(item: NavigationMenuItem): boolean {
    return this.tree?.isExpanded(item) ?? false;
  }

  hasChildren(item: NavigationMenuItem): boolean {
    return !!item.children?.length;
  }

  trackById(_: number, item: NavigationMenuItem): number {
    return item.id;
  }

  getLevel(item: NavigationMenuItem): number {
    return (this.flatItems.find(({ item: candidate }) => candidate.id === item.id)?.depth ?? 0) + 1;
  }

  getRolesText(item: NavigationMenuItem): string {
    if (!item.roleIds?.length) {
      return 'Todos los roles';
    }

    return item.roleIds.map((id) => this.roleLookup.get(id) ?? id).join(', ');
  }

  getPermissionsText(item: NavigationMenuItem): string {
    if (!item.permissionIds?.length) {
      return 'Sin permiso adicional';
    }

    return item.permissionIds.map((id) => this.permissionLookup.get(id) ?? id).join(', ');
  }

  parentLabel(parentId: number | null | undefined): string {
    return parentId ? this.findById(parentId)?.label ?? 'Padre no disponible' : 'Sin padre';
  }

  isSelected(item: NavigationMenuItem): boolean {
    return this.selectedItem?.id === item.id;
  }

  private openEditor(item: NavigationMenuItem): void {
    this.mode = 'edit';
    const payload = this.itemPayload(item);
    this.resetForm(item.id, payload);
    this.initialPayload = this.clonePayload(payload);
    this.cdr.markForCheck();
  }

  private resetForm(id: number | null, payload: SaveNavigationMenuItem): void {
    this.form.reset({
      id,
      label: payload.label,
      route: payload.route,
      icon: payload.icon ?? '',
      order: payload.order,
      exact: payload.exact,
      isActive: payload.isActive,
      parentId: payload.parentId ?? null,
      roleIds: [...payload.roleIds],
      permissionIds: [...payload.permissionIds]
    });
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.form.controls.route.updateValueAndValidity({ emitEvent: false });
    this.form.controls.parentId.updateValueAndValidity({ emitEvent: false });
    this.form.updateValueAndValidity({ emitEvent: false });
  }

  private applyLoadedItems(items: NavigationMenuItem[] | null | undefined): void {
    this.menuItems = Array.isArray(items) ? items : [];
    this.flatItems = this.flatten(this.menuItems);
    this.form.get('route')?.updateValueAndValidity({ emitEvent: false });
    this.form.get('parentId')?.updateValueAndValidity({ emitEvent: false });
    this.applyFilters();
  }

  private applyFilters(): void {
    const hasFilters = this.hasActiveFilters;
    this.filteredCount = this.flatItems.filter(({ item, depth }) => this.matchesFilters(item, depth)).length;
    this.filteredMenuItems = hasFilters ? this.filterBranch(this.menuItems, 0) : this.menuItems;
    this.cdr.markForCheck();

    if (hasFilters && this.filteredMenuItems.length) {
      afterNextRender(() => this.tree?.expandAll(), { injector: this.injector });
    }
  }

  private filterBranch(items: NavigationMenuItem[], depth: number): NavigationMenuItem[] {
    const result: NavigationMenuItem[] = [];
    for (const item of items) {
      const children = this.filterBranch(item.children ?? [], depth + 1);
      if (this.matchesFilters(item, depth) || children.length) {
        result.push({ ...item, children });
      }
    }
    return result;
  }

  private matchesFilters(item: NavigationMenuItem, depth: number): boolean {
    const { search, status, level } = this.filtersForm.getRawValue();
    const normalizedSearch = search.trim().toLocaleLowerCase('es');
    const searchable = [
      item.label,
      item.route,
      item.icon ?? '',
      this.getRolesText(item),
      this.getPermissionsText(item)
    ].join(' ').toLocaleLowerCase('es');
    const matchesSearch = !normalizedSearch || searchable.includes(normalizedSearch);
    const matchesStatus = status === 'all'
      || (status === 'active' && item.isActive)
      || (status === 'inactive' && !item.isActive);
    const matchesLevel = level === 'all'
      || (level === 'root' && depth === 0)
      || (level === 'child' && depth > 0);
    return matchesSearch && matchesStatus && matchesLevel;
  }

  private flatten(items: NavigationMenuItem[], depth = 0, visited = new Set<number>()): FlatMenuItem[] {
    const result: FlatMenuItem[] = [];
    for (const item of items) {
      if (visited.has(item.id)) {
        continue;
      }
      visited.add(item.id);
      result.push({ item, depth });
      if (item.children?.length) {
        result.push(...this.flatten(item.children, depth + 1, visited));
      }
    }
    return result;
  }

  private findById(id: number, items = this.menuItems, visited = new Set<number>()): NavigationMenuItem | null {
    for (const item of items) {
      if (visited.has(item.id)) {
        continue;
      }
      visited.add(item.id);
      if (item.id === id) {
        return item;
      }
      const child = this.findById(id, item.children ?? [], visited);
      if (child) {
        return child;
      }
    }
    return null;
  }

  private getDescendantIds(id: number): Set<number> {
    const descendants = new Set<number>();
    const root = this.findById(id);
    const visit = (items: NavigationMenuItem[]): void => {
      for (const item of items) {
        if (descendants.has(item.id)) {
          continue;
        }
        descendants.add(item.id);
        visit(item.children ?? []);
      }
    };
    visit(root?.children ?? []);
    return descendants;
  }

  private duplicateRouteValidator(control: AbstractControl): ValidationErrors | null {
    const route = String(control.value ?? '').trim().toLocaleLowerCase('es');
    if (!route) {
      return null;
    }
    const currentId = this.form?.value.id;
    return this.flatItems.some(({ item }) => (
      item.id !== currentId && item.route.trim().toLocaleLowerCase('es') === route
    ))
      ? { duplicateRoute: true }
      : null;
  }

  private parentValidator(control: AbstractControl): ValidationErrors | null {
    const parentId = control.value === null || control.value === undefined ? null : Number(control.value);
    if (parentId === null) {
      return null;
    }
    const currentId = this.form?.value.id;
    if (!this.findById(parentId)) {
      return { parentMissing: true };
    }
    if (currentId === parentId) {
      return { parentSelf: true };
    }
    if (currentId && this.getDescendantIds(currentId).has(parentId)) {
      return { parentCycle: true };
    }
    return null;
  }

  private emptyPayload(parentId: number | null): SaveNavigationMenuItem {
    return {
      label: '',
      route: '',
      icon: '',
      order: this.nextOrder(parentId),
      exact: false,
      isActive: true,
      parentId,
      roleIds: [],
      permissionIds: []
    };
  }

  private nextOrder(parentId: number | null): number {
    const siblings = parentId
      ? this.findById(parentId)?.children ?? []
      : this.menuItems;
    return siblings.length ? Math.max(...siblings.map((item) => item.order)) + 1 : 1;
  }

  private itemPayload(item: NavigationMenuItem): SaveNavigationMenuItem {
    return {
      label: item.label,
      route: item.route,
      icon: item.icon ?? '',
      order: item.order,
      exact: item.exact,
      isActive: item.isActive,
      parentId: item.parentId ?? null,
      roleIds: [...(item.roleIds ?? [])],
      permissionIds: [...(item.permissionIds ?? [])]
    };
  }

  private toPayload(): SaveNavigationMenuItem {
    const value = this.form.getRawValue();
    return {
      label: value.label?.trim() ?? '',
      route: value.route?.trim() ?? '',
      icon: value.icon?.trim() || null,
      order: Number(value.order ?? 0),
      exact: !!value.exact,
      isActive: !!value.isActive,
      parentId: value.parentId ?? null,
      roleIds: [...(value.roleIds ?? [])],
      permissionIds: [...(value.permissionIds ?? [])]
    };
  }

  private clonePayload(payload: SaveNavigationMenuItem): SaveNavigationMenuItem {
    return {
      ...payload,
      roleIds: [...payload.roleIds],
      permissionIds: [...payload.permissionIds]
    };
  }

  private payloadKey(payload: SaveNavigationMenuItem): string {
    return JSON.stringify({
      ...payload,
      icon: payload.icon ?? '',
      parentId: payload.parentId ?? null,
      roleIds: [...payload.roleIds].sort(),
      permissionIds: [...payload.permissionIds].sort()
    });
  }

  private ensureCanManage(): boolean {
    if (this.canManage) {
      return true;
    }
    this.notificationService.error('No tienes permiso para administrar opciones del menú.');
    return false;
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (typeof error === 'string' && error.trim()) {
      return error;
    }
    if (!error || typeof error !== 'object') {
      return fallback;
    }
    const candidate = error as {
      error?: string | { message?: string; title?: string; detail?: string };
      message?: string;
    };
    if (typeof candidate.error === 'string' && candidate.error.trim()) {
      return candidate.error;
    }
    if (candidate.error && typeof candidate.error === 'object') {
      return candidate.error.detail
        ?? candidate.error.message
        ?? candidate.error.title
        ?? fallback;
    }
    return candidate.message && !candidate.message.includes('[object Object]')
      ? candidate.message
      : fallback;
  }
}
