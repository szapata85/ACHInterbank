import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { CustomersApiService } from '../services/customers-api.service';
import { CustomerSummary } from '../models/customer.model';
import { NotificationService } from '../../../core/services/notification.service';
import { FormBuilder } from '@angular/forms';
import { ColDef } from 'ag-grid-community';

@Component({
  selector: 'app-customers-list',
  templateUrl: './customers-list.component.html',
  styleUrls: ['./customers-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class CustomersListComponent implements OnInit {
  private readonly api = inject(CustomersApiService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder);

  customers: CustomerSummary[] = [];
  filteredCustomers: CustomerSummary[] = [];
  loading = false;

  readonly columnas: ColDef[] = [
    { field: 'documento', headerName: 'Documento', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'nombre', headerName: 'Nombre', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'cuenta', headerName: 'Cuenta', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'tipoPersona', headerName: 'Tipo persona', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'razonSocial', headerName: 'Razón social', sortable: true, filter: 'agTextColumnFilter' },
    {
      field: 'acciones',
      headerName: 'Acciones',
      sortable: false,
      filter: false,
      cellRenderer: (params: any) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.classList.add('link');
        button.innerText = 'Editar';
        button.addEventListener('click', () => this.edit(params.data._original));
        return button;
      },
      maxWidth: 140
    }
  ];

  get filasGrilla(): any[] {
    return this.filteredCustomers.map((customer) => ({
      documento: `${customer.documentType} ${customer.documentNumber}`.trim(),
      nombre: customer.fullName,
      cuenta: (customer.accountNumbers?.length ? customer.accountNumbers.join(', ') : customer.accountNumber) || '-',
      tipoPersona: customer.personType,
      razonSocial: customer.companyName || '-',
      acciones: 'Editar',
      _original: customer
    }));
  }

  readonly filtersForm = this.fb.group({
    document: [''],
    name: [''],
    account: [''],
    personType: ['']
  });

  ngOnInit(): void {
    this.load();
    this.filtersForm.valueChanges.subscribe(() => this.applyFilters());
  }

  load(): void {
    this.loading = true;
    this.cdr.markForCheck();

    this.api.getAll().subscribe({
      next: (items) => {
        this.customers = items ?? [];
        this.applyFilters();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar los clientes registrados');
        this.customers = [];
        this.filteredCustomers = [];
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  resetFilters(): void {
    this.filtersForm.reset({ document: '', name: '', account: '', personType: '' });
  }

  create(): void {
    this.router.navigate(['/customers/new']);
  }

  edit(customer: CustomerSummary): void {
    this.router.navigate(['/customers', customer.id, 'edit']);
  }

  private applyFilters(): void {
    const { document, name, account, personType } = this.filtersForm.getRawValue();

    const documentTerm = (document ?? '').trim().toLowerCase();
    const nameTerm = (name ?? '').trim().toLowerCase();
    const accountTerm = (account ?? '').trim().toLowerCase();
    const personTypeTerm = (personType ?? '').trim();

    this.filteredCustomers = this.customers.filter((customer) => {
      const documentValue = `${customer.documentType} ${customer.documentNumber}`.toLowerCase();
      const accountValue = ((customer.accountNumbers?.length ? customer.accountNumbers.join(' ') : customer.accountNumber) ?? '').toLowerCase();
      const nameValue = (customer.fullName ?? '').toLowerCase();

      if (documentTerm && !documentValue.includes(documentTerm)) {
        return false;
      }

      if (nameTerm && !nameValue.includes(nameTerm)) {
        return false;
      }

      if (accountTerm && !accountValue.includes(accountTerm)) {
        return false;
      }

      if (personTypeTerm && customer.personType !== personTypeTerm) {
        return false;
      }

      return true;
    });

    this.cdr.markForCheck();
  }
}
