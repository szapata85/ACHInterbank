import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { AliasesApiService } from '../services/aliases-api.service';
import { AliasFilter, AliasSummary } from '../models/alias.model';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-aliases-list',
  templateUrl: './aliases-list.component.html',
  styleUrls: ['./aliases-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class AliasesListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AliasesApiService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  // Exponer Math para cálculos de paginación en la plantilla
  readonly math = Math;

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
  hasLoaded = false;
  loadError: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const filter: AliasFilter = this.filterForm.value;
    this.loading = true;
    this.loadError = null;
    this.cdr.markForCheck();
    this.api.search(filter).subscribe({
      next: (response) => {
        this.aliases = response.items;
        this.total = response.total;
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadError = 'No fue posible cargar los alias registrados';
        this.notifications.error(this.loadError);
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      }
    });
  }

  changePage(page: number): void {
    const nextPage = Math.max(1, page);
    this.filterForm.patchValue({ page: nextPage });
    this.load();
  }

  create(): void {
    this.router.navigate(['/aliases/new']);
  }

  edit(item: AliasSummary): void {
    this.router.navigate(['/aliases', item.id, 'edit']);
  }
}
