import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ClearingHousesApiService } from '../../features/ach-cycles/services/ach-cycles-api.service';

interface ClearingHouseOption {
  id: number;
  name: string;
  code: string;
}

@Component({
  selector: 'app-clearing-house-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <select
      [disabled]="disabled || loading || hasError"
      [ngModel]="value"
      (ngModelChange)="onSelectionChange($event)">
      <option [ngValue]="null">{{ placeholder }}</option>
      <option *ngIf="allowAll" [ngValue]="allValue">{{ allLabel }}</option>
      <option *ngFor="let option of options" [ngValue]="option.id">
        {{ option.name }} ({{ option.code }})
      </option>
      <option *ngIf="loading" [ngValue]="null" disabled>Cargando cámaras...</option>
      <option *ngIf="hasError" [ngValue]="null" disabled>Error al cargar cámaras</option>
    </select>
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ClearingHouseSelectComponent),
      multi: true
    }
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHouseSelectComponent implements ControlValueAccessor, OnInit {
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() placeholder = 'Seleccione una cámara';
  @Input() allowAll = false;
  @Input() allLabel = 'Todas las cámaras';
  @Input() allValue: number | null = null;

  options: ClearingHouseOption[] = [];
  loading = false;
  hasError = false;
  disabled = false;
  value: number | null = null;

  private onChange: (value: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    this.loading = true;
    this.hasError = false;
    this.cdr.markForCheck();

    this.clearingHousesApi.list().subscribe({
      next: (items) => {
        this.options = items;
        this.loading = false;
        this.hasError = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.options = [];
        this.loading = false;
        this.hasError = true;
        this.cdr.markForCheck();
      }
    });
  }

  writeValue(value: number | null): void {
    this.value = value;
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.cdr.markForCheck();
  }

  onSelectionChange(value: number | null): void {
    this.value = value;
    this.onChange(value);
    this.onTouched();
  }
}
