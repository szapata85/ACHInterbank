import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { IntegrationMappingAdminService, IntegrationMappingSet, IntegrationMethod } from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { ColDef } from 'ag-grid-community';

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

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones', ruta: '/integraciones' },
    { etiqueta: 'Configuración funcional' }
  ];

  readonly columnas: ColDef[] = [
    { field: 'nombre', headerName: 'Nombre', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'integracion', headerName: 'Integración', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'version', headerName: 'Versión', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'estado', headerName: 'Estado', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'publicadoPor', headerName: 'Publicado por', sortable: true, filter: 'agTextColumnFilter' },
    {
      field: 'acciones',
      headerName: 'Acciones',
      sortable: false,
      filter: false,
      maxWidth: 150,
      cellRenderer: (params: any) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.innerText = 'Abrir editor';
        button.classList.add('link');
        button.addEventListener('click', () => this.openEditor(params.data._original));
        return button;
      }
    }
  ];

  get filasGrilla(): any[] {
    return this.mappingSets.map((set) => ({
      nombre: set.name,
      integracion: set.methodCode.replace('WSCFAACH.', ''),
      version: set.version || 'Borrador',
      estado: this.getStatusLabel(set.status),
      publicadoPor: set.publishedBy || '—',
      acciones: 'Abrir editor',
      _original: set
    }));
  }

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

  get stats() {
    const total = this.mappingSets.length;
    const draft = this.mappingSets.filter((x) => this.normalizeStatus(x.status) === 'Draft').length;
    const published = this.mappingSets.filter((x) => this.normalizeStatus(x.status) === 'Published').length;
    const archived = this.mappingSets.filter((x) => this.normalizeStatus(x.status) === 'Archived').length;
    return { total, draft, published, archived };
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
      error: () => this.notifications.error('No fue posible cargar métodos de integración.'),
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

  openCompare(): void {
    const methodId = this.selectedMethodId;
    const method = (this.methods ?? []).find((x) => x.id === methodId);
    if (!method) {
      this.notifications.error('Selecciona un método para comparar versiones.');
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
        this.notifications.success('Borrador de configuración creado.');
        this.openEditor(created);
      },
      error: () => this.notifications.error('No fue posible crear el borrador de configuración.'),
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
