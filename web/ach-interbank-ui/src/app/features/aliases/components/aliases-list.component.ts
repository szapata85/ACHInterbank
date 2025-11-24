import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { AliasesApiService } from '../services/aliases-api.service';
import { AliasFilter, AliasSummary } from '../models/alias.model';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-aliases-list',
  templateUrl: './aliases-list.component.html',
  styleUrls: ['./aliases-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AliasesListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AliasesApiService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  readonly filterForm = this.fb.group({
    search: [''],
    documentNumber: [''],
    phoneNumber: [''],
    page: [1],
    pageSize: [10]
  });

  aliases: AliasSummary[] = [];
  total = 0;
  loading = false;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const filter: AliasFilter = this.filterForm.value;
    this.loading = true;
    this.api.search(filter).subscribe({
      next: (response) => {
        this.aliases = response.items;
        this.total = response.total;
        this.loading = false;
      },
      error: () => {
        this.notifications.error('No fue posible cargar los alias registrados');
        this.loading = false;
      }
    });
  }

  changePage(page: number): void {
    this.filterForm.patchValue({ page });
    this.load();
  }

  create(): void {
    this.router.navigate(['/aliases/new']);
  }

  edit(item: AliasSummary): void {
    this.router.navigate(['/aliases', item.id, 'edit']);
  }
}
