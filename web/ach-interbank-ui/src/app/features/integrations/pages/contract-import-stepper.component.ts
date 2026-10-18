import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Output,
  computed,
  effect,
  inject,
  signal
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatStepperModule } from '@angular/material/stepper';
import {
  IntegrationContractChangeType,
  IntegrationContractDiffChangeDto,
  IntegrationContractDiffImpact,
  IntegrationContractVersionDto
} from '../data-access/integration-api.models';
import { ContractWorkspaceStore } from '../state/contract-workspace.store';

@Component({
  selector: 'app-contract-import-stepper',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatStepperModule
  ],
  templateUrl: './contract-import-stepper.component.html',
  styleUrls: ['./contract-import-stepper.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ContractImportStepperComponent {
  @Output() readonly closed = new EventEmitter<void>();

  readonly store = inject(ContractWorkspaceStore);
  private readonly fb = inject(FormBuilder);

  readonly currentStep = signal(0);
  readonly operationCandidates = signal<string[]>([]);
  readonly selectedBaseContractId = signal('');
  readonly diffReviewed = signal(false);
  private readonly analysisIntent = signal<'initial' | 'operation' | null>(null);

  readonly operationForm = this.fb.nonNullable.group({
    operationName: ['', [Validators.required, Validators.maxLength(200)]],
    standaloneXsdSoapAction: ['', [Validators.required, Validators.maxLength(1000)]]
  });

  readonly importForm = this.fb.nonNullable.group({
    versionLabel: ['', [Validators.required, Validators.maxLength(100)]],
    notes: ['', [Validators.maxLength(2000)]]
  });

  readonly analysisValid = computed(() => this.store.artifactAnalysis()?.importable === true);
  readonly baseVersions = computed(() => this.store.versions());
  readonly selectedBaseVersion = computed(() =>
    this.baseVersions().find((version) => version.id === this.selectedBaseContractId()) ?? null
  );
  readonly previewMatchesBase = computed(() =>
    this.store.artifactPreview()?.diff?.leftContractVersionId === this.selectedBaseContractId()
  );
  readonly isStandaloneXsd = computed(() => this.store.artifactAnalysis()?.artifactKind === 'XmlSchema');
  readonly isMultipleOperationWsdl = computed(() =>
    this.store.artifactAnalysis()?.artifactKind === 'Wsdl11' && this.operationCandidates().length > 1
  );
  readonly requiresOperationSelection = computed(() =>
    this.store.artifactAnalysis()?.artifactKind === 'Wsdl11'
      && !this.store.artifactAnalysis()?.operationName
      && this.operationCandidates().length > 0
  );

  constructor() {
    effect(() => {
      const versions = this.baseVersions();
      if (this.selectedBaseContractId() || versions.length === 0) return;
      const preferred = versions.find((version) => version.status === 'Active') ?? versions[0];
      this.selectedBaseContractId.set(preferred.id);
    });

    effect(() => {
      if (this.store.artifactAnalysisStatus() !== 'ready') return;
      const analysis = this.store.artifactAnalysis();
      const intent = this.analysisIntent();
      if (!analysis || !intent) return;

      if (analysis.availableOperations.length > 1) {
        this.operationCandidates.set(analysis.availableOperations);
      } else if (this.operationCandidates().length === 0 && analysis.availableOperations.length === 1) {
        this.operationCandidates.set(analysis.availableOperations);
      }

      if (intent === 'initial') {
        if (analysis.operationName) {
          this.operationForm.controls.operationName.setValue(analysis.operationName);
        } else if (analysis.availableOperations.length === 1) {
          this.operationForm.controls.operationName.setValue(analysis.availableOperations[0]);
        }
        if (analysis.artifactKind === 'XmlSchema') {
          this.seedStandaloneXsdMetadata();
        }
        this.currentStep.set(1);
      }
      this.analysisIntent.set(null);
    });

    effect(() => {
      if (this.store.artifactPreviewStatus() === 'ready' && this.store.artifactPreview()?.diff && this.previewMatchesBase()) {
        this.diffReviewed.set(false);
        this.currentStep.set(3);
      }
    });
  }

  onStepSelection(index: number): void {
    const max = this.maxAccessibleStep();
    this.currentStep.set(Math.min(index, max));
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? input.files?.item?.(0) ?? null;
    this.store.selectArtifact(file);
    this.currentStep.set(0);
    this.operationCandidates.set([]);
    this.operationForm.reset({ operationName: '', standaloneXsdSoapAction: '' });
    this.importForm.reset({ versionLabel: '', notes: '' });
    this.diffReviewed.set(false);
  }

  analyzeSelectedFile(): void {
    if (!this.store.permissions().canManage) return;
    if (!this.store.artifactFile() || this.store.artifactAnalysisStatus() === 'loading') return;
    this.analysisIntent.set('initial');
    this.store.analyzeArtifact();
  }

  validateOperation(): void {
    if (!this.store.permissions().canManage) return;
    if (this.operationForm.controls.operationName.invalid || this.store.artifactAnalysisStatus() === 'loading') {
      this.operationForm.markAllAsTouched();
      return;
    }

    if (this.isStandaloneXsd() && this.operationForm.controls.standaloneXsdSoapAction.invalid) {
      this.operationForm.markAllAsTouched();
      return;
    }

    this.analysisIntent.set('operation');
    this.store.analyzeArtifact(
      this.operationForm.controls.operationName.value,
      this.isStandaloneXsd() ? this.operationForm.controls.standaloneXsdSoapAction.value : null
    );
  }

  continueToSummary(): void {
    if (!this.analysisValid()) return;
    this.currentStep.set(2);
  }

  selectBaseVersion(contractVersionId: string): void {
    this.selectedBaseContractId.set(contractVersionId);
    this.diffReviewed.set(false);
  }

  previewDiff(): void {
    if (!this.store.permissions().canManage) return;
    if (!this.analysisValid() || !this.selectedBaseContractId() || this.store.artifactPreviewStatus() === 'loading') return;
    this.store.previewArtifact(
      this.selectedBaseContractId(),
      this.effectiveOperationName(),
      this.effectiveStandaloneSoapAction()
    );
  }

  continueToConfirmation(): void {
    if (this.store.artifactPreviewStatus() !== 'ready' || !this.store.artifactPreview()?.diff || !this.previewMatchesBase()) return;
    this.diffReviewed.set(true);
    this.currentStep.set(4);
  }

  importDraft(): void {
    if (!this.store.permissions().canManage) return;
    if (this.importForm.invalid || !this.diffReviewed() || !this.previewMatchesBase() || this.store.artifactImportStatus() === 'loading') {
      this.importForm.markAllAsTouched();
      return;
    }

    const value = this.importForm.getRawValue();
    this.store.importArtifactDraft(
      value.versionLabel,
      value.notes,
      this.effectiveOperationName(),
      this.effectiveStandaloneSoapAction()
    );
  }

  close(): void {
    this.store.resetArtifactImport();
    this.closed.emit();
  }

  impactLabel(impact: IntegrationContractDiffImpact): string {
    switch (impact) {
      case 'MappingRequired': return 'Requiere relacionar';
      case 'RequiresDevelopment': return 'Requiere desarrollo';
    }
  }

  changeLabel(type: IntegrationContractChangeType): string {
    const labels: Record<IntegrationContractChangeType, string> = {
      Added: 'Dato nuevo',
      Removed: 'Dato eliminado',
      RequiredChanged: 'Cambio de obligatoriedad',
      TypeChanged: 'Cambio de tipo',
      CardinalityChanged: 'Cambio de cardinalidad',
      OrderChanged: 'Cambio de orden',
      NamespaceChanged: 'Cambio de namespace',
      DirectionChanged: 'Cambio de dirección'
    };
    return labels[type];
  }

  changeDescription(change: IntegrationContractDiffChangeDto): string {
    const name = change.functionalName || change.technicalName;
    switch (change.changeType) {
      case 'Added': return `${name}: dato nuevo detectado por el backend.`;
      case 'Removed': return `${name}: dato eliminado frente a la versión base.`;
      case 'RequiredChanged': return `${name}: cambió su obligatoriedad.`;
      case 'TypeChanged': return `${name}: cambió su tipo.`;
      case 'CardinalityChanged': return `${name}: cambió su cardinalidad.`;
      case 'OrderChanged': return `${name}: cambió su orden.`;
      case 'NamespaceChanged': return 'Cambió el namespace del contrato SOAP.';
      case 'DirectionChanged': return `${name}: cambió su dirección.`;
    }
  }

  formatBytes(size: number): string {
    if (!Number.isFinite(size) || size < 0) return 'Tamaño no informado';
    if (size < 1024) return `${size} B`;
    const kb = size / 1024;
    if (kb < 1024) return `${kb.toFixed(kb >= 100 ? 0 : 1)} KB`;
    return `${(kb / 1024).toFixed(1)} MB`;
  }

  versionLabel(version: IntegrationContractVersionDto): string {
    const status = version.status === 'Active' ? 'Activa' : version.status === 'Retired' ? 'Retirada' : 'Borrador';
    return `${version.versionLabel} · ${status} · revisión ${version.revision}`;
  }

  private maxAccessibleStep(): number {
    if (this.store.artifactAnalysisStatus() !== 'ready') return 0;
    if (!this.analysisValid()) return 1;
    if (this.store.artifactPreviewStatus() !== 'ready' || !this.store.artifactPreview()?.diff || !this.previewMatchesBase()) return 2;
    if (!this.diffReviewed()) return 3;
    return 4;
  }

  private effectiveOperationName(): string | null {
    const backendOperation = this.store.artifactAnalysis()?.operationName?.trim();
    if (backendOperation) return backendOperation;
    const selected = this.operationForm.controls.operationName.value.trim();
    return selected || null;
  }

  private effectiveStandaloneSoapAction(): string | null {
    if (!this.isStandaloneXsd()) return null;
    const value = this.operationForm.controls.standaloneXsdSoapAction.value.trim();
    return value || null;
  }

  private seedStandaloneXsdMetadata(): void {
    const base = this.store.activeContract() ?? this.baseVersions()[0] ?? null;
    if (!base) return;
    if (!this.operationForm.controls.operationName.value.trim()) {
      this.operationForm.controls.operationName.setValue(base.operationName);
    }
    if (!this.operationForm.controls.standaloneXsdSoapAction.value.trim()) {
      this.operationForm.controls.standaloneXsdSoapAction.setValue(base.soapAction);
    }
  }
}
