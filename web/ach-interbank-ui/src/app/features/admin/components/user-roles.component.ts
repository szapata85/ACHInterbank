import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RolesApiService, UsersApiService } from '../services/users-api.service';
import { RoleSummary, UserSummary } from '../models/user.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-user-roles',
  templateUrl: './user-roles.component.html',
  styleUrls: ['./user-roles.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class UserRolesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly usersApi = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);

  roles: RoleSummary[] = [];
  user?: UserSummary;
  selectedRoleIds = new Set<string>();

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    if (!userId) {
      this.router.navigate(['/users']);
      return;
    }

    this.rolesApi.getRoles().subscribe((roles) => (this.roles = roles));
    this.usersApi.getUser(userId).subscribe((user) => {
      this.user = user;
      this.selectedRoleIds = new Set(user.roles.map((r) => r.id));
    });
  }

  toggleRole(role: RoleSummary): void {
    if (this.selectedRoleIds.has(role.id)) {
      this.selectedRoleIds.delete(role.id);
    } else {
      this.selectedRoleIds.add(role.id);
    }
  }

  save(): void {
    if (!this.user) {
      return;
    }

    this.usersApi.assignRoles(this.user.id, Array.from(this.selectedRoleIds)).subscribe(() => {
      this.router.navigate(['/users']);
    });
  }
}
