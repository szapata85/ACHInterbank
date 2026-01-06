import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AbstractControl, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UsersApiService, RolesApiService } from '../services/users-api.service';
import { RoleSummary, SaveUserRequest, UserSummary } from '../models/user.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
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

  readonly passwordRules = {
    minLength: 6,
    minUppercase: 1,
    minNumbers: 1,
    minSpecial: 1,
    maxSpecial: 4
  };

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
    } else {
      const passwordControl = this.form.get('password');
      passwordControl?.setValidators([Validators.required, this.passwordStrengthValidator]);
      passwordControl?.updateValueAndValidity({ emitEvent: false });
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

  get passwordStrength(): number {
    const value = this.form.get('password')?.value ?? '';
    return this.calculatePasswordStrength(value);
  }

  get passwordStrengthLabel(): string {
    const score = this.passwordStrength;
    if (score >= 5) {
      return 'Fuerte';
    }
    if (score >= 3) {
      return 'Media';
    }
    if (score >= 2) {
      return 'Débil';
    }
    return 'Muy débil';
  }

  private passwordStrengthValidator(control: AbstractControl) {
    const value = String(control.value ?? '');
    const { minLength, minUppercase, minNumbers, minSpecial, maxSpecial } = (this as UserFormComponent).passwordRules;
    const uppercaseCount = (value.match(/[A-Z]/g) ?? []).length;
    const numberCount = (value.match(/\d/g) ?? []).length;
    const specialCount = (value.match(/[^A-Za-z0-9]/g) ?? []).length;
    const errors: Record<string, boolean> = {};

    if (value.length < minLength) {
      errors.minLength = true;
    }
    if (uppercaseCount < minUppercase) {
      errors.minUppercase = true;
    }
    if (numberCount < minNumbers) {
      errors.minNumbers = true;
    }
    if (specialCount < minSpecial) {
      errors.minSpecial = true;
    }
    if (maxSpecial !== null && specialCount > maxSpecial) {
      errors.maxSpecial = true;
    }

    return Object.keys(errors).length > 0 ? { weakPassword: true, ...errors } : null;
  }

  private calculatePasswordStrength(value: string): number {
    const { minLength, minUppercase, minNumbers, minSpecial, maxSpecial } = this.passwordRules;
    const uppercaseCount = (value.match(/[A-Z]/g) ?? []).length;
    const numberCount = (value.match(/\d/g) ?? []).length;
    const specialCount = (value.match(/[^A-Za-z0-9]/g) ?? []).length;
    const rules = [
      value.length >= minLength,
      uppercaseCount >= minUppercase,
      numberCount >= minNumbers,
      specialCount >= minSpecial,
      maxSpecial === null || specialCount <= maxSpecial
    ];

    return rules.filter(Boolean).length;
  }
}
