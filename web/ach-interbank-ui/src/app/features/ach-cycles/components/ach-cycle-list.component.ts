import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import { AchCyclesApiService } from '../../../core/services/ach-cycles-api.service';
import { ClearingHouseCycleConfigsApiService } from '../../../core/services/clearing-house-cycle-configs-api.service';
import { ClearingHouseCycleConfigItem } from '../../../core/models/clearing-house-cycle-config.model';
import { AchCycleFilter, AchCycleSummary } from '../../../core/models/ach-cycle.model';
import { ClearingHouseOption } from '../../../core/models/clearing-house.model';
import { ClearingHousesApiService } from '../../../core/services/clearing-houses-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { finalize, forkJoin, map, Subscription } from 'rxjs';

@Component({
  selector: 'app-ach-cycle-list',
  templateUrl: './ach-cycle-list.component.html',
  styleUrls: ['./ach-cycle-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule]
})
export class AchCycleListComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AchCyclesApiService);
  private readonly configApi = inject(ClearingHouseCycleConfigsApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly dateFormatter = new Intl.DateTimeFormat('es-CO', {
    timeZone: 'America/Bogota',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  });
  private requestSub?: Subscription;
  private configSub?: Subscription;

  readonly filterForm = this.fb.group({
    clearingHouseId: [null as number | null],
    startDate: [''],
    endDate: [''],
    page: [1],
    pageSize: [10]
  });

  cycles: AchCycleSummary[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  currentConfigs: ClearingHouseCycleConfigItem[] = [];
  configLoading = false;
  configError = false;
  total = 0;
  today = new Date();
  loading = false;
  loadError = false;

  ngOnInit(): void {
    this.loadClearingHouses();
    if (!this.route.snapshot.queryParamMap.get('clearingHouseCode')) {
      this.load();
    }
  }

  ngOnDestroy(): void {
    this.requestSub?.unsubscribe();
    this.configSub?.unsubscribe();
  }

  submit(): void {
    this.filterForm.patchValue({ page: 1 });
    this.load();
    this.loadCurrentConfigurations();
  }

  load(): void {
    this.requestSub?.unsubscribe();
    const filter: AchCycleFilter = {
      ...this.filterForm.value,
      clearingHouseId: this.filterForm.value.clearingHouseId ?? undefined
    };
    this.loading = true;
    this.loadError = false;
    this.cdr.markForCheck();
    this.requestSub = this.api
      .search(filter)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
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
          this.cdr.markForCheck();
        },
        error: () => {
          this.loadError = true;
          this.notifications.error('No fue posible cargar los ciclos ACH');
          this.cdr.markForCheck();
        }
      });
  }

  private loadClearingHouses(): void {
    this.clearingHouseApi.list().subscribe({
      next: (items) => {
        this.clearingHouses = items;
        const requestedCode = this.route.snapshot.queryParamMap.get('clearingHouseCode');
        if (requestedCode) {
          const selected = items.find((item) =>
            item.code?.toUpperCase() === requestedCode.toUpperCase() ||
            item.name.replace(/\s+/g, '').toUpperCase().includes(requestedCode.replace(/\s+/g, '').toUpperCase()));
          this.filterForm.patchValue({ clearingHouseId: selected?.id ?? null, page: 1 });
          this.load();
        }
        this.loadCurrentConfigurations();
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar las cámaras compensadoras');
        this.cdr.markForCheck();
      }
    });
  }

  // The regulatory seed populates ClearingHouseCycleConfigs, not dated AchCycles.
  // Display current configuration separately, so the screen is useful before the
  // scheduler creates operational instances for a particular processing day.
  private loadCurrentConfigurations(): void {
    this.configSub?.unsubscribe();
    const selectedId = this.filterForm.controls.clearingHouseId.value;
    const houses = this.clearingHouses.filter(house => !selectedId || house.id === selectedId);
    this.currentConfigs = [];
    this.configError = false;
    if (!houses.length) {
      this.configLoading = false;
      this.cdr.markForCheck();
      return;
    }

    this.configLoading = true;
    this.cdr.markForCheck();
    this.configSub = forkJoin(houses.map(house =>
      this.configApi.getCurrentByClearingHouse(house.id).pipe(
        map(configs => configs.map(config => ({
          ...config,
          clearingHouseName: config.clearingHouseName || house.name
        })))
      )
    )).pipe(finalize(() => {
      this.configLoading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: grouped => {
        this.currentConfigs = grouped.flat().sort((a, b) =>
          (a.clearingHouseName || '').localeCompare(b.clearingHouseName || '') ||
          this.cycleNumber(a.cycleName) - this.cycleNumber(b.cycleName));
        this.cdr.markForCheck();
      },
      error: () => {
        this.configError = true;
        this.notifications.error('No fue posible consultar las configuraciones vigentes de ciclos');
        this.cdr.markForCheck();
      }
    });
  }

  private cycleNumber(value: string): number {
    return Number(value.match(/\d+/)?.[0] ?? 0);
  }

  configDate(value: string): string {
    return value?.slice(0, 10) ?? '-';
  }

  configTime(value: string | null | undefined): string {
    return value?.slice(0, 5) ?? '-';
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
