import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { IntegrationMappingAdminService, IntegrationMappingSet, IntegrationMethod } from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-mapping-sets-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
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
    const draft = this.mappingSets.filter((x) => x.status === 'Draft').length;
    const published = this.mappingSets.filter((x) => x.status === 'Published').length;
    const archived = this.mappingSets.filter((x) => x.status === 'Archived').length;
    return { total, draft, published, archived };
  }

  loadMethods(): void {
    this.loading = true;
    this.api.getMethods().subscribe({
      next: (items) => {
        this.methods = items;
        if (!this.selectedMethodId && items.length > 0) {
          this.createDraftForm.patchValue({ methodId: items[0].id });
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
      next: (items) => (this.mappingSets = items),
      error: () => this.notifications.error('No fue posible cargar MappingSets.')
    });
  }

  onMethodChange(): void {
    this.loadMappingSets();
  }

  openCompare(): void {
    const methodId = this.selectedMethodId;
    const method = this.methods.find((x) => x.id === methodId);
    if (!method) {
      this.notifications.error('Selecciona un método para comparar versiones.');
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

    this.api.createDraft(methodId, name, notes, 'ui-admin').subscribe({
      next: (created) => {
        this.notifications.success('MappingSet Draft creado.');
        this.openEditor(created);
      },
      error: () => this.notifications.error('No fue posible crear el MappingSet Draft.')
    });
  }
}
