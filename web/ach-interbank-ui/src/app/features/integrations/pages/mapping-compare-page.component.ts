import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  MappingSetComparisonResult,
  MappingSetRuleComparison
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { ColDef } from 'ag-grid-community';

type TipoCambio = 'All' | 'Added' | 'Removed' | 'Modified' | 'Equal';

@Component({
  selector: 'app-mapping-compare-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './mapping-compare-page.component.html',
  styleUrls: ['./mapping-compare-page.component.scss']
})
export class MappingComparePageComponent implements OnInit {
  private readonly api = inject(IntegrationMappingAdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(NotificationService);

  methodCode = '';
  mappingSets: IntegrationMappingSet[] = [];
  comparison?: MappingSetComparisonResult;
  loading = false;

  readonly versionIzquierdaControl = new FormControl<string>('', { nonNullable: true });
  readonly versionDerechaControl = new FormControl<string>('', { nonNullable: true });
  readonly filtroCambioControl = new FormControl<TipoCambio>('All', { nonNullable: true });

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones', ruta: '/integraciones' },
    { etiqueta: 'Comparador de versiones' }
  ];

  readonly columnasComparacion: ColDef[] = [
    { field: 'parametro', headerName: 'Parámetro', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'cambio', headerName: 'Cambio', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'camposModificados', headerName: 'Campos modificados', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'impacto', headerName: 'Impacto potencial', sortable: true, filter: 'agTextColumnFilter' }
  ];

  ngOnInit(): void {
    this.methodCode = this.route.snapshot.paramMap.get('methodCode') ?? '';
    this.loadMappingSets();
  }

  filasGrupo(group: string): any[] {
    return (this.groupedRules[group] || []).map((row) => ({
      parametro: row.parameterPath,
      cambio: this.getChangeLabel(row.changeType),
      camposModificados: row.changedFields.join(', ') || 'N/D',
      impacto: row.potentialImpact
    }));
  }

  loadMappingSets(): void {
    this.loading = true;
    this.api.getMethods().subscribe({
      next: (methods) => {
        const method = methods.find((x) => x.code.toLowerCase() === this.methodCode.toLowerCase());
        if (!method) {
          this.notifications.error('No se encontró el método para comparación.');
          this.loading = false;
          return;
        }

        this.api.getMappingSets(method.id).subscribe({
          next: (sets) => {
            this.mappingSets = [...sets].sort((a, b) => b.version - a.version);
            const izquierda = this.mappingSets[0]?.id ?? '';
            const derecha = this.mappingSets[1]?.id ?? this.mappingSets[0]?.id ?? '';
            this.versionIzquierdaControl.setValue(izquierda);
            this.versionDerechaControl.setValue(derecha);
            if (this.canCompare) {
              this.runCompare();
            }
          },
          error: () => this.notifications.error('No fue posible cargar versiones para comparar.'),
          complete: () => (this.loading = false)
        });
      },
      error: () => {
        this.notifications.error('No fue posible cargar métodos.');
        this.loading = false;
      }
    });
  }

  runCompare(): void {
    const leftId = this.versionIzquierdaControl.value;
    const rightId = this.versionDerechaControl.value;
    if (!leftId || !rightId || leftId === rightId) {
      this.notifications.error('Selecciona dos versiones distintas del mismo método.');
      return;
    }

    this.api.compare(leftId, rightId).subscribe({
      next: (result) => (this.comparison = result),
      error: () => this.notifications.error('No fue posible comparar las versiones seleccionadas.')
    });
  }

  limpiarFiltros(): void {
    this.filtroCambioControl.setValue('All');
  }

  get filteredRules(): MappingSetRuleComparison[] {
    const rules = this.comparison?.rules ?? [];
    const changeFilter = this.filtroCambioControl.value;
    if (changeFilter === 'All') return rules;
    return rules.filter((x) => x.changeType === changeFilter);
  }

  get canCompare(): boolean {
    const leftId = this.versionIzquierdaControl.value;
    const rightId = this.versionDerechaControl.value;
    return Boolean(leftId && rightId && leftId !== rightId);
  }

  get methodDisplayName(): string {
    return this.methodCode.replace('WSCFAACH.', '');
  }

  get groupedRules(): Record<string, MappingSetRuleComparison[]> {
    const groups: Record<string, MappingSetRuleComparison[]> = {
      'ciclo-camara': [],
      transaccion: [],
      lote: [],
      addenda: [],
      configuracion: [],
      'respuesta-esperada': []
    };

    for (const rule of this.filteredRules) {
      const key = rule.parameterGroup || 'configuracion';
      if (!groups[key]) groups.configuracion.push(rule);
      else groups[key].push(rule);
    }

    return groups;
  }

  getChangeLabel(changeType: string): string {
    switch (changeType) {
      case 'Added':
        return 'Agregado';
      case 'Removed':
        return 'Eliminado';
      case 'Modified':
        return 'Modificado';
      case 'Equal':
        return 'Sin cambios';
      default:
        return changeType;
    }
  }

  getStatusLabel(status: string): string {
    const normalized = this.normalizeStatus(status);
    switch (normalized) {
      case 'Draft':
        return 'Borrador';
      case 'Published':
        return 'Publicado';
      case 'Archived':
        return 'Archivado';
      default:
        return normalized || 'Sin estado';
    }
  }

  private normalizeStatus(status: string | number | null | undefined): string {
    if (status === null || status === undefined) return '';
    if (typeof status === 'number') {
      if (status === 0) return 'Draft';
      if (status === 1) return 'Published';
      if (status === 2) return 'Archived';
      return String(status);
    }

    const raw = String(status).trim();
    if (!raw) return '';
    const lowered = raw.toLowerCase();
    if (lowered === 'draft') return 'Draft';
    if (lowered === 'published') return 'Published';
    if (lowered === 'archived') return 'Archived';
    return raw;
  }

  getGroupLabel(group: string): string {
    switch (group) {
      case 'ciclo-camara':
        return 'Ciclo / Cámara';
      case 'transaccion':
        return 'Transacción';
      case 'lote':
        return 'Lote';
      case 'addenda':
        return 'Complementario';
      case 'respuesta-esperada':
        return 'Respuesta esperada';
      default:
        return 'Configuración';
    }
  }
}
