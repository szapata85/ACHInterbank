import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { UsersApiService, RolesApiService } from '../services/users-api.service';
import { RoleSummary, UserFilter, UserSummary } from '../models/user.model';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UsersListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly usersApi = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly router = inject(Router);

  readonly filterForm = this.fb.group({
    search: [''],
    roleId: [''],
    page: [1],
    pageSize: [10]
  });

  users: UserSummary[] = [];
  roles: RoleSummary[] = [];
  total = 0;

  ngOnInit(): void {
    this.loadRoles();
    this.loadUsers();
  }

  loadUsers(): void {
    const filter: UserFilter = this.filterForm.value;
    this.usersApi.getUsers(filter).subscribe((response) => {
      this.users = response.items;
      this.total = response.total;
    });
  }

  loadRoles(): void {
    this.rolesApi.getRoles().subscribe((roles) => (this.roles = roles));
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
}
