import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, HostListener, OnInit, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ClearingHouse, ClearingHouseInput, NachaProfileOption, PaymentRailOption } from './clearing-houses.models';
import { ClearingHousesService } from './clearing-houses.service';

@Component({
  selector: 'app-clearing-houses',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './clearing-houses.component.html',
  styleUrl: './clearing-houses.component.scss'
})
export class ClearingHousesComponent implements OnInit {
  private readonly api = inject(ClearingHousesService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  rows: ClearingHouse[] = [];
  profiles: NachaProfileOption[] = [];
  paymentRailOptions: PaymentRailOption[] = [];
  selected?: ClearingHouse;
  loading = false;
  saving = false;
  error = '';
  message = '';
  search = '';
  state: 'all' | 'active' | 'inactive' = 'all';
  page = 1;
  totalPages = 1;
  editing = false;
  readonly canCreate = this.auth.hasPermission('ClearingHouses.Create');
  readonly canUpdate = this.auth.hasPermission('ClearingHouses.Update');
  readonly canStatus = this.auth.hasPermission('ClearingHouses.ChangeStatus');
  readonly canCycles = this.auth.hasPermission('ClearingHouses.ManageCycles');
  readonly canSpecialDates = this.auth.hasPermission('ClearingHouses.ManageSpecialDates');

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^[A-Z0-9][A-Z0-9_-]{1,19}$/)]],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    originCode: ['', [Validators.required, Validators.maxLength(20)]],
    timeZoneId: ['America/Bogota', Validators.required],
    holidayStrategy: ['Colombian', Validators.required],
    paymentRailCode: [null as string | null],
    requiresNachaProfile: [false],
    nachaProfileId: [null as number | null]
  });

  ngOnInit(): void { this.loadPaymentRailOptions(); this.load(); }

  loadPaymentRailOptions(): void {
    this.api.paymentRailOptions().subscribe({
      next: options => {
        this.paymentRailOptions = options.filter(option => option.code.toUpperCase() !== 'UNKNOWN');
        this.cdr.markForCheck();
      },
      error: error => { this.error = this.errorText(error); this.cdr.markForCheck(); }
    });
  }

  load(): void {
    this.loading = true; this.error = '';
    const active = this.state === 'all' ? null : this.state === 'active';
    this.api.list(this.search, active, this.page).pipe(finalize(() => { this.loading = false; this.cdr.markForCheck(); })).subscribe({
      next: (result) => { this.rows = result.items; this.totalPages = Math.max(1, result.totalPages); this.cdr.markForCheck(); },
      error: (error) => { this.error = this.errorText(error); this.cdr.markForCheck(); }
    });
  }

  applyFilters(): void { this.page = 1; this.load(); }
  normalizeCode(): void { this.form.controls.code.setValue(this.form.controls.code.value.trim().toUpperCase()); }

  create(): void {
    this.selected = undefined; this.editing = true; this.message = ''; this.error = '';
    this.form.reset({ code: '', name: '', originCode: '', timeZoneId: 'America/Bogota', holidayStrategy: 'Colombian', paymentRailCode: null, requiresNachaProfile: false, nachaProfileId: null });
  }

  edit(row: ClearingHouse): void {
    this.selected = row; this.editing = true; this.message = ''; this.error = '';
    this.form.reset({ code: row.code, name: row.name, originCode: row.originCode, timeZoneId: row.timeZoneId,
      holidayStrategy: row.holidayStrategy, paymentRailCode: row.paymentRailCode ?? null,
      requiresNachaProfile: row.requiresNachaProfile, nachaProfileId: row.nachaProfileId ?? null });
    this.loadProfiles();
  }

  view(row: ClearingHouse): void { this.selected = row; this.editing = false; this.error = ''; }
  loadProfiles(): void { this.normalizeCode(); this.api.profiles(this.form.controls.code.value).subscribe({ next: p => { this.profiles = p; this.cdr.markForCheck(); } }); }

  save(): void {
    this.normalizeCode();
    if (this.form.invalid || this.saving) { this.form.markAllAsTouched(); return; }
    this.saving = true; this.error = ''; this.message = '';
    const input: ClearingHouseInput = { ...this.form.getRawValue(), expectedUpdatedAt: this.selected?.updatedAt };
    const request = this.selected ? this.api.update(this.selected.id, input) : this.api.create(input);
    request.pipe(finalize(() => { this.saving = false; this.cdr.markForCheck(); })).subscribe({
      next: (row) => { this.selected = row; this.editing = false; this.message = 'Cámara compensadora guardada correctamente.'; this.load(); },
      error: (error) => { this.error = this.errorText(error); this.cdr.markForCheck(); }
    });
  }

  cancel(): void {
    if (!this.form.dirty || window.confirm('Hay cambios sin guardar. ¿Desea salir?')) this.editing = false;
  }

  changeStatus(row: ClearingHouse): void {
    if (row.isActive && !window.confirm(`¿Desea desactivar ${row.code}?`)) return;
    this.api.changeStatus(row.id, !row.isActive).subscribe({
      next: (updated) => { this.selected = updated; this.message = updated.isActive ? 'Cámara activada.' : 'Cámara desactivada.'; this.load(); },
      error: (error) => { this.error = this.errorText(error); this.cdr.markForCheck(); }
    });
  }

  previous(): void { if (this.page > 1) { this.page--; this.load(); } }
  next(): void { if (this.page < this.totalPages) { this.page++; this.load(); } }

  @HostListener('window:beforeunload', ['$event'])
  protectChanges(event: BeforeUnloadEvent): void { if (this.editing && this.form.dirty) event.preventDefault(); }

  private errorText(error: unknown): string {
    const value = error as { error?: { detail?: string; title?: string; missingRequirements?: string[]; errors?: Record<string, string[]> }; message?: string };
    const missing = value?.error?.missingRequirements;
    const validations = value?.error?.errors ? Object.values(value.error.errors).flat() : [];
    return [...(missing ?? []), ...validations].join(' ') || value?.error?.detail || value?.error?.title || value?.message || 'No fue posible completar la operación.';
  }
}
