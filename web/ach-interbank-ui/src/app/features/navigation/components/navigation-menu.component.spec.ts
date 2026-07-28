import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { RolesApiService } from '../../admin/services/users-api.service';
import { NavigationMenuItem } from '../models/navigation-menu.model';
import { NavigationMenuService } from '../services/navigation-menu.service';
import { NavigationMenuComponent } from './navigation-menu.component';

describe('NavigationMenuComponent', () => {
  let fixture: ComponentFixture<NavigationMenuComponent>;
  let component: NavigationMenuComponent;
  let service: jasmine.SpyObj<NavigationMenuService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let dialog: jasmine.SpyObj<MatDialog>;

  const child: NavigationMenuItem = {
    id: 2,
    parentId: 1,
    label: 'Hijo',
    route: '/root/child',
    icon: 'list',
    order: 1,
    exact: true,
    isActive: true,
    roleIds: ['role-admin'],
    permissionIds: ['permission-manage'],
    children: [
      {
        id: 3,
        parentId: 2,
        label: 'Nieto',
        route: '/root/child/grandchild',
        icon: 'menu',
        order: 1,
        exact: true,
        isActive: true,
        roleIds: [],
        permissionIds: [],
        children: []
      }
    ]
  };

  const menuItems: NavigationMenuItem[] = [
    {
      id: 1,
      parentId: null,
      label: 'Raíz',
      route: '/root',
      icon: 'home',
      order: 1,
      exact: true,
      isActive: true,
      roleIds: ['role-admin'],
      permissionIds: ['permission-manage'],
      children: [child]
    },
    {
      id: 4,
      parentId: null,
      label: 'Inactivo',
      route: '/inactive',
      icon: null,
      order: 2,
      exact: false,
      isActive: false,
      roleIds: [],
      permissionIds: [],
      children: []
    }
  ];

  beforeEach(async () => {
    service = jasmine.createSpyObj<NavigationMenuService>(
      'NavigationMenuService',
      ['getMenuItems', 'createMenuItem', 'updateMenuItem', 'deleteMenuItem']
    );
    service.getMenuItems.and.returnValue(of(menuItems));

    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    const auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole', 'hasPermission']);
    auth.hasRole.and.returnValue(true);
    auth.hasPermission.and.returnValue(true);
    const rolesApi = jasmine.createSpyObj<RolesApiService>('RolesApiService', ['getRoles']);
    rolesApi.getRoles.and.returnValue(of([
      { id: 'role-admin', name: 'Admin', description: 'Acceso completo al sistema' },
      { id: 'role-operator', name: 'ACH.Operator', description: 'Operaciones básicas sobre ACH' },
      { id: 'role-auditor', name: 'Custom.Auditor', description: 'Auditoría operativa' }
    ]));
    const permissions = jasmine.createSpyObj<PermissionsService>('PermissionsService', ['getPermissions']);
    permissions.getPermissions.and.returnValue(of([
      { id: 'permission-manage', name: 'CanManageUsers', description: null },
      { id: 'permission-read', name: 'CanReadAch', description: 'Consulta de operaciones ACH' },
      { id: 'permission-custom', name: 'Custom.Reconcile', description: 'Conciliar operaciones manuales' }
    ]));

    await TestBed.configureTestingModule({
      imports: [NavigationMenuComponent, NoopAnimationsModule],
      providers: [
        { provide: NavigationMenuService, useValue: service },
        { provide: NotificationService, useValue: notifications },
        { provide: AuthService, useValue: auth },
        { provide: RolesApiService, useValue: rolesApi },
        { provide: PermissionsService, useValue: permissions }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NavigationMenuComponent);
    component = fixture.componentInstance;
    const componentDialog = (component as unknown as { dialog: MatDialog }).dialog;
    spyOn(componentDialog, 'open');
    dialog = componentDialog as jasmine.SpyObj<MatDialog>;
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('loads the hierarchy, catalogs and counters on initialization', () => {
    expect(service.getMenuItems).toHaveBeenCalledTimes(1);
    expect(component.hasLoaded).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.totalCount).toBe(4);
    expect(component.activeCount).toBe(3);
    expect(component.inactiveCount).toBe(1);
    expect(component.roles.length).toBe(3);
    expect(component.permissions.length).toBe(3);
  });

  it('humanizes known roles and uses a friendly description when no mapping exists', () => {
    expect(component.roleDisplayLabel(component.roles[0])).toBe('Administrador');
    expect(component.roleDisplayLabel(component.roles[1])).toBe('Operador ACH');
    expect(component.roleDisplayLabel(component.roles[2])).toBe('Auditoría operativa');
  });

  it('humanizes known permissions and uses a friendly description when no mapping exists', () => {
    expect(component.permissionDisplayLabel(component.permissions[0])).toBe('Administrar usuarios');
    expect(component.permissionDisplayLabel(component.permissions[1])).toBe('Consultar información ACH');
    expect(component.permissionDisplayLabel(component.permissions[2])).toBe('Conciliar operaciones manuales');
  });

  it('keeps technical role and permission ids unchanged in the form and payload', () => {
    component.editItem(child);

    expect(component.form.value.roleIds).toEqual(['role-admin']);
    expect(component.form.value.permissionIds).toEqual(['permission-manage']);

    const payload = (
      component as unknown as {
        toPayload(): { roleIds: string[]; permissionIds: string[] };
      }
    ).toPayload();
    expect(payload.roleIds).toEqual(['role-admin']);
    expect(payload.permissionIds).toEqual(['permission-manage']);
  });

  it('uses friendly labels in details and summarizes multiple selections', () => {
    expect(component.getRolesText(menuItems[0])).toBe('Administrador');
    expect(component.getPermissionsText(menuItems[0])).toBe('Administrar usuarios');
    expect(component.roleSelectionText(['role-admin', 'role-operator', 'role-auditor']))
      .toBe('Administrador, Operador ACH +1 adicionales');
    expect(component.permissionSelectionText(['permission-manage', 'permission-read', 'permission-custom']))
      .toBe('Administrar usuarios, Consultar información ACH +1 adicionales');
  });

  it('renders friendly labels in the closed multiple-select triggers without changing the form', async () => {
    component.editItem(child);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const roleTrigger = fixture.nativeElement.querySelector(
      'mat-select[formcontrolname="roleIds"] .mat-mdc-select-value'
    ) as HTMLElement;
    const permissionTrigger = fixture.nativeElement.querySelector(
      'mat-select[formcontrolname="permissionIds"] .mat-mdc-select-value'
    ) as HTMLElement;
    expect(roleTrigger.textContent?.replace(/\s+/g, ' ').trim()).toBe('Administrador');
    expect(permissionTrigger.textContent?.replace(/\s+/g, ' ').trim()).toBe('Administrar usuarios');
    expect(component.form.value.roleIds).toEqual(['role-admin']);
    expect(component.form.value.permissionIds).toEqual(['permission-manage']);
  });

  it('finds menu items by friendly and technical permission labels', () => {
    component.filtersForm.controls.search.setValue('administrar usuarios');
    expect(component.filteredCount).toBe(2);

    component.filtersForm.controls.search.setValue('CanManageUsers');
    expect(component.filteredCount).toBe(2);
  });

  it('exposes loading until the initial request finishes', () => {
    const pending = new Subject<NavigationMenuItem[]>();
    service.getMenuItems.and.returnValue(pending);

    component.loadInitialData();

    expect(component.loading).toBeTrue();
    pending.next(menuItems);
    pending.complete();
    expect(component.loading).toBeFalse();
    expect(component.hasLoaded).toBeTrue();
  });

  it('shows a clear loading error and finishes the loading state', () => {
    service.getMenuItems.and.returnValue(throwError(() => ({ error: { detail: 'API no disponible' } })));

    component.loadInitialData();

    expect(component.loading).toBeFalse();
    expect(component.loadError).toBe('API no disponible');
    expect(notifications.error).toHaveBeenCalledWith('API no disponible');
  });

  it('selects an item without opening the editor or performing writes', () => {
    component.selectItem(child);

    expect(component.selectedItem?.id).toBe(2);
    expect(component.mode).toBe('none');
    expect(service.createMenuItem).not.toHaveBeenCalled();
    expect(service.updateMenuItem).not.toHaveBeenCalled();
  });

  it('starts root and child creation with the correct clean context', () => {
    component.editItem(child);
    component.startCreate();

    expect(component.mode).toBe('create');
    expect(component.form.value.id).toBeNull();
    expect(component.form.value.parentId).toBeNull();
    expect(component.form.value.label).toBe('');

    component.startCreate(menuItems[0]);

    expect(component.form.value.parentId).toBe(1);
    expect(component.form.value.order).toBe(2);
    expect(component.canSave).toBeFalse();
  });

  it('opens a copied edit model and cancel restores persisted values without writing', () => {
    component.editItem(child);
    component.form.controls.label.setValue('Cambio no persistido');

    component.cancelEditing();

    expect(component.mode).toBe('none');
    expect(component.form.value.label).toBe('Hijo');
    expect(child.label).toBe('Hijo');
    expect(service.updateMenuItem).not.toHaveBeenCalled();
  });

  it('requires valid fields and renders Material validation messages', () => {
    component.startCreate();
    component.form.controls.label.setValue('');
    component.form.controls.route.setValue('ruta con espacios');
    component.form.controls.order.setValue(-1);
    component.form.markAllAsTouched();
    fixture.detectChanges();

    expect(component.form.invalid).toBeTrue();
    expect(component.canSave).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('La etiqueta es obligatoria');
    expect(fixture.nativeElement.textContent).toContain('Usa una ruta interna válida');
    expect(fixture.nativeElement.textContent).toContain('El orden no puede ser negativo');
  });

  it('detects duplicate routes and actual changes instead of relying only on dirty state', () => {
    component.editItem(child);
    component.form.controls.label.setValue('Cambio');
    expect(component.hasChanges).toBeTrue();

    component.form.controls.label.setValue('Hijo');
    expect(component.hasChanges).toBeFalse();

    component.form.controls.route.setValue('/inactive');
    expect(component.form.controls.route.hasError('duplicateRoute')).toBeTrue();
  });

  it('does not save an invalid or unchanged form', () => {
    component.startCreate();
    component.save();
    component.editItem(child);
    component.save();

    expect(service.createMenuItem).not.toHaveBeenCalled();
    expect(service.updateMenuItem).not.toHaveBeenCalled();
  });

  it('prevents duplicate create requests while a save is pending', () => {
    const pending = new Subject<NavigationMenuItem>();
    service.createMenuItem.and.returnValue(pending);
    component.startCreate();
    component.form.patchValue({ label: 'Temporal', route: '/temporal' });

    component.save();
    component.save();

    expect(service.createMenuItem).toHaveBeenCalledTimes(1);
    expect(component.saving).toBeTrue();
    pending.error({ error: 'Error controlado' });
    expect(component.saving).toBeFalse();
    expect(component.form.value.label).toBe('Temporal');
  });

  it('saves one update, refreshes the tree and keeps the saved item selected', () => {
    const updated = { ...child, label: 'Hijo actualizado' };
    const refreshed = [{ ...menuItems[0], children: [updated] }, menuItems[1]];
    service.updateMenuItem.and.returnValue(of(updated));
    service.getMenuItems.and.returnValue(of(refreshed));
    component.editItem(child);
    component.form.controls.label.setValue('Hijo actualizado');

    component.save();

    expect(service.updateMenuItem).toHaveBeenCalledTimes(1);
    expect(component.selectedItem?.label).toBe('Hijo actualizado');
    expect(component.mode).toBe('edit');
    expect(component.hasChanges).toBeFalse();
    expect(notifications.success).toHaveBeenCalledWith('Cambios guardados.');
  });

  it('excludes self and every descendant from valid parent options', () => {
    component.editItem(menuItems[0]);

    expect(component.parentOptions.map(({ item }) => item.id)).toEqual([4]);
  });

  it('rejects self, missing parents and indirect cycles even when values are patched manually', () => {
    component.editItem(menuItems[0]);

    component.form.controls.parentId.setValue(1);
    expect(component.form.controls.parentId.hasError('parentSelf')).toBeTrue();

    component.form.controls.parentId.setValue(999);
    expect(component.form.controls.parentId.hasError('parentMissing')).toBeTrue();

    component.form.controls.parentId.setValue(3);
    expect(component.form.controls.parentId.hasError('parentCycle')).toBeTrue();
  });

  it('filters by name, route, status and restores the full hierarchy', () => {
    component.filtersForm.controls.search.setValue('grandchild');
    expect(component.filteredCount).toBe(1);
    expect(component.filteredMenuItems[0].children?.[0].children?.[0].id).toBe(3);
    fixture.detectChanges();
    component.expandAll();
    fixture.detectChanges();
    const renderedLabels = Array.from(
      fixture.nativeElement.querySelectorAll('.navigation-admin__node-title') as NodeListOf<HTMLElement>
    ).map((element) => element.textContent?.trim());
    expect(renderedLabels).toContain('Nieto');

    component.filtersForm.patchValue({ search: '', status: 'inactive' });
    expect(component.filteredCount).toBe(1);
    expect(component.filteredMenuItems.map((item) => item.id)).toEqual([4]);

    component.clearFilters();
    expect(component.filteredCount).toBe(4);
    expect(component.filteredMenuItems.length).toBe(2);
  });

  it('persists activate or deactivate only for the selected item', () => {
    service.updateMenuItem.and.returnValue(of({ ...child, isActive: false }));
    service.getMenuItems.and.returnValue(of(menuItems));

    component.toggleActive(child);

    expect(service.updateMenuItem).toHaveBeenCalledTimes(1);
    const [id, payload] = service.updateMenuItem.calls.mostRecent().args;
    expect(id).toBe(2);
    expect(payload.isActive).toBeFalse();
  });

  it('deletes only after Material dialog confirmation and refreshes the hierarchy', () => {
    const leaf = child.children![0];
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as never);
    service.deleteMenuItem.and.returnValue(of(void 0));
    service.getMenuItems.and.returnValue(of(menuItems));

    component.confirmDelete(leaf);

    expect(dialog.open).toHaveBeenCalled();
    expect(service.deleteMenuItem).toHaveBeenCalledOnceWith(3);
  });

  it('blocks deletion of a parent and explains the restriction', () => {
    component.confirmDelete(menuItems[0]);

    expect(dialog.open).not.toHaveBeenCalled();
    expect(service.deleteMenuItem).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith(
      'La opción tiene hijos. Reasígnalos o elimínalos primero.'
    );
  });
});
