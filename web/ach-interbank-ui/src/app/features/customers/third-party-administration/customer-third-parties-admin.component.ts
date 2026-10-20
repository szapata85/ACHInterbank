import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CustomersApiService } from '../../../core/services/customers-api.service';
import { CustomerDetail } from '../../../core/models/customer.model';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message.util';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { ThirdPartyAdministrationApiService } from './third-party-administration-api.service';
import {
  ThirdPartyAdministrationDto,
  ThirdPartyAdministrativeStatus
} from './third-party-administration.models';
import {
  administrativeStatusLabel,
  administrativeStatusTone
} from './third-party-administration.presentation';

@Component({
  selector: 'app-customer-third-parties-admin',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './customer-third-parties-admin.component.html',
  styleUrls: ['./customer-third-parties-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CustomerThirdPartiesAdminComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ThirdPartyAdministrationApiService);
  private readonly customersApi = inject(CustomersApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly ThirdPartyAdministrativeStatus = ThirdPartyAdministrativeStatus;
  readonly administrativeStatusLabel = administrativeStatusLabel;
  readonly administrativeStatusTone = administrativeStatusTone;

  customerId = 0;
  customer: CustomerDetail | null = null;
  items: ThirdPartyAdministrationDto[] = [];
  total = 0;
  page = 1;
  readonly pageSize = 20;
  loading = false;
  loadError = '';
  createOpen = false;
  saving = false;
  commandError = '';

  readonly filters = this.fb.group({
    search: [''],
    status: [null as ThirdPartyAdministrativeStatus | null]
  });

  readonly createForm = this.fb.group({
    personType: ['', Validators.required],
    documentType: ['', Validators.required],
    documentNumber: ['', [Validators.required, Validators.maxLength(64)]],
    displayName: ['', [Validators.required, Validators.maxLength(200)]]
  });

  get canManage(): boolean {
    return this.auth.hasPermission('CanManageAch');
  }

  get customerDisplayName(): string {
    if (!this.customer) return `Cliente ${this.customerId}`;
    if (this.customer.companyName?.trim()) return this.customer.companyName.trim();
    return [this.customer.firstName, this.customer.middleName, this.customer.lastName, this.customer.secondLastName]
      .filter((part) => !!part?.trim())
      .join(' ') || `Cliente ${this.customerId}`;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  ngOnInit(): void {
    this.customerId = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(this.customerId) || this.customerId <= 0) {
      void this.router.navigate(['/customers']);
      return;
    }

    this.loadCustomer();
    this.load();
  }

  load(page = 1): void {
    this.loading = true;
    this.loadError = '';
    this.page = page;
    const raw = this.filters.getRawValue();
    this.api.listThirdParties(this.customerId, {
      search: raw.search ?? '',
      status: raw.status,
      page,
      pageSize: this.pageSize
    }).subscribe({
      next: (response) => {
        this.items = response.items ?? [];
        this.total = response.total ?? 0;
        this.page = response.page ?? page;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.items = [];
        this.total = 0;
        this.loading = false;
        this.loadError = this.errorMessage(error, 'No fue posible cargar los terceros del cliente.');
        this.cdr.markForCheck();
      }
    });
  }

  clearFilters(): void {
    this.filters.reset({ search: '', status: null });
    this.load(1);
  }

  toggleCreate(): void {
    if (!this.canManage) return;
    this.createOpen = !this.createOpen;
    this.commandError = '';
    if (!this.createOpen) this.createForm.reset({ personType: '', documentType: '', documentNumber: '', displayName: '' });
  }

  createThirdParty(): void {
    if (!this.canManage || this.saving) return;
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const raw = this.createForm.getRawValue();
    this.saving = true;
    this.commandError = '';
    this.api.createThirdParty(this.customerId, {
      personType: raw.personType!.trim(),
      documentType: raw.documentType!.trim(),
      documentNumber: raw.documentNumber!.trim(),
      displayName: raw.displayName!.trim()
    }).subscribe({
      next: (created) => {
        this.saving = false;
        this.createOpen = false;
        this.createForm.reset({ personType: '', documentType: '', documentNumber: '', displayName: '' });
        this.notifications.success('Tercero registrado.');
        this.load(1);
        void this.router.navigate(['/customers', this.customerId, 'third-parties', created.thirdPartyId]);
      },
      error: (error) => {
        this.saving = false;
        this.commandError = this.errorMessage(error, 'No fue posible registrar el tercero.');
        this.cdr.markForCheck();
      }
    });
  }

  openDetail(item: ThirdPartyAdministrationDto): void {
    void this.router.navigate(['/customers', this.customerId, 'third-parties', item.thirdPartyId]);
  }

  previousPage(): void {
    if (this.page > 1 && !this.loading) this.load(this.page - 1);
  }

  nextPage(): void {
    if (this.page < this.totalPages && !this.loading) this.load(this.page + 1);
  }

  private loadCustomer(): void {
    this.customersApi.getById(this.customerId).subscribe({
      next: (customer) => {
        this.customer = customer;
        this.cdr.markForCheck();
      },
      error: () => this.cdr.markForCheck()
    });
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && error.status === 403) {
      return 'No tienes permisos para consultar o modificar esta información.';
    }
    if (error instanceof HttpErrorResponse && error.status === 409) {
      return getHttpErrorMessage(error, 'El registro entra en conflicto con una identidad existente.');
    }
    return getHttpErrorMessage(error, fallback);
  }
}
