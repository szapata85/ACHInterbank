import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { CustomersApiService } from '../services/customers-api.service';
import { CustomerSummary } from '../models/customer.model';
import { NotificationService } from '../../../core/services/notification.service';

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

  customers: CustomerSummary[] = [];
  loading = false;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.getAll().subscribe({
      next: (items) => {
        this.customers = items;
        this.loading = false;
      },
      error: () => {
        this.notifications.error('No fue posible cargar los clientes registrados');
        this.loading = false;
      }
    });
  }

  create(): void {
    this.router.navigate(['/customers/new']);
  }

  edit(customer: CustomerSummary): void {
    this.router.navigate(['/customers', customer.id, 'edit']);
  }
}
