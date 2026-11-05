import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { SharedModule } from '../../../shared/shared.module';
import { CustomersApiService } from '../../../core/services/customers-api.service';
import { CustomerDetail, SaveCustomerRequest } from '../../../core/models/customer.model';
import { NotificationService } from '../../../core/services/notification.service';

interface SelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-customer-form',
  templateUrl: './customer-form.component.html',
  styleUrls: ['./customer-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    SharedModule, RouterModule, MatButtonModule, MatCardModule, MatChipsModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule, MatSelectModule
  ]
})
export class CustomerFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(CustomersApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  isEdit = false;
  customerId: string | null = null;
  loading = false;
  loadError = false;
  saving = false;
  operationError = '';
  accountError = '';

  readonly personTypeOptions: SelectOption[] = [
    { value: 'PN', label: 'Persona natural' },
    { value: 'PJ', label: 'Persona jurídica' }
  ];
  readonly documentTypeOptions: SelectOption[] = [
    { value: 'CC', label: 'Cédula de ciudadanía' },
    { value: 'CE', label: 'Cédula de extranjería' },
    { value: 'NIT', label: 'Número de identificación tributaria (NIT)' },
    { value: 'PAS', label: 'Pasaporte' },
    { value: 'TI', label: 'Tarjeta de identidad' },
    { value: 'OTRO', label: 'Otro documento' }
  ];
  readonly genderOptions: SelectOption[] = [
    { value: 'MASCULINO', label: 'Masculino' },
    { value: 'FEMENINO', label: 'Femenino' },
    { value: 'NO_BINARIO', label: 'No binario' },
    { value: 'OTRO', label: 'Otro' },
    { value: 'NO_ESPECIFICA', label: 'Prefiere no indicar' }
  ];

  readonly form = this.fb.group({
    personType: ['', Validators.required],
    documentType: ['', Validators.required],
    documentNumber: ['', [Validators.required, Validators.pattern(/\S/)]],
    companyName: [''],
    firstName: [''],
    middleName: [''],
    lastName: [''],
    secondLastName: [''],
    gender: ['']
  });

  readonly accountNumberInput = this.fb.control('');
  accountNumbers: string[] = [];

  get isCompany(): boolean {
    return this.form.controls.personType.value === 'PJ';
  }

  get hasAccountNumbers(): boolean {
    return this.accountNumbers.length > 0;
  }

  ngOnInit(): void {
    this.form.controls.personType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updatePersonValidators();
        this.cdr.markForCheck();
      });
    this.updatePersonValidators();

    this.customerId = this.route.snapshot.paramMap.get('id');
    if (this.customerId) {
      this.isEdit = true;
      this.loadCustomer();
    }
  }

  private updatePersonValidators(): void {
    const requiredText = [Validators.required, Validators.pattern(/\S/)];
    this.form.controls.companyName.setValidators(this.isCompany ? requiredText : []);
    this.form.controls.firstName.setValidators(this.form.controls.personType.value === 'PN' ? requiredText : []);
    this.form.controls.lastName.setValidators(this.form.controls.personType.value === 'PN' ? requiredText : []);
    this.form.controls.companyName.updateValueAndValidity({ emitEvent: false });
    this.form.controls.firstName.updateValueAndValidity({ emitEvent: false });
    this.form.controls.lastName.updateValueAndValidity({ emitEvent: false });
  }

  loadCustomer(): void {
    if (!this.customerId) return;
    this.loading = true;
    this.loadError = false;
    this.operationError = '';
    this.api.getById(this.customerId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: customer => {
        this.patch(customer);
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.notifications.error('No pudimos cargar la información del cliente. Puedes intentarlo nuevamente.');
        this.cdr.markForCheck();
      }
    });
  }

  private patch(customer: CustomerDetail): void {
    this.form.patchValue({
      personType: customer.personType,
      documentType: customer.documentType,
      documentNumber: customer.documentNumber,
      companyName: customer.companyName ?? '',
      firstName: customer.firstName ?? '',
      middleName: customer.middleName ?? '',
      lastName: customer.lastName ?? '',
      secondLastName: customer.secondLastName ?? '',
      gender: customer.gender ?? ''
    });
    const accounts = customer.accountNumbers?.length ? customer.accountNumbers : [customer.accountNumber];
    this.accountNumbers = Array.from(new Set(accounts.map(item => (item ?? '').trim()).filter(Boolean)));
    this.accountError = '';
    this.form.markAsPristine();
  }

  addAccountNumber(): boolean {
    const accountNumber = (this.accountNumberInput.value ?? '').trim();
    if (!accountNumber) {
      this.accountError = 'Escribe un número de cuenta para agregarlo.';
      this.accountNumberInput.markAsTouched();
      this.cdr.markForCheck();
      return false;
    }
    if (this.accountNumbers.includes(accountNumber)) {
      this.accountError = 'Esta cuenta ya está agregada.';
      this.accountNumberInput.markAsTouched();
      this.cdr.markForCheck();
      return false;
    }

    this.accountNumbers = [...this.accountNumbers, accountNumber];
    this.accountNumberInput.setValue('');
    this.accountError = '';
    this.cdr.markForCheck();
    return true;
  }

  removeAccountNumber(accountNumber: string): void {
    if (this.saving || this.loading) return;
    this.accountNumbers = this.accountNumbers.filter(item => item !== accountNumber);
    this.accountError = this.hasAccountNumbers ? '' : 'Agrega al menos una cuenta para continuar.';
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.saving || this.loading || this.loadError) return;

    this.operationError = '';
    const pendingAccount = (this.accountNumberInput.value ?? '').trim();
    // No se pierde una cuenta que el operador escribió pero aún no agregó con el botón.
    if (pendingAccount && !this.addAccountNumber()) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.operationError = 'Revisa los campos señalados antes de guardar.';
      this.cdr.markForCheck();
      return;
    }
    if (!this.hasAccountNumbers) {
      this.accountError = 'Agrega al menos una cuenta para continuar.';
      this.cdr.markForCheck();
      return;
    }

    const values = this.form.getRawValue();
    const payload: SaveCustomerRequest = {
      personType: (values.personType ?? '').trim(),
      documentType: (values.documentType ?? '').trim(),
      documentNumber: (values.documentNumber ?? '').trim(),
      companyName: (values.companyName ?? '').trim(),
      firstName: (values.firstName ?? '').trim(),
      middleName: (values.middleName ?? '').trim(),
      lastName: (values.lastName ?? '').trim(),
      secondLastName: (values.secondLastName ?? '').trim(),
      gender: (values.gender ?? '').trim(),
      accountNumbers: [...this.accountNumbers]
    };

    this.saving = true;
    this.cdr.markForCheck();
    const request$ = this.isEdit && this.customerId
      ? this.api.update(this.customerId, payload)
      : this.api.create(payload);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving = false;
        this.notifications.success(this.isEdit ? 'Los datos del cliente se actualizaron correctamente.' : 'El cliente se creó correctamente.');
        void this.router.navigate(['/customers']);
        this.cdr.markForCheck();
      },
      error: () => {
        this.saving = false;
        this.operationError = 'No pudimos guardar los datos del cliente. Revisa la información e inténtalo nuevamente.';
        this.notifications.error(this.operationError);
        this.cdr.markForCheck();
      }
    });
  }
}
