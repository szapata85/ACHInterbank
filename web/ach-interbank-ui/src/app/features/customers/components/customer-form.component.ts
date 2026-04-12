import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { CustomersApiService } from '../services/customers-api.service';
import { CustomerDetail, SaveCustomerRequest } from '../models/customer.model';
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
  imports: [SharedModule, RouterModule]
})
export class CustomerFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(CustomersApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  isEdit = false;
  customerId: string | null = null;

  readonly personTypeOptions: SelectOption[] = [
    { value: 'PN', label: 'Persona natural' },
    { value: 'PJ', label: 'Persona jurídica' }
  ];

  readonly documentTypeOptions: SelectOption[] = [
    { value: 'CC', label: 'Cédula de Ciudadanía' },
    { value: 'CE', label: 'Cédula de Extranjería' },
    { value: 'NIT', label: 'Número de Identificación Tributaria' },
    { value: 'PAS', label: 'Pasaporte' },
    { value: 'TI', label: 'Tarjeta de Identidad' },
    { value: 'OTRO', label: 'Otro' }
  ];

  readonly genderOptions: SelectOption[] = [
    { value: 'MASCULINO', label: 'Masculino' },
    { value: 'FEMENINO', label: 'Femenino' },
    { value: 'NO_BINARIO', label: 'No binario' },
    { value: 'OTRO', label: 'Otro' },
    { value: 'NO_ESPECIFICA', label: 'No especifica' }
  ];

  readonly form = this.fb.group({
    personType: ['', Validators.required],
    documentType: ['', Validators.required],
    documentNumber: ['', Validators.required],
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
    return this.form.get('personType')?.value === 'PJ';
  }

  get hasAccountNumbers(): boolean {
    return this.accountNumbers.length > 0;
  }

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id');
    if (this.customerId) {
      this.isEdit = true;
      this.api.getById(this.customerId).subscribe({
        next: (customer) => {
          this.patch(customer);
          this.cdr.markForCheck();
        },
        error: () => {
          this.notifications.error('No fue posible cargar el cliente seleccionado');
          this.cdr.markForCheck();
        }
      });
    }
  }

  private patch(customer: CustomerDetail): void {
    this.form.patchValue({
      personType: customer.personType,
      documentType: customer.documentType,
      documentNumber: customer.documentNumber,
      companyName: customer.companyName ?? '',
      firstName: customer.firstName,
      middleName: customer.middleName ?? '',
      lastName: customer.lastName,
      secondLastName: customer.secondLastName ?? '',
      gender: customer.gender ?? ''
    });

    const accounts = (customer.accountNumbers?.length ? customer.accountNumbers : [customer.accountNumber])
      .filter((item) => !!item);
    this.accountNumbers = [...accounts];
  }

  addAccountNumber(): void {
    const accountNumber = (this.accountNumberInput.value ?? '').trim();
    if (!accountNumber || this.accountNumbers.includes(accountNumber)) {
      return;
    }

    this.accountNumbers = [...this.accountNumbers, accountNumber];
    this.accountNumberInput.setValue('');
  }

  removeAccountNumber(accountNumber: string): void {
    this.accountNumbers = this.accountNumbers.filter((item) => item !== accountNumber);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.hasAccountNumbers) {
      this.notifications.error('Debe agregar al menos un número de cuenta.');
      return;
    }

    const payload = {
      ...(this.form.getRawValue() as Omit<SaveCustomerRequest, 'accountNumbers'>),
      accountNumbers: this.accountNumbers
    } as SaveCustomerRequest;

    const request$ = this.isEdit && this.customerId
      ? this.api.update(this.customerId, payload)
      : this.api.create(payload);

    request$.subscribe({
      next: () => {
        this.cdr.markForCheck();
        this.router.navigate(['/customers']);
      },
      error: () => {
        this.notifications.error('No fue posible guardar el cliente');
        this.cdr.markForCheck();
      }
    });
  }
}
