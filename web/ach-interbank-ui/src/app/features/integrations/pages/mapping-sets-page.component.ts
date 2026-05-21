import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  IntegrationMethod
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';

type MappingModalMode = 'detail' | 'edit' | 'draft' | null;

@Component({
  selector: 'app-mapping-sets-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './mapping-sets-page.component.html',
  styleUrls: ['./mapping-sets-page.component.scss']
})
export class MappingSetsPageComponent implements OnInit {
  private readonly api = inject(IntegrationMappingAdminService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  loading = false;
  methods: IntegrationMethod[] = [];
  mappingSets: IntegrationMappingSet[] = [];
  creatingDraft = false;
  modalMode: MappingModalMode = null;
  selectedMapping: IntegrationMappingSet | null = null;
  searchTerm = '';
  statusFilter = 'all';

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones', ruta: '/integraciones' },
    { etiqueta: 'Configuracion funcional' }
  ];

  readonly createDraftForm = this.fb.group({
    methodId: [null as number | null, Validators.required],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    notes: ['']
  });

  ngOnInit(): void {
    this.loadMethods();
  }

  get selectedMethodId(): number | null {
    return this.createDraftForm.controls.methodId.value;
  }

  get selectedMethod(): IntegrationMethod | undefined {
    return this.methods.find((x) => x.id === this.selectedMethodId);
  }

  get stats() {
    const total = this.mappingSets.length;
    const draft = this.mappingSets.filter((x) => this.normalizeStatus(x.status) === 'Draft').length;
    const published = this.mappingSets.filter((x) => this.normalizeStatus(x.status) === 'Published').length;
    const archived = this.mappingSets.filter((x) => this.normalizeStatus(x.status) === 'Archived').length;
    return { total, draft, published, archived, integrations: this.methods.length };
  }

  get filteredMappingSets(): IntegrationMappingSet[] {
    const term = this.searchTerm.trim().toLowerCase();
    return this.mappingSets.filter((set) => {
      const normalizedStatus = this.normalizeStatus(set.status);
      const statusMatches = this.statusFilter === 'all' || normalizedStatus === this.statusFilter;
      if (!statusMatches) {
        return false;
      }

      if (!term) {
        return true;
      }

      return [set.name, set.methodCode, set.notes, set.publishedBy, this.getStatusLabel(set.status)]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(term));
    });
  }

  loadMethods(): void {
    this.loading = true;
    this.api.getMethods().subscribe({
      next: (items) => {
        this.methods = items ?? [];
        if (!this.selectedMethodId && this.methods.length > 0) {
          this.createDraftForm.patchValue({ methodId: this.methods[0].id });
        }
        this.loadMappingSets();
      },
      error: () => this.notifications.error('No fue posible cargar metodos de integracion.'),
      complete: () => (this.loading = false)
    });
  }

  loadMappingSets(): void {
    const methodId = this.selectedMethodId;
    this.api.getMappingSets(methodId ?? undefined).subscribe({
      next: (items) => (this.mappingSets = items ?? []),
      error: () => this.notifications.error('No fue posible cargar las configuraciones funcionales.')
    });
  }

  onMethodChange(): void {
    this.loadMappingSets();
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter = value;
  }

  openDraftModal(): void {
    this.selectedMapping = null;
    this.modalMode = 'draft';
  }

  openDetail(mapping: IntegrationMappingSet): void {
    this.selectedMapping = mapping;
    this.modalMode = 'detail';
  }

  openEdit(mapping: IntegrationMappingSet): void {
    this.selectedMapping = mapping;
    this.modalMode = 'edit';
  }

  closeModal(): void {
    this.modalMode = null;
    this.selectedMapping = null;
  }

  openCompare(): void {
    const method = this.selectedMethod;
    if (!method) {
      this.notifications.error('Selecciona un metodo para comparar versiones.');
      return;
    }
    if (this.mappingSets.length < 2) {
      this.notifications.error('Se requieren al menos dos versiones para comparar.');
      return;
    }

    this.router.navigate(['/integraciones/mappings/compare', method.code]);
  }

  openEditor(mapping: IntegrationMappingSet): void {
    this.router.navigate(['/integraciones/mappings', mapping.methodCode, mapping.id]);
  }

  goToSelectedEditor(): void {
    if (this.selectedMapping) {
      this.openEditor(this.selectedMapping);
    }
  }

  createDraft(): void {
    if (this.createDraftForm.invalid) {
      this.createDraftForm.markAllAsTouched();
      return;
    }

    const methodId = this.createDraftForm.controls.methodId.value!;
    const name = this.createDraftForm.controls.name.value!.trim();
    const notes = this.createDraftForm.controls.notes.value?.trim() ?? '';
    if (!name) {
      this.notifications.error('El nombre del borrador es obligatorio.');
      return;
    }

    this.creatingDraft = true;
    this.api.createDraft(methodId, name, notes, 'ui-admin').subscribe({
      next: (created) => {
        this.notifications.success('Borrador de configuracion creado.');
        this.closeModal();
        this.openEditor(created);
      },
      error: () => this.notifications.error('No fue posible crear el borrador de configuracion.'),
      complete: () => (this.creatingDraft = false)
    });
  }

  getStatusLabel(status: IntegrationMappingSet['status'] | number | string | null | undefined): string {
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

  getStatusClass(status: IntegrationMappingSet['status'] | number | string | null | undefined): string {
    return this.normalizeStatus(status).toLowerCase();
  }

  getMethodLabel(method?: IntegrationMethod | null): string {
    if (!method) {
      return 'Sin integracion seleccionada';
    }

    return `${method.displayName} - ${method.code}`;
  }

  getMethodById(methodId: number): IntegrationMethod | undefined {
    return this.methods.find((x) => x.id === methodId);
  }

  private normalizeStatus(status: IntegrationMappingSet['status'] | number | string | null | undefined): string {
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
}
