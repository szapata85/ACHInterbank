import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ClearingHousesApiService } from '../../features/ach-cycles/services/ach-cycles-api.service';

interface ClearingHouseOption {
  id: number;
  name: string;
  code?: string;
}

@Component({
  selector: 'app-clearing-house-select',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <select
      [disabled]="disabled || loading || hasError"
      [formControl]="selectionControl">
      <option [ngValue]="null">{{ placeholder }}</option>
      <option *ngIf="allowAll" [ngValue]="allValue">{{ allLabel }}</option>
      <option *ngFor="let option of options" [ngValue]="option.id">
        {{ option.code ? option.name + ' (' + option.code + ')' : option.name }}
      </option>
      <option *ngIf="loading" [ngValue]="null" disabled>Cargando cámaras...</option>
      <option *ngIf="hasError" [ngValue]="null" disabled>No fue posible cargar cámaras</option>
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

  readonly selectionControl = new FormControl<number | null>(null);
  options: ClearingHouseOption[] = [];
  loading = false;
  hasError = false;
  disabled = false;
  value: number | null = null;

  private onChange: (value: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    this.selectionControl.valueChanges.subscribe((value) => this.onSelectionChange(value));
  }

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
    this.selectionControl.setValue(value, { emitEvent: false });
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
    if (isDisabled) {
      this.selectionControl.disable({ emitEvent: false });
    } else {
      this.selectionControl.enable({ emitEvent: false });
    }
    this.cdr.markForCheck();
  }

  private onSelectionChange(value: number | null): void {
    this.value = value;
    this.onChange(value);
    this.onTouched();
  }
}
