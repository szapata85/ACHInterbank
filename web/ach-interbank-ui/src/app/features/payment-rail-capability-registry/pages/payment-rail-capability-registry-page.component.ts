import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';
import { SharedModule } from '../../../shared/shared.module';
import { PaymentRailCapabilityRegistryApiService } from '../services/payment-rail-capability-registry-api.service';
import { PaymentRailCapabilityItem, PaymentRailItem } from '../models/payment-rail-capability-registry.models';

@Component({
  selector: 'app-payment-rail-capability-registry-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './payment-rail-capability-registry-page.component.html',
  styleUrls: ['./payment-rail-capability-registry-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PaymentRailCapabilityRegistryPageComponent implements OnInit {
  private readonly api = inject(PaymentRailCapabilityRegistryApiService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    railCode: '',
    capabilityCode: '',
    asOfUtc: ''
  });

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Governanza multi-riel' },
    { etiqueta: 'Capability Registry (solo lectura)' }
  ];

  rails: PaymentRailItem[] = [];
  capabilities: PaymentRailCapabilityItem[] = [];
  loading = false;
  loadingRails = false;
  error = '';

  readonly columnas: ColDef<PaymentRailCapabilityItem>[] = [
    { field: 'railCode', headerName: 'Riel', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'capabilityCode', headerName: 'Capability', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'state', headerName: 'Estado efectivo', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'source', headerName: 'Origen', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'effectiveFromUtc', headerName: 'Vigencia desde (UTC)', sortable: true },
    { field: 'effectiveToUtc', headerName: 'Vigencia hasta (UTC)', sortable: true },
    { field: 'version', headerName: 'Versión', sortable: true },
    { field: 'changeSource', headerName: 'Fuente cambio', sortable: true },
    { field: 'changeTicket', headerName: 'Ticket cambio', sortable: true },
    { field: 'changedBy', headerName: 'Cambiado por', sortable: true },
    { field: 'changedAtUtc', headerName: 'Cambio en (UTC)', sortable: true }
  ];

  ngOnInit(): void {
    this.loadRails();
  }

  get indicadores(): Array<{ etiqueta: string; valor: string; estado: 'activo' | 'pendiente' | 'exitoso' }> {
    const selectedRail = this.rails.find((x) => x.railCode === this.form.controls.railCode.value);

    return [
      { etiqueta: 'Modo', valor: 'Solo lectura', estado: 'activo' },
      { etiqueta: 'Riel seleccionado', valor: selectedRail?.displayName ?? 'Sin selección', estado: selectedRail ? 'exitoso' : 'pendiente' },
      { etiqueta: 'Registros visibles', valor: this.capabilities.length.toString(), estado: this.capabilities.length ? 'exitoso' : 'pendiente' },
      { etiqueta: 'Estado consulta', valor: this.error ? 'Con novedad' : 'Operativa', estado: this.error ? 'pendiente' : 'activo' }
    ];
  }

  consultar(): void {
    const railCode = this.form.controls.railCode.value;
    if (!railCode) {
      this.error = 'Seleccione un riel para consultar capacidades.';
      this.capabilities = [];
      return;
    }

    this.loading = true;
    this.error = '';

    const asOfUtc = this.toIsoUtc(this.form.controls.asOfUtc.value);
    const capabilityCode = this.form.controls.capabilityCode.value.trim();

    if (capabilityCode) {
      this.api
        .getCapabilityByRail(railCode, capabilityCode, asOfUtc)
        .pipe(finalize(() => (this.loading = false)))
        .subscribe({
          next: (response) => {
            this.capabilities = [response];
          },
          error: () => {
            this.capabilities = [];
            this.error = 'No fue posible consultar el Capability Registry read-only para el riel seleccionado.';
          }
        });
      return;
    }

    this.api
      .getCapabilitiesByRail(railCode, asOfUtc)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (response) => {
          this.capabilities = response;
        },
        error: () => {
          this.capabilities = [];
          this.error = 'No fue posible consultar el Capability Registry read-only para el riel seleccionado.';
        }
      });
  }

  limpiar(): void {
    this.form.patchValue({ capabilityCode: '', asOfUtc: '' });
    this.error = '';
    this.capabilities = [];
  }

  private loadRails(): void {
    this.loadingRails = true;
    this.api
      .getRails()
      .pipe(finalize(() => (this.loadingRails = false)))
      .subscribe({
        next: (rails) => {
          this.rails = rails;
          const defaultRail = rails.find((x) => x.isOperational)?.railCode ?? rails[0]?.railCode ?? '';
          this.form.controls.railCode.setValue(defaultRail);
          if (defaultRail) {
            this.consultar();
          }
        },
        error: () => {
          this.error = 'No fue posible cargar el catálogo de rieles disponibles.';
        }
      });
  }

  private toIsoUtc(value: string): string | undefined {
    if (!value) {
      return undefined;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return undefined;
    }

    return date.toISOString();
  }
}
