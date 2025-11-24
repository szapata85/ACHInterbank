import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { AchCyclesApiService, ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { AchCycleFilter, AchCycleSummary, ClearingHouseOption } from '../models/ach-cycle.model';

@Component({
  selector: 'app-ach-cycle-list',
  templateUrl: './ach-cycle-list.component.html',
  styleUrls: ['./ach-cycle-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchCycleListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AchCyclesApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly router = inject(Router);

  readonly filterForm = this.fb.group({
    clearingHouseId: [''],
    date: [''],
    page: [1],
    pageSize: [10]
  });

  cycles: AchCycleSummary[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  total = 0;
  today = new Date();

  ngOnInit(): void {
    this.clearingHouseApi.list().subscribe((items) => (this.clearingHouses = items));
    this.load();
  }

  load(): void {
    const filter: AchCycleFilter = this.filterForm.value;
    this.api.search(filter).subscribe((response) => {
      const formatter = new Intl.DateTimeFormat('es-CO', {
        timeZone: 'America/Bogota',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      });

      this.cycles = response.items.map((cycle) => ({
        ...cycle,
        dateText: cycle.date ? formatter.format(new Date(cycle.date)) : '-',
        startText: cycle.startTime,
        endText: cycle.endTime
      }));
      this.total = response.total;
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
}
