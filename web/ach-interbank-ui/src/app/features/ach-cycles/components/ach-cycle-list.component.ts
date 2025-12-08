import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { AchCyclesApiService, ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { AchCycleFilter, AchCycleSummary, ClearingHouseOption } from '../models/ach-cycle.model';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-ach-cycle-list',
  templateUrl: './ach-cycle-list.component.html',
  styleUrls: ['./ach-cycle-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class AchCycleListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AchCyclesApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

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

  load(): void {
    const filter: AchCycleFilter = {
      ...this.filterForm.value,
      clearingHouseId: this.filterForm.value.clearingHouseId ?? undefined
    };
    this.loading = true;
    this.api.search(filter).subscribe({
      next: (response) => {
        const items = response?.items ?? [];
        const formatter = new Intl.DateTimeFormat('es-CO', {
          timeZone: 'America/Bogota',
          year: 'numeric',
          month: '2-digit',
          day: '2-digit'
        });

        this.cycles = items.map((cycle) => ({
          ...cycle,
          dateText: cycle.date ? formatter.format(new Date(cycle.date)) : '-',
          startText: cycle.startTime,
          endText: cycle.endTime
        }));
        this.total = response?.total ?? 0;
        this.loading = false;
      },
      error: () => {
        this.notifications.error('No fue posible cargar los ciclos ACH');
        this.loading = false;
      }
    });
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
