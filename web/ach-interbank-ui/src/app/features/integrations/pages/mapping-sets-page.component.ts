import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  IntegrationMethod,
  IntegrationMethodParameter,
  IntegrationSourceCatalogField
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
  private readonly route = inject(ActivatedRoute);

  loading = false;
  methods: IntegrationMethod[] = [];
  mappingSets: IntegrationMappingSet[] = [];
  sourceCatalog: IntegrationSourceCatalogField[] = [];
  targetFields: IntegrationMethodParameter[] = [];
  catalogLoadState: 'idle' | 'loading' | 'ready' | 'error' = 'idle';
  creatingDraft = false;
  modalMode: MappingModalMode = null;
  selectedMapping: IntegrationMappingSet | null = null;
  searchTerm = '';
  statusFilter = 'all';
  purposeFilter = 'all';
  directionFilter = 'all';

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

      const method = this.getMethodById(set.methodId);
      if (this.purposeFilter !== 'all' && method?.mappingPurpose !== this.purposeFilter) {
        return false;
      }

      if (this.directionFilter !== 'all' && method?.mappingDirection !== this.directionFilter) {
        return false;
      }

      if (!term) {
        return true;
      }

      return [
        set.name,
        set.methodCode,
        set.notes,
        set.publishedBy,
        this.getStatusLabel(set.status),
        method?.mappingPurpose,
        method?.mappingDirection,
        method?.operationKey
      ]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(term));
    });
  }

  get mappingPurposes(): string[] {
    return [...new Set(this.methods.map((method) => method.mappingPurpose).filter(Boolean))].sort();
  }

  get mappingDirections(): string[] {
    return [...new Set(this.methods.map((method) => method.mappingDirection).filter(Boolean))].sort();
  }

  get sourceGroups(): Array<{ entityName: string; sourceKind: string; fields: IntegrationSourceCatalogField[] }> {
    const groups = new Map<string, { entityName: string; sourceKind: string; fields: IntegrationSourceCatalogField[] }>();
    for (const field of this.sourceCatalog) {
      const sourceKind = this.normalizeSourceKind(field.sourceKind as any);
      const key = `${field.entityName}|${sourceKind}`;
      const existing = groups.get(key) ?? { entityName: field.entityName, sourceKind, fields: [] };
      existing.fields.push(field);
      groups.set(key, existing);
    }

    return Array.from(groups.values())
      .map((group) => ({ ...group, fields: group.fields.sort((a, b) => a.sortOrder - b.sortOrder) }))
      .sort((a, b) => a.entityName.localeCompare(b.entityName));
  }

  get visibleTargetFields(): IntegrationMethodParameter[] {
    return [...this.targetFields].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  loadMethods(): void {
    this.loading = true;
    this.api.getMethods().subscribe({
      next: (items) => {
        this.methods = items ?? [];
        const requestedMethodCode = this.route.snapshot.queryParamMap.get('method');
        const requestedMethod = requestedMethodCode
          ? this.methods.find((method) => method.code === requestedMethodCode)
          : undefined;
        if (requestedMethod) {
          this.createDraftForm.patchValue({ methodId: requestedMethod.id });
        } else if (!this.selectedMethodId && this.methods.length > 0) {
          this.createDraftForm.patchValue({ methodId: this.methods[0].id });
        }
        this.loadCatalogForSelectedMethod();
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
    this.loadCatalogForSelectedMethod();
    this.loadMappingSets();
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter = value;
  }

  onPurposeFilterChange(value: string): void {
    this.purposeFilter = value;
  }

  onDirectionFilterChange(value: string): void {
    this.directionFilter = value;
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

    return `${method.integrationKey} / ${method.operationKey} - ${method.mappingPurpose}`;
  }

  getMethodSummary(method?: IntegrationMethod | null): string {
    if (!method) {
      return 'Seleccione una integracion para ver su clasificacion funcional.';
    }

    const money = method.movesMoney ? 'Mueve dinero' : 'No mueve dinero';
    return `${method.functionalNature}. ${method.functionalOriginator}. ${money}. Direccion: ${method.mappingDirection}.`;
  }

  getMethodById(methodId: number): IntegrationMethod | undefined {
    return this.methods.find((x) => x.id === methodId);
  }

  getSourceKindLabel(kind: string | null | undefined): string {
    switch (this.normalizeSourceKind(kind as any).toLowerCase()) {
      case 'nachaheader': return 'NachaHeaders';
      case 'batchheader': return 'BatchHeaders';
      case 'entrydetail': return 'EntryDetails';
      case 'addendarecord': return 'AddendaRecords';
      case 'batchcontrol': return 'BatchControls';
      case 'filecontrol': return 'FileControls';
      case 'transaction': return 'AchTransaction';
      case 'prenotification': return 'Prenotification';
      case 'differentialresponse': return 'DifferentialResponse';
      case 'financialinstitution': return 'FinancialInstitution';
      case 'clearinghouse': return 'ClearingHouse';
      case 'cycle': return 'AchCycle';
      default: return kind || 'Fuente';
    }
  }

  isNachaSource(kind: string | null | undefined): boolean {
    return ['nachaheader', 'batchheader', 'entrydetail', 'addendarecord', 'batchcontrol', 'filecontrol']
      .includes(this.normalizeSourceKind(kind as any).toLowerCase());
  }

  private normalizeSourceKind(kind: string | number | null | undefined): string {
    if (kind === null || kind === undefined) return '';
    if (typeof kind === 'number') {
      if (kind === 1) return 'Transaction';
      if (kind === 2) return 'Addenda';
      if (kind === 3) return 'Batch';
      if (kind === 4) return 'Cycle';
      if (kind === 5) return 'ClearingHouse';
      if (kind === 6) return 'Constant';
      if (kind === 7) return 'Expression';
      if (kind === 8) return 'NachaHeader';
      if (kind === 9) return 'BatchHeader';
      if (kind === 10) return 'EntryDetail';
      if (kind === 11) return 'AddendaRecord';
      if (kind === 12) return 'BatchControl';
      if (kind === 13) return 'FileControl';
      if (kind === 14) return 'Prenotification';
      if (kind === 15) return 'DifferentialResponse';
      return String(kind);
    }

    const raw = String(kind).trim();
    if (!raw) return '';
    if (/^\d+$/.test(raw)) return this.normalizeSourceKind(Number(raw));
    const lowered = raw.toLowerCase();
    if (lowered === 'nachaheaders') return 'NachaHeader';
    if (lowered === 'batchheaders') return 'BatchHeader';
    if (lowered === 'entrydetails') return 'EntryDetail';
    if (lowered === 'addendarecords') return 'AddendaRecord';
    if (lowered === 'batchcontrols') return 'BatchControl';
    if (lowered === 'filecontrols') return 'FileControl';
    return raw;
  }

  private loadCatalogForSelectedMethod(): void {
    const methodId = this.selectedMethodId;
    this.sourceCatalog = [];
    this.targetFields = [];
    if (!methodId) {
      this.catalogLoadState = 'idle';
      return;
    }

    this.catalogLoadState = 'loading';
    this.api.getSourceCatalog(methodId).subscribe({
      next: (items) => {
        this.sourceCatalog = items ?? [];
        this.catalogLoadState = 'ready';
      },
      error: () => {
        this.catalogLoadState = 'error';
        this.notifications.error('No fue posible cargar el catalogo de campos origen.');
      }
    });

    this.api.getMethodParameters(methodId).subscribe({
      next: (items) => (this.targetFields = items ?? []),
      error: () => this.notifications.error('No fue posible cargar campos destino SOAP/XML.')
    });
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
