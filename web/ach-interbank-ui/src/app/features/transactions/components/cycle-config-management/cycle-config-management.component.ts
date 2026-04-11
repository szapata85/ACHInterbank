import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import {
  ClearingHouseCycleConfigItem,
  CycleStatusFilter,
  CycleValidityFilter,
  UpsertCycleConfigRequest
} from '../../transactions.models';
import { ClearingHouseCycleConfigsApiService } from '../../services/clearing-house-cycle-configs-api.service';

@Component({
  selector: 'app-cycle-config-management',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './cycle-config-management.component.html',
  styleUrls: ['./cycle-config-management.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CycleConfigManagementComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cycleConfigApi = inject(ClearingHouseCycleConfigsApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly notifications = inject(NotificationService);

  loading = false;
  saving = false;
  showForm = false;
  hasSearched = false;
  editingSource: ClearingHouseCycleConfigItem | null = null;
  selectedForInactivation: ClearingHouseCycleConfigItem | null = null;

  allItems: ClearingHouseCycleConfigItem[] = [];
  visibleItems: ClearingHouseCycleConfigItem[] = [];
  clearingHouses: Array<{ id: number; name: string }> = [];

  readonly statusOptions: Array<{ value: CycleStatusFilter; label: string }> = [
    { value: 'all', label: 'Todos' },
    { value: 'active', label: 'Activas' },
    { value: 'inactive', label: 'Inactivas' }
  ];

  readonly validityOptions: Array<{ value: CycleValidityFilter; label: string }> = [
    { value: 'all', label: 'Todas' },
    { value: 'current', label: 'Vigentes' },
    { value: 'future', label: 'Futuras' },
    { value: 'expired', label: 'Vencidas' }
  ];

  readonly filterForm = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    cycleName: [''],
    status: ['all' as CycleStatusFilter],
    validity: ['all' as CycleValidityFilter],
    effectiveAt: [this.todayInputValue()]
  });

  readonly form = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    cycleName: ['', [Validators.required, Validators.maxLength(60)]],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
    cutoffTime: ['', Validators.required],
    effectiveFrom: [this.todayInputValue(), Validators.required]
  });

  ngOnInit(): void {
    this.loadClearingHouses();
  }

  get validationWarnings(): string[] {
    const warnings: string[] = [];
    const startTime = this.form.controls.startTime.value;
    const endTime = this.form.controls.endTime.value;
    const cutoffTime = this.form.controls.cutoffTime.value;

    if (startTime && endTime && startTime >= endTime) {
      warnings.push('La hora de inicio debe ser menor a la hora de fin.');
    }

    if (cutoffTime && endTime && cutoffTime > endTime) {
      warnings.push('La hora de cutoff no debe superar la hora de fin de la ventana operativa.');
    }

    if (!this.form.controls.effectiveFrom.value) {
      warnings.push('La vigencia desde es obligatoria.');
    }

    if (this.editingSource) {
      warnings.push('Guardar creará una nueva versión y mantendrá el histórico de configuraciones.');
    }

    return warnings;
  }

  openCreateForm(): void {
    this.showForm = true;
    this.editingSource = null;

    this.form.reset({
      clearingHouseId: this.filterForm.controls.clearingHouseId.value,
      cycleName: '',
      startTime: '',
      endTime: '',
      cutoffTime: '',
      effectiveFrom: this.todayInputValue()
    });
  }

  edit(item: ClearingHouseCycleConfigItem): void {
    this.showForm = true;
    this.editingSource = item;

    this.form.reset({
      clearingHouseId: item.clearingHouseId,
      cycleName: item.cycleName,
      startTime: this.toTimeInput(item.startTime),
      endTime: this.toTimeInput(item.endTime),
      cutoffTime: this.toTimeInput(item.cutoffTime),
      effectiveFrom: this.todayInputValue()
    });
  }

  clone(item: ClearingHouseCycleConfigItem): void {
    this.edit(item);
    this.form.patchValue({
      cycleName: `${item.cycleName}-V2`
    });
  }

  closeForm(): void {
    this.showForm = false;
    this.editingSource = null;
  }

  search(): void {
    this.filterForm.markAllAsTouched();
    if (this.filterForm.invalid) {
      return;
    }

    const clearingHouseId = Number(this.filterForm.controls.clearingHouseId.value);
    if (!clearingHouseId) {
      return;
    }

    this.loading = true;
    this.hasSearched = true;

    this.cycleConfigApi
      .getByClearingHouse({
        clearingHouseId,
        effectiveAt: this.filterForm.controls.effectiveAt.value || null
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.allItems = items;
          this.applyLocalFilters();
        },
        error: () => {
          this.notifications.error('No fue posible consultar configuraciones de ciclos.');
          this.allItems = [];
          this.visibleItems = [];
        }
      });
  }

  applyLocalFilters(): void {
    const cycleName = (this.filterForm.controls.cycleName.value ?? '').trim().toLowerCase();
    const status = this.filterForm.controls.status.value ?? 'all';
    const validity = this.filterForm.controls.validity.value ?? 'all';
    const effectiveAt = this.referenceDate();

    this.visibleItems = this.allItems.filter((item) => {
      const matchesName = !cycleName || item.cycleName.toLowerCase().includes(cycleName);
      const matchesStatus =
        status === 'all' || (status === 'active' ? item.isActive : !item.isActive);
      const itemState = this.resolveValidity(item, effectiveAt);
      const matchesValidity = validity === 'all' || itemState === validity;

      return matchesName && matchesStatus && matchesValidity;
    });
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.validationWarnings.some((x) => x.includes('debe') || x.includes('obligatoria'))) {
      this.notifications.warning('Revise las validaciones antes de guardar.');
      return;
    }

    const payload: UpsertCycleConfigRequest = {
      clearingHouseId: Number(this.form.controls.clearingHouseId.value),
      cycleName: (this.form.controls.cycleName.value ?? '').trim(),
      startTime: this.toApiTime(this.form.controls.startTime.value ?? ''),
      endTime: this.toApiTime(this.form.controls.endTime.value ?? ''),
      cutoffTime: this.toApiTime(this.form.controls.cutoffTime.value ?? ''),
      effectiveFrom: `${this.form.controls.effectiveFrom.value}T00:00:00Z`
    };

    this.saving = true;
    this.cycleConfigApi
      .createVersion(payload)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.notifications.success('Configuración versionada correctamente.');
          this.closeForm();
          this.search();
        },
        error: () => {
          this.notifications.error('No fue posible guardar la configuración de ciclo.');
        }
      });
  }

  askInactivate(item: ClearingHouseCycleConfigItem): void {
    this.selectedForInactivation = item;
  }

  cancelInactivate(): void {
    this.selectedForInactivation = null;
  }

  confirmInactivate(): void {
    if (!this.selectedForInactivation) {
      return;
    }

    const effectiveTo = `${this.todayInputValue()}T00:00:00Z`;
    const id = this.selectedForInactivation.id;

    this.cycleConfigApi.inactivate(id, { effectiveTo }).subscribe({
      next: () => {
        this.notifications.success('Configuración inactivada correctamente.');
        this.selectedForInactivation = null;
        this.search();
      },
      error: () => {
        this.notifications.error('No fue posible inactivar la configuración.');
        this.selectedForInactivation = null;
      }
    });
  }

  statusBadge(item: ClearingHouseCycleConfigItem): { text: string; css: string } {
    const validity = this.resolveValidity(item, this.referenceDate());

    if (!item.isActive) {
      return { text: 'Inactiva', css: 'inactive' };
    }

    if (validity === 'current') {
      return { text: 'Vigente', css: 'current' };
    }

    if (validity === 'future') {
      return { text: 'Futura', css: 'future' };
    }

    return { text: 'Vencida', css: 'expired' };
  }

  private loadClearingHouses(): void {
    this.clearingHouseApi.list().subscribe({
      next: (items) => {
        this.clearingHouses = items.map((x) => ({ id: x.id, name: x.name }));
      },
      error: () => {
        this.notifications.error('No fue posible cargar cámaras de compensación.');
      }
    });
  }

  private resolveValidity(item: ClearingHouseCycleConfigItem, referenceDate: Date): CycleValidityFilter {
    const from = new Date(item.effectiveFrom);
    const to = item.effectiveTo ? new Date(item.effectiveTo) : null;

    if (from > referenceDate) {
      return 'future';
    }

    if (to && to < referenceDate) {
      return 'expired';
    }

    return 'current';
  }

  private referenceDate(): Date {
    const effectiveAt = this.filterForm.controls.effectiveAt.value;
    return effectiveAt ? new Date(`${effectiveAt}T00:00:00Z`) : new Date();
  }

  private toTimeInput(value: string): string {
    return value?.slice(0, 5) ?? '';
  }

  private toApiTime(value: string): string {
    return value.length === 5 ? `${value}:00` : value;
  }

  private todayInputValue(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
