import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ColDef } from 'ag-grid-community';
import { SharedModule } from '../../../shared/shared.module';
import { CustomersApiService } from '../../../core/services/customers-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { CustomerSummary } from '../../../core/models/customer.model';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-customers-list',
  templateUrl: './customers-list.component.html',
  styleUrls: ['./customers-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    SharedModule, RouterModule, MatButtonModule, MatCardModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatProgressBarModule, MatSelectModule
  ]
})
export class CustomersListComponent implements OnInit {
  private readonly api = inject(CustomersApiService);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly canManage = this.auth.hasPermission('CanManageAch');
  customers: CustomerSummary[] = [];
  filteredCustomers: CustomerSummary[] = [];
  loading = false;
  loadError = false;

  readonly columnas: ColDef[] = [
    { field: 'documento', headerName: 'Documento', sortable: true, filter: 'agTextColumnFilter', minWidth: 155 },
    { field: 'nombre', headerName: 'Nombre o razón social', sortable: true, filter: 'agTextColumnFilter', minWidth: 180, flex: 1 },
    { field: 'cuenta', headerName: 'Cuenta(s)', sortable: true, filter: 'agTextColumnFilter', minWidth: 170 },
    { field: 'tipoPersona', headerName: 'Tipo de persona', sortable: true, filter: 'agTextColumnFilter', minWidth: 150 },
    {
      field: 'acciones',
      headerName: 'Acciones',
      sortable: false,
      filter: false,
      floatingFilter: false,
      cellRenderer: (params: any) => {
        const actions = document.createElement('div');
        actions.classList.add('grid-actions');
        const customer = params.data?._original as CustomerSummary | undefined;
        if (!customer) return actions;

        const thirdPartiesButton = document.createElement('button');
        thirdPartiesButton.type = 'button';
        thirdPartiesButton.classList.add('link');
        thirdPartiesButton.innerText = 'Ver terceros';
        thirdPartiesButton.setAttribute('aria-label', `Ver terceros del cliente ${customer.documentNumber}`);
        thirdPartiesButton.addEventListener('click', () => this.manageThirdParties(customer));
        actions.append(thirdPartiesButton);

        if (this.canManage) {
          const editButton = document.createElement('button');
          editButton.type = 'button';
          editButton.classList.add('link');
          editButton.innerText = 'Editar';
          editButton.setAttribute('aria-label', `Editar cliente ${customer.documentNumber}`);
          editButton.addEventListener('click', () => this.edit(customer));
          actions.append(editButton);
        }
        return actions;
      },
      minWidth: this.canManage ? 210 : 135,
      maxWidth: this.canManage ? 240 : 160
    }
  ];

  get filasGrilla(): any[] {
    return this.filteredCustomers.map(customer => ({
      documento: `${customer.documentType} ${customer.documentNumber}`.trim(),
      nombre: customer.companyName || customer.fullName || 'Sin nombre registrado',
      cuenta: (customer.accountNumbers?.length ? customer.accountNumbers.join(', ') : customer.accountNumber) || 'Sin cuenta',
      tipoPersona: customer.personType === 'PJ' ? 'Persona jurídica' : customer.personType === 'PN' ? 'Persona natural' : customer.personType,
      acciones: 'Acciones',
      _original: customer
    }));
  }

  readonly filtersForm = this.fb.group({
    document: [''],
    name: [''],
    account: [''],
    personType: ['']
  });

  get hasActiveFilters(): boolean {
    return Object.values(this.filtersForm.getRawValue()).some(value => !!String(value ?? '').trim());
  }

  ngOnInit(): void {
    this.load();
    this.filtersForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.applyFilters());
  }

  load(): void {
    if (this.loading) return;
    this.loading = true;
    this.loadError = false;
    this.cdr.markForCheck();

    this.api.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => {
        this.customers = items ?? [];
        this.applyFilters();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No pudimos consultar los clientes. Inténtalo nuevamente.');
        this.customers = [];
        this.filteredCustomers = [];
        this.loadError = true;
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  resetFilters(): void {
    this.filtersForm.reset({ document: '', name: '', account: '', personType: '' });
  }

  create(): void {
    if (this.canManage) void this.router.navigate(['/customers/new']);
  }

  manageThirdParties(customer: CustomerSummary): void {
    void this.router.navigate(['/customers', customer.id, 'third-parties']);
  }

  edit(customer: CustomerSummary): void {
    if (this.canManage) void this.router.navigate(['/customers', customer.id, 'edit']);
  }

  private applyFilters(): void {
    const { document, name, account, personType } = this.filtersForm.getRawValue();
    const documentTerm = (document ?? '').trim().toLocaleLowerCase('es');
    const nameTerm = (name ?? '').trim().toLocaleLowerCase('es');
    const accountTerm = (account ?? '').trim().toLocaleLowerCase('es');
    const personTypeTerm = (personType ?? '').trim();

    this.filteredCustomers = this.customers.filter(customer => {
      const documentValue = `${customer.documentType} ${customer.documentNumber}`.toLocaleLowerCase('es');
      const accountValue = (customer.accountNumbers?.length ? customer.accountNumbers.join(' ') : customer.accountNumber ?? '').toLocaleLowerCase('es');
      const nameValue = `${customer.fullName ?? ''} ${customer.companyName ?? ''}`.toLocaleLowerCase('es');
      return (!documentTerm || documentValue.includes(documentTerm))
        && (!nameTerm || nameValue.includes(nameTerm))
        && (!accountTerm || accountValue.includes(accountTerm))
        && (!personTypeTerm || customer.personType === personTypeTerm);
    });
    this.cdr.markForCheck();
  }
}
