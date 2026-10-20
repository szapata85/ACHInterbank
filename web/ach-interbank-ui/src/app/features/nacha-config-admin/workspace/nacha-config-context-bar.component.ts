import { ChangeDetectionStrategy, Component, Input, OnChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import {
  NachaConfigFilterCatalogOption,
  NachaConfigFilterCatalogs,
  NachaConfigWorkspace
} from '../../../core/models/nacha-config.model';

@Component({
  selector: 'app-nacha-config-context-bar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatCardModule, MatChipsModule, MatFormFieldModule, MatSelectModule],
  templateUrl: './nacha-config-context-bar.component.html',
  styleUrls: ['./nacha-config-context-bar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigContextBarComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  @Input({ required: true }) perfil!: NachaConfigWorkspace;
  @Input() catalogos: NachaConfigFilterCatalogs | null = null;
  @Input() catalogosError = false;

  readonly form = this.fb.group({
    camara: this.fb.control({ value: '', disabled: true }),
    flujo: this.fb.control({ value: '', disabled: true }),
    direccion: this.fb.control({ value: '', disabled: true }),
    servicio: this.fb.control({ value: '', disabled: true })
  });

  ngOnChanges(): void {
    if (!this.perfil) {
      return;
    }

    this.form.reset({
      camara: this.perfil.camara,
      flujo: this.perfil.flujo,
      direccion: this.perfil.direccion,
      servicio: this.perfil.servicio ?? ''
    }, { emitEvent: false });
  }

  get camaras(): NachaConfigFilterCatalogOption[] {
    return this.withCurrent(this.catalogos?.camaras ?? [], this.perfil?.camara, this.perfil?.camaraNombre);
  }

  get flujos(): NachaConfigFilterCatalogOption[] {
    return this.withCurrent(this.catalogos?.flujos ?? [], this.perfil?.flujo, this.perfil?.flujoNombre);
  }

  get direcciones(): NachaConfigFilterCatalogOption[] {
    return this.withCurrent(this.catalogos?.direcciones ?? [], this.perfil?.direccion, this.perfil?.direccionNombre);
  }

  get servicios(): NachaConfigFilterCatalogOption[] {
    if (!this.perfil?.servicio) {
      return this.catalogos?.servicios ?? [];
    }
    return this.withCurrent(this.catalogos?.servicios ?? [], this.perfil.servicio, this.perfil.servicioNombre ?? this.perfil.servicio);
  }

  vigencia(): string {
    const desde = this.fecha(this.perfil?.effectiveFrom);
    const hasta = this.perfil?.effectiveTo ? this.fecha(this.perfil.effectiveTo) : 'Sin fecha final';
    return `${desde} a ${hasta}`;
  }

  private withCurrent(
    options: NachaConfigFilterCatalogOption[],
    code?: string | null,
    label?: string | null
  ): NachaConfigFilterCatalogOption[] {
    if (!code || options.some((option) => option.code === code)) {
      return options;
    }
    return [...options, { code, labelEs: label?.trim() || code }];
  }

  private fecha(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }
}
