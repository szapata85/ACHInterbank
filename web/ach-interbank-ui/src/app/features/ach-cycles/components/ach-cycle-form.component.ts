import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AchCyclesApiService, ClearingHousesApiService } from '../services/ach-cycles-api.service';
import {
  AchCycleConfigurationOption,
  AchCycleSummary,
  ClearingHouseOption,
  SaveAchCycleRequest
} from '../models/ach-cycle.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-ach-cycle-form',
  templateUrl: './ach-cycle-form.component.html',
  styleUrls: ['./ach-cycle-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class AchCycleFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(AchCyclesApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  clearingHouses: ClearingHouseOption[] = [];
  configurations: AchCycleConfigurationOption[] = [];
  isEdit = false;
  cycleId: string | null = null;
  isLoadingConfigurations = false;
  isSaving = false;
  errorMessage: string | null = null;

  readonly form = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    processingDate: ['', Validators.required],
    clearingHouseCycleConfigId: [null as number | null, Validators.required],
    rescheduleOnHoliday: [false]
  });

  ngOnInit(): void {
    this.clearingHouseApi.list().subscribe((items) => {
      this.clearingHouses = items;
      this.cdr.markForCheck();
    });

    this.cycleId = this.route.snapshot.paramMap.get('id');
    if (this.cycleId) {
      this.isEdit = true;
      this.api.getById(this.cycleId).subscribe((cycle) => {
        this.patch(cycle);
        this.loadConfigurations(cycle.clearingHouseCycleConfigId);
      });
    }
  }

  private patch(cycle: AchCycleSummary): void {
    this.form.patchValue({
      clearingHouseId: cycle.clearingHouseId,
      processingDate: (cycle.processingDate ?? cycle.date ?? '').slice(0, 10),
      clearingHouseCycleConfigId: cycle.clearingHouseCycleConfigId,
      rescheduleOnHoliday: cycle.rescheduleOnHoliday
    });
  }

  configurationContextChanged(): void {
    this.loadConfigurations();
  }

  configurationLabel(config: AchCycleConfigurationOption): string {
    const validity = config.effectiveTo
      ? `${config.effectiveFrom.slice(0, 10)} a ${config.effectiveTo.slice(0, 10)}`
      : `desde ${config.effectiveFrom.slice(0, 10)}`;
    return `${config.cycleName} · ${config.startTime.slice(0, 5)}–${config.endTime.slice(0, 5)} · corte ${config.cutoffTime.slice(0, 5)} · vigente ${validity}`;
  }

  private loadConfigurations(selectedId: number | null = null): void {
    const clearingHouseId = this.form.controls.clearingHouseId.value;
    const processingDate = this.form.controls.processingDate.value;
    this.errorMessage = null;

    if (!clearingHouseId || !processingDate) {
      this.configurations = [];
      this.form.controls.clearingHouseCycleConfigId.setValue(null);
      this.cdr.markForCheck();
      return;
    }

    this.isLoadingConfigurations = true;
    this.api.getCurrentConfigurations(clearingHouseId, processingDate).subscribe({
      next: (items) => {
        this.configurations = items.filter((item) => item.isActive && item.isCurrent);
        const requestedId = selectedId ?? this.form.controls.clearingHouseCycleConfigId.value;
        this.form.controls.clearingHouseCycleConfigId.setValue(
          this.configurations.some((item) => item.id === requestedId) ? requestedId : null
        );
        this.isLoadingConfigurations = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.configurations = [];
        this.form.controls.clearingHouseCycleConfigId.setValue(null);
        this.errorMessage = this.toErrorMessage(error, 'No fue posible cargar las configuraciones de ciclo.');
        this.isLoadingConfigurations = false;
        this.cdr.markForCheck();
      }
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.isSaving) {
      return;
    }

    const value = this.form.getRawValue();
    const configuration = this.configurations.find((item) => item.id === value.clearingHouseCycleConfigId);
    if (!configuration || !value.clearingHouseId || !value.processingDate) {
      this.errorMessage = 'Seleccione una configuración de ciclo vigente para la cámara y fecha indicadas.';
      return;
    }

    const payload: SaveAchCycleRequest = {
      clearingHouseId: value.clearingHouseId,
      clearingHouseCycleConfigId: configuration.id,
      cycleName: configuration.cycleName,
      processingDate: value.processingDate,
      startTime: configuration.startTime,
      endTime: configuration.endTime,
      cutoffTime: configuration.cutoffTime,
      rescheduleOnHoliday: value.rescheduleOnHoliday ?? false
    };
    const request$ = this.isEdit && this.cycleId
      ? this.api.update(this.cycleId, payload)
      : this.api.create(payload);

    this.isSaving = true;
    this.errorMessage = null;
    request$.subscribe({
      next: () => this.router.navigate(['/ach-cycles']),
      error: (error) => {
        this.errorMessage = this.toErrorMessage(error, 'No fue posible guardar el ciclo.');
        this.isSaving = false;
        this.cdr.markForCheck();
      }
    });
  }

  private toErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof Error && error.message && error.message !== '[object Object]') {
      return error.message;
    }

    if (typeof error === 'object' && error !== null) {
      const response = error as { error?: { detail?: unknown; title?: unknown }; message?: unknown };
      const candidate = response.error?.detail ?? response.error?.title ?? response.message;
      if (typeof candidate === 'string' && candidate.trim() && candidate !== '[object Object]') {
        return candidate;
      }
    }

    return fallback;
  }
}
