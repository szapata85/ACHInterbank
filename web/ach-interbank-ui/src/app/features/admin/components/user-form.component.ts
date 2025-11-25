import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UsersApiService, RolesApiService } from '../services/users-api.service';
import { RoleSummary, SaveUserRequest, UserSummary } from '../models/user.model';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly usersApi = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);

  roles: RoleSummary[] = [];
  isEdit = false;
  userId: string | null = null;

  readonly form = this.fb.group({
    userName: ['', Validators.required],
    fullName: [''],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    password: [''],
    roleIds: this.fb.control<string[]>([])
  });

  ngOnInit(): void {
    this.rolesApi.getRoles().subscribe((roles) => (this.roles = roles));

    this.userId = this.route.snapshot.paramMap.get('id');
    if (this.userId) {
      this.isEdit = true;
      this.usersApi.getUser(this.userId).subscribe((user) => this.patchForm(user));
    }
  }

  private patchForm(user: UserSummary): void {
    this.form.patchValue({
      userName: user.userName,
      fullName: user.fullName,
      email: user.email,
      phoneNumber: user.phoneNumber,
      roleIds: user.roles.map((r) => r.id)
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: SaveUserRequest = this.form.value as SaveUserRequest;

    const request$ = this.isEdit && this.userId
      ? this.usersApi.updateUser(this.userId, payload)
      : this.usersApi.createUser(payload);

    request$.subscribe(() => this.router.navigate(['/users']));
  }
}
