import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { AchCyclesApiService, ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { AchCycleFilter, AchCycleSummary, ClearingHouseOption } from '../models/ach-cycle.model';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

@Component({
  selector: 'app-ach-cycle-list',
  templateUrl: './ach-cycle-list.component.html',
  styleUrls: ['./ach-cycle-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class AchCycleListComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AchCyclesApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly dateFormatter = new Intl.DateTimeFormat('es-CO', {
    timeZone: 'America/Bogota',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  });
  private requestSub?: Subscription;

  readonly filterForm = this.fb.group({
    clearingHouseId: [null as number | null],
    date: [''],
    page: [1],
    pageSize: [10]
  });

  cycles: AchCycleSummary[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  total = 0;
  today = new Date();
  loading = false;

  ngOnInit(): void {
    this.clearingHouseApi.list().subscribe((items) => (this.clearingHouses = items));
  }

  ngOnDestroy(): void {
    this.requestSub?.unsubscribe();
  }

  submit(): void {
    this.filterForm.patchValue({ page: 1 });
    this.load();
  }

  load(): void {
    this.requestSub?.unsubscribe();
    const filter: AchCycleFilter = {
      ...this.filterForm.value,
      clearingHouseId: this.filterForm.value.clearingHouseId ?? undefined
    };
    this.loading = true;
    this.requestSub = this.api
      .search(filter)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (response) => {
          const items = response?.items ?? [];
          this.cycles = items.map((cycle) => ({
            ...cycle,
            dateText: this.formatDate(cycle.date),
            startText: this.formatTime(cycle.startTime),
            endText: this.formatTime(cycle.endTime),
            statusText: this.formatStatus(cycle.status)
          }));
          this.total = response?.total ?? 0;
        },
        error: () => {
          this.notifications.error('No fue posible cargar los ciclos ACH');
        }
      });
  }

  private formatDate(date: string | null | undefined): string {
    if (!date) {
      return '-';
    }

    const [year, month, day] = date.split('-').map((part) => Number(part));
    if (!year || !month || !day) {
      return date;
    }

    return this.dateFormatter.format(new Date(year, month - 1, day));
  }

  private formatTime(time: string | null | undefined): string {
    if (!time) {
      return '-';
    }

    const [hours, minutes] = time.split(':');
    return hours && minutes ? `${hours}:${minutes}` : time;
  }

  private formatStatus(status: string | null | undefined): string {
    if (!status) {
      return '-';
    }

    const normalized = status.toLowerCase();

    if (normalized === 'activo' || normalized === 'active') {
      return 'Activo';
    }

    if (normalized === 'inactivo' || normalized === 'inactive') {
      return 'Inactivo';
    }

    return status;
  }

  changePage(page: number): void {
    this.filterForm.patchValue({ page });
    this.load();
  }

  create(): void {
    this.router.navigate(['/ach-cycles/new']);
  }

  edit(item: AchCycleSummary): void {
    this.router.navigate(['/ach-cycles', item.id, 'edit']);
  }

  goToExport(): void {
    this.router.navigate(['/ach-cycles/nacha/export']);
  }
}
