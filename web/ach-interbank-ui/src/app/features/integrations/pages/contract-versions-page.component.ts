import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IntegrationContractVersionReadModel, IntegrationParameterReadModel } from '../data-access/integration-read.models';
import { ContractWorkspaceStore } from '../state/contract-workspace.store';
import { ContractImportStepperComponent } from './contract-import-stepper.component';

@Component({
  selector: 'app-contract-versions-page',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ContractImportStepperComponent
  ],
  templateUrl: './contract-versions-page.component.html',
  styleUrls: ['./contract-versions-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ContractVersionsPageComponent {
  readonly store = inject(ContractWorkspaceStore);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  readonly editingParameterId = signal<number | null>(null);
  readonly showImportFlow = signal(false);
  readonly draftVersions = computed(() => this.byState('Draft'));
  readonly activeVersions = computed(() => this.byState('Active'));
  readonly retiredVersions = computed(() => this.byState('Retired'));
  readonly canEditCurrentContract = computed(() =>
    this.store.permissions().canManage && this.store.selectedVersion()?.canEditFunctionalMetadata === true
  );

  readonly functionalMetadataForm = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    descriptionEs: ['', [Validators.maxLength(1000)]],
    uiHelpText: ['', [Validators.maxLength(1000)]],
    category: ['', [Validators.maxLength(120)]],
    exampleValue: ['', [Validators.maxLength(500)]]
  });

  private readonly dateFormatter = new Intl.DateTimeFormat('es-CO', {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });

  constructor() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => this.store.loadForOperation(params.get('method') ?? ''));

    effect(() => {
      if (this.store.parameterSaveStatus() === 'ready' && this.editingParameterId() != null) {
        this.editingParameterId.set(null);
      }
    });
  }

  beginImport(): void {
    if (!this.store.permissions().canManage || this.store.methodId() == null) return;
    this.store.clearDetail();
    this.store.resetArtifactImport();
    this.showImportFlow.set(true);
  }

  closeImport(): void {
    this.showImportFlow.set(false);
  }

  openDetail(version: IntegrationContractVersionReadModel): void {
    this.editingParameterId.set(null);
    this.store.loadDetail(version.id);
  }

  closeDetail(): void {
    this.editingParameterId.set(null);
    this.store.clearDetail();
  }

  beginEdit(parameter: IntegrationParameterReadModel): void {
    if (!this.canEditCurrentContract()) return;

    this.store.clearParameterSaveState();
    this.functionalMetadataForm.reset({
      displayName: parameter.functionalName,
      descriptionEs: parameter.functionalDescription,
      uiHelpText: parameter.uiHelpText,
      category: parameter.categoryLabel,
      exampleValue: parameter.exampleValue
    });
    this.editingParameterId.set(parameter.parameterId);
  }

  cancelEdit(): void {
    this.editingParameterId.set(null);
    this.store.clearParameterSaveState();
  }

  saveEdit(parameterId: number): void {
    if (this.functionalMetadataForm.invalid || !this.canEditCurrentContract()) {
      this.functionalMetadataForm.markAllAsTouched();
      return;
    }

    this.store.saveParameterPresentation(parameterId, this.functionalMetadataForm.getRawValue());
  }

  formatDate(value: string | null): string {
    if (!value) return 'No informada por backend';
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? value : this.dateFormatter.format(parsed);
  }

  sourceDocumentLabel(): string {
    const contract = this.store.detail()?.contract;
    return contract?.sourceDocumentName || contract?.sourceReference || 'No informado por backend';
  }

  private byState(state: IntegrationContractVersionReadModel['displayState']): IntegrationContractVersionReadModel[] {
    return this.store.versionRows().filter((version) => version.displayState === state);
  }
}
