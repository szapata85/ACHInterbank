import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { TableColumn } from '../../../shared/components/table.component';
import { RolesApiService, UsersApiService } from '../services/users-api.service';
import { RoleSummary, UserFilter, UserSummary } from '../models/user.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class UsersListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly usersApi = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('rowActions', { static: true }) rowActionsTemplate!: TemplateRef<any>;
  @ViewChild('headerActions', { static: true }) headerActionsTemplate!: TemplateRef<any>;

  readonly filterForm = this.fb.group({
    search: [''],
    roleId: [''],
    page: [1],
    pageSize: [10]
  });

  columns: TableColumn[] = [
    { key: 'userName', label: 'Usuario' },
    { key: 'fullName', label: 'Nombre' },
    { key: 'email', label: 'Email' },
    { key: 'phoneNumber', label: 'Teléfono' },
    { key: 'rolesText', label: 'Roles' },
    { key: 'statusText', label: 'Estado' }
  ];

  users: UserSummary[] = [];
  roles: RoleSummary[] = [];
  total = 0;
  loading = false;
  hasLoaded = false;
  loadError: string | null = null;
  confirmUser: UserSummary | null = null;

  ngOnInit(): void {
    this.loadRoles();
    this.loadUsers();
  }

  loadUsers(): void {
    const filter: UserFilter = this.filterForm.value;
    this.loading = true;
    this.loadError = null;
    this.cdr.markForCheck();
    this.usersApi.getUsers(filter).subscribe({
      next: (response) => {
        this.users = response.items.map((x) => ({
          ...x,
          rolesText: x.roles?.map((r) => r.name).join(', ') ?? '-',
          statusText: x.isActive ? 'Activo' : 'Inactivo'
        }));
        this.total = response.total;
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadError = 'No fue posible cargar los usuarios';
        this.notifications.error(this.loadError);
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      }
    });
  }

  loadRoles(): void {
    this.rolesApi.getRoles().subscribe((roles) => {
      this.roles = roles;
      this.cdr.markForCheck();
    });
  }

  changePage(page: number): void {
    this.filterForm.patchValue({ page });
    this.loadUsers();
  }

  createUser(): void {
    this.router.navigate(['/users/new']);
  }

  editUser(user: UserSummary): void {
    this.router.navigate(['/users', user.id, 'edit']);
  }

  manageRoles(user: UserSummary): void {
    this.router.navigate(['/users', user.id, 'roles']);
  }

  goToBranding(): void {
    this.router.navigate(['/users/branding']);
  }

  confirmDisable(user: UserSummary): void {
    this.confirmUser = user;
  }

  disableUser(): void {
    if (!this.confirmUser) {
      return;
    }
    this.usersApi.deactivateUser(this.confirmUser.id).subscribe({
      next: () => {
        this.notifications.success('Usuario desactivado');
        this.confirmUser = null;
        this.loadUsers();
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible desactivar al usuario');
        this.confirmUser = null;
        this.cdr.markForCheck();
      }
    });
  }
}
