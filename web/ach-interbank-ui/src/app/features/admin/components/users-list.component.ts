import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { NotificationService } from '../../../core/services/notification.service';
import { RolesApiService, UsersApiService } from '../services/users-api.service';
import { RoleSummary, UserFilter, UserSummary } from '../models/user.model';
import { UserPresentationService } from '../services/user-presentation.service';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    SharedModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule
  ]
})
export class UsersListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly usersApi = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly presentation = inject(UserPresentationService);

  readonly filterForm = this.fb.group({
    search: [''],
    roleId: [''],
    page: [1],
    pageSize: [10]
  });

  readonly displayedColumns = ['fullName', 'userName', 'email', 'roles', 'status', 'actions'];

  users: UserSummary[] = [];
  roles: RoleSummary[] = [];
  total = 0;
  loading = false;
  hasLoaded = false;
  loadError: string | null = null;
  confirmUser: UserSummary | null = null;
  isUpdatingStatus = false;

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

  changePage(event: PageEvent): void {
    this.filterForm.patchValue({ page: event.pageIndex + 1, pageSize: event.pageSize });
    this.loadUsers();
  }

  clearFilters(): void {
    this.filterForm.patchValue({ search: '', roleId: '', page: 1 });
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

  roleLabel(role: RoleSummary): string {
    return this.presentation.roleLabel(role);
  }

  disableUser(): void {
    if (!this.confirmUser) {
      return;
    }
    this.usersApi.deactivateUser(this.confirmUser.id).subscribe({
      next: () => {
        this.notifications.success('El usuario fue desactivado correctamente.');
        this.confirmUser = null;
        this.loadUsers();
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible desactivar el usuario. Inténtalo nuevamente.');
        this.confirmUser = null;
        this.cdr.markForCheck();
      }
    });
  }
}
