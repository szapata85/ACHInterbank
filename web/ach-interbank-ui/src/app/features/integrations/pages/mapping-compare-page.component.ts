import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  MappingSetComparisonResult,
  MappingSetRuleComparison
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-mapping-compare-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
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

  leftId = '';
  rightId = '';
  changeFilter: 'All' | 'Added' | 'Removed' | 'Modified' | 'Equal' = 'All';

  ngOnInit(): void {
    this.methodCode = this.route.snapshot.paramMap.get('methodCode') ?? '';
    this.loadMappingSets();
  }

  loadMappingSets(): void {
    this.api.getMethods().subscribe({
      next: (methods) => {
        const method = methods.find((x) => x.code.toLowerCase() === this.methodCode.toLowerCase());
        if (!method) {
          this.notifications.error('No se encontró el método para comparación.');
          return;
        }

        this.api.getMappingSets(method.id).subscribe({
          next: (sets) => {
            this.mappingSets = [...sets].sort((a, b) => b.version - a.version);
            this.leftId = this.mappingSets[0]?.id ?? '';
            this.rightId = this.mappingSets[1]?.id ?? this.mappingSets[0]?.id ?? '';
            if (this.leftId && this.rightId && this.leftId !== this.rightId) {
              this.runCompare();
            }
          },
          error: () => this.notifications.error('No fue posible cargar versiones para comparar.')
        });
      },
      error: () => this.notifications.error('No fue posible cargar métodos.')
    });
  }

  runCompare(): void {
    if (!this.leftId || !this.rightId || this.leftId === this.rightId) {
      this.notifications.error('Selecciona dos versiones distintas del mismo método.');
      return;
    }

    this.api.compare(this.leftId, this.rightId).subscribe({
      next: (result) => (this.comparison = result),
      error: () => this.notifications.error('No fue posible comparar las versiones seleccionadas.')
    });
  }

  get filteredRules(): MappingSetRuleComparison[] {
    const rules = this.comparison?.rules ?? [];
    if (this.changeFilter === 'All') return rules;
    return rules.filter((x) => x.changeType === this.changeFilter);
  }

  get groupedRules(): Record<string, MappingSetRuleComparison[]> {
    const groups: Record<string, MappingSetRuleComparison[]> = {
      'ciclo-camara': [],
      transaccion: [],
      lote: [],
      addenda: [],
      configuracion: []
    };

    for (const rule of this.filteredRules) {
      const key = rule.parameterGroup || 'configuracion';
      if (!groups[key]) groups.configuracion.push(rule);
      else groups[key].push(rule);
    }

    return groups;
  }

  getChangeBadgeClass(changeType: string): string {
    return `change-${changeType.toLowerCase()}`;
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
    switch (status) {
      case 'Draft':
        return 'Borrador';
      case 'Published':
        return 'Publicado';
      case 'Archived':
        return 'Archivado';
      default:
        return status;
    }
  }
}
