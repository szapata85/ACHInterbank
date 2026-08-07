import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RolesApiService, UsersApiService } from '../services/users-api.service';
import { RoleSummary, UserSummary } from '../models/user.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationService } from '../../../core/services/notification.service';
import { UserPresentationService } from '../services/user-presentation.service';

@Component({
  selector: 'app-user-roles',
  templateUrl: './user-roles.component.html',
  styleUrls: ['./user-roles.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule, MatButtonModule, MatCardModule, MatCheckboxModule, MatProgressSpinnerModule]
})
export class UserRolesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly usersApi = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly notifications = inject(NotificationService);
  private readonly presentation = inject(UserPresentationService);

  roles: RoleSummary[] = [];
  user?: UserSummary;
  selectedRoleIds = new Set<string>();
  saving = false;

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    if (!userId) {
      this.router.navigate(['/users']);
      return;
    }

    this.rolesApi.getRoles().subscribe((roles) => {
      this.roles = roles;
      this.cdr.markForCheck();
    });
    this.usersApi.getUser(userId).subscribe((user) => {
      this.user = user;
      this.selectedRoleIds = new Set(user.roles.map((r) => r.id));
      this.cdr.markForCheck();
    });
  }

  toggleRole(role: RoleSummary): void {
    if (this.selectedRoleIds.has(role.id)) {
      this.selectedRoleIds.delete(role.id);
    } else {
      this.selectedRoleIds.add(role.id);
    }
  }

  roleLabel(role: RoleSummary): string {
    return this.presentation.roleLabel(role);
  }

  save(): void {
    if (!this.user || this.saving) {
      return;
    }

    if (this.selectedRoleIds.size === 0) {
      this.notifications.error('Selecciona al menos un perfil de acceso.');
      return;
    }

    this.saving = true;
    this.usersApi.assignRoles(this.user.id, Array.from(this.selectedRoleIds)).subscribe({
      next: () => {
        this.saving = false;
        this.notifications.success('Los perfiles de acceso se actualizaron correctamente.');
        this.cdr.markForCheck();
        this.router.navigate(['/users/list']);
      },
      error: () => {
        this.saving = false;
        this.notifications.error('No fue posible actualizar los perfiles de acceso. Inténtalo nuevamente.');
        this.cdr.markForCheck();
      }
    });
  }
}
