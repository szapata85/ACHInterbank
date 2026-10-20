import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, map, switchMap, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  UpdateIntegrationContractParameterPresentationRequest,
  IntegrationContractApi
} from '../data-access/integration-contract.api';
import {
  IntegrationContractArtifactAnalysisDto,
  IntegrationContractArtifactImportResultDto,
  IntegrationContractArtifactPreviewDto,
  IntegrationContractVersionDetailDto,
  IntegrationContractVersionDto,
  IntegrationMappingSetDto,
  IntegrationMethodDto
} from '../data-access/integration-api.models';
import { IntegrationMappingApi } from '../data-access/integration-mapping.api';
import {
  IntegrationContractVersionReadModel,
  IntegrationParameterReadModel,
  toIntegrationContractVersionReadModel,
  toIntegrationParameterReadModel
} from '../data-access/integration-read.models';
import { IntegrationWorkspaceApi } from '../data-access/integration-workspace.api';
import {
  IntegrationErrorState,
  IntegrationLoadStatus,
  IntegrationPermissionsState,
  statusFromError,
  toIntegrationErrorState
} from './integration-state.models';

export interface UpdateContractFunctionalMetadataInput {
  displayName: string;
  descriptionEs: string;
  uiHelpText: string;
  category: string;
  exampleValue: string;
}

@Injectable({ providedIn: 'root' })
export class ContractWorkspaceStore {
  private readonly api = inject(IntegrationContractApi);
  private readonly mappingApi = inject(IntegrationMappingApi);
  private readonly workspaceApi = inject(IntegrationWorkspaceApi);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly user = toSignal(this.auth.user$, { initialValue: this.auth.getCurrentUser() });

  private readonly operationKeySignal = signal<string | null>(null);
  private readonly methodSignal = signal<IntegrationMethodDto | null>(null);
  private readonly methodIdSignal = signal<number | null>(null);
  private readonly versionsSignal = signal<IntegrationContractVersionDto[]>([]);
  private readonly mappingsSignal = signal<IntegrationMappingSetDto[]>([]);
  private readonly detailSignal = signal<IntegrationContractVersionDetailDto | null>(null);
  private readonly listStatusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly detailStatusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly parameterSaveStatusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly listErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly detailErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly parameterSaveErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly artifactFileSignal = signal<File | null>(null);
  private readonly artifactAnalysisSignal = signal<IntegrationContractArtifactAnalysisDto | null>(null);
  private readonly artifactPreviewSignal = signal<IntegrationContractArtifactPreviewDto | null>(null);
  private readonly artifactImportResultSignal = signal<IntegrationContractArtifactImportResultDto | null>(null);
  private readonly artifactAnalysisStatusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly artifactPreviewStatusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly artifactImportStatusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly artifactAnalysisErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly artifactPreviewErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly artifactImportErrorSignal = signal<IntegrationErrorState | null>(null);

  readonly operationKey = this.operationKeySignal.asReadonly();
  readonly method = this.methodSignal.asReadonly();
  readonly methodId = this.methodIdSignal.asReadonly();
  readonly versions = this.versionsSignal.asReadonly();
  readonly mappings = this.mappingsSignal.asReadonly();
  readonly detail = this.detailSignal.asReadonly();
  readonly listStatus = this.listStatusSignal.asReadonly();
  readonly detailStatus = this.detailStatusSignal.asReadonly();
  readonly parameterSaveStatus = this.parameterSaveStatusSignal.asReadonly();
  readonly listError = this.listErrorSignal.asReadonly();
  readonly detailError = this.detailErrorSignal.asReadonly();
  readonly parameterSaveError = this.parameterSaveErrorSignal.asReadonly();
  readonly artifactFile = this.artifactFileSignal.asReadonly();
  readonly artifactAnalysis = this.artifactAnalysisSignal.asReadonly();
  readonly artifactPreview = this.artifactPreviewSignal.asReadonly();
  readonly artifactImportResult = this.artifactImportResultSignal.asReadonly();
  readonly artifactAnalysisStatus = this.artifactAnalysisStatusSignal.asReadonly();
  readonly artifactPreviewStatus = this.artifactPreviewStatusSignal.asReadonly();
  readonly artifactImportStatus = this.artifactImportStatusSignal.asReadonly();
  readonly artifactAnalysisError = this.artifactAnalysisErrorSignal.asReadonly();
  readonly artifactPreviewError = this.artifactPreviewErrorSignal.asReadonly();
  readonly artifactImportError = this.artifactImportErrorSignal.asReadonly();
  readonly error = computed(() => this.detailErrorSignal() ?? this.listErrorSignal());
  readonly loading = computed(() => this.listStatusSignal() === 'loading' || this.detailStatusSignal() === 'loading');
  readonly empty = computed(() => this.listStatusSignal() === 'empty');
  readonly forbidden = computed(() => this.listStatusSignal() === 'forbidden');
  readonly permissions = computed<IntegrationPermissionsState>(() => {
    const permissions = this.user()?.permissions ?? [];
    return {
      canRead: permissions.includes('Integrations.Read') || permissions.includes('CanReadAch'),
      canManage: permissions.includes('Integrations.ManageContracts') || permissions.includes('CanManageAch')
    };
  });
  readonly activeContract = computed(() => this.versionsSignal().find((version) => version.status === 'Active') ?? null);
  readonly versionRows = computed<IntegrationContractVersionReadModel[]>(() =>
    this.versionsSignal().map((version) => toIntegrationContractVersionReadModel(version, this.mappingsSignal()))
  );
  readonly selectedVersion = computed<IntegrationContractVersionReadModel | null>(() => {
    const id = this.detailSignal()?.contract.id;
    return id ? this.versionRows().find((version) => version.id === id) ?? null : null;
  });
  readonly parameters = computed<IntegrationParameterReadModel[]>(() =>
    (this.detailSignal()?.parameters ?? [])
      .filter((parameter) => parameter.isActive)
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .map((parameter) => toIntegrationParameterReadModel(parameter))
  );

  loadForOperation(operationKey: string): void {
    const normalized = operationKey.trim();
    this.operationKeySignal.set(normalized || null);
    this.resetListState();

    if (!this.permissions().canRead) {
      this.applyForbidden('No tiene permisos para consultar las versiones contractuales.');
      return;
    }

    if (!normalized) {
      this.listErrorSignal.set({ kind: 'unknown', status: null, message: 'Seleccione un servicio para consultar sus versiones.' });
      this.listStatusSignal.set('error');
      return;
    }

    this.listStatusSignal.set('loading');
    this.workspaceApi.getMethods().pipe(
      map((methods) => methods.find((method) => method.isActive && (method.operationKey === normalized || method.code === normalized)) ?? null),
      switchMap((method) => {
        if (!method) {
          return throwError(() => new Error(`No se encontró el servicio ${normalized}.`));
        }

        this.methodSignal.set(method);
        this.methodIdSignal.set(method.id);
        return forkJoin({
          versions: this.api.getByMethod(method.id),
          mappings: this.mappingApi.getByMethod(method.id)
        });
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: ({ versions, mappings }) => this.applyList(versions, mappings),
      error: (error: unknown) => this.applyError(error, this.listStatusSignal, this.listErrorSignal)
    });
  }

  loadVersions(methodId: number): void {
    this.operationKeySignal.set(null);
    this.methodSignal.set(null);
    this.methodIdSignal.set(methodId);
    this.resetDetailState();
    this.listErrorSignal.set(null);

    if (!this.permissions().canRead) {
      this.applyForbidden('No tiene permisos para consultar las versiones contractuales.');
      return;
    }

    this.listStatusSignal.set('loading');
    forkJoin({
      versions: this.api.getByMethod(methodId),
      mappings: this.mappingApi.getByMethod(methodId)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ versions, mappings }) => this.applyList(versions, mappings),
      error: (error: unknown) => this.applyError(error, this.listStatusSignal, this.listErrorSignal)
    });
  }

  loadDetail(contractVersionId: string): void {
    this.detailStatusSignal.set('loading');
    this.detailErrorSignal.set(null);
    this.clearParameterSaveState();

    if (!this.permissions().canRead) {
      this.detailSignal.set(null);
      this.applyError(
        { kind: 'forbidden', status: 403, message: 'No tiene permisos para consultar el detalle contractual.' } satisfies IntegrationErrorState,
        this.detailStatusSignal,
        this.detailErrorSignal
      );
      return;
    }

    this.api.getById(contractVersionId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.detailSignal.set(detail);
        this.detailStatusSignal.set('ready');
      },
      error: (error: unknown) => {
        this.detailSignal.set(null);
        this.applyError(error, this.detailStatusSignal, this.detailErrorSignal);
      }
    });
  }

  saveParameterPresentation(parameterId: number, input: UpdateContractFunctionalMetadataInput): void {
    const detail = this.detailSignal();
    const canEdit = detail?.contract.capabilities?.canEditFunctionalMetadata === true;
    const actor = this.user()?.username?.trim();
    if (!detail || !this.permissions().canManage || !canEdit || !actor) {
      this.parameterSaveErrorSignal.set({
        kind: 'forbidden',
        status: 403,
        message: 'El backend o sus permisos no permiten editar esta versión contractual.'
      });
      this.parameterSaveStatusSignal.set('forbidden');
      return;
    }

    this.parameterSaveStatusSignal.set('loading');
    this.parameterSaveErrorSignal.set(null);
    const request: UpdateIntegrationContractParameterPresentationRequest = {
      ...input,
      expectedRevision: detail.contract.revision,
      actor
    };

    this.api.updateParameterPresentation(detail.contract.id, parameterId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          const current = this.detailSignal();
          if (!current || current.contract.id !== result.contractVersionId) {
            this.parameterSaveStatusSignal.set('ready');
            return;
          }

          this.detailSignal.set({
            contract: { ...current.contract, revision: result.contractRevision },
            parameters: current.parameters.map((parameter) => parameter.id === result.parameter.id ? result.parameter : parameter)
          });
          this.versionsSignal.update((versions) => versions.map((version) =>
            version.id === result.contractVersionId ? { ...version, revision: result.contractRevision } : version
          ));
          this.parameterSaveStatusSignal.set('ready');
        },
        error: (error: unknown) => this.applyError(error, this.parameterSaveStatusSignal, this.parameterSaveErrorSignal)
      });
  }

  selectArtifact(file: File | null): void {
    this.artifactFileSignal.set(file);
    this.artifactAnalysisSignal.set(null);
    this.artifactPreviewSignal.set(null);
    this.artifactImportResultSignal.set(null);
    this.artifactAnalysisStatusSignal.set('idle');
    this.artifactPreviewStatusSignal.set('idle');
    this.artifactImportStatusSignal.set('idle');
    this.artifactAnalysisErrorSignal.set(null);
    this.artifactPreviewErrorSignal.set(null);
    this.artifactImportErrorSignal.set(null);
  }

  analyzeArtifact(operationName?: string | null, standaloneXsdSoapAction?: string | null): void {
    const file = this.artifactFileSignal();
    if (!this.permissions().canManage) {
      this.artifactAnalysisErrorSignal.set({ kind: 'forbidden', status: 403, message: 'No tiene permisos para analizar o importar contratos.' });
      this.artifactAnalysisStatusSignal.set('forbidden');
      return;
    }
    if (!file) {
      this.artifactAnalysisErrorSignal.set({ kind: 'unknown', status: null, message: 'Seleccione un archivo WSDL o XSD local.' });
      this.artifactAnalysisStatusSignal.set('error');
      return;
    }

    this.artifactAnalysisStatusSignal.set('loading');
    this.artifactAnalysisErrorSignal.set(null);
    this.artifactPreviewSignal.set(null);
    this.artifactPreviewStatusSignal.set('idle');
    this.artifactPreviewErrorSignal.set(null);
    this.artifactImportResultSignal.set(null);
    this.artifactImportStatusSignal.set('idle');
    this.artifactImportErrorSignal.set(null);

    this.api.analyze({ file, operationName, standaloneXsdSoapAction })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (analysis) => {
          this.artifactAnalysisSignal.set(analysis);
          this.artifactAnalysisStatusSignal.set('ready');
        },
        error: (error: unknown) => this.applyError(error, this.artifactAnalysisStatusSignal, this.artifactAnalysisErrorSignal)
      });
  }

  previewArtifact(
    baseContractVersionId: string,
    operationName?: string | null,
    standaloneXsdSoapAction?: string | null
  ): void {
    const file = this.artifactFileSignal();
    const methodId = this.methodIdSignal();
    const analysis = this.artifactAnalysisSignal();
    if (!this.permissions().canManage) {
      this.artifactPreviewErrorSignal.set({ kind: 'forbidden', status: 403, message: 'No tiene permisos para revisar cambios contractuales.' });
      this.artifactPreviewStatusSignal.set('forbidden');
      return;
    }
    if (!file || methodId == null || !analysis?.importable || !baseContractVersionId) {
      this.artifactPreviewErrorSignal.set({
        kind: 'unknown',
        status: null,
        message: 'Complete un análisis válido y seleccione una versión base antes de revisar cambios.'
      });
      this.artifactPreviewStatusSignal.set('error');
      return;
    }

    this.artifactPreviewStatusSignal.set('loading');
    this.artifactPreviewErrorSignal.set(null);
    this.artifactPreviewSignal.set(null);
    this.api.preview(methodId, baseContractVersionId, { file, operationName, standaloneXsdSoapAction })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (preview) => {
          this.artifactPreviewSignal.set(preview);
          this.artifactAnalysisSignal.set(preview.analysis);
          if (preview.analysis.importable && preview.diff) {
            this.artifactPreviewStatusSignal.set('ready');
          } else {
            this.artifactPreviewErrorSignal.set({
              kind: 'unknown',
              status: null,
              message: preview.analysis.issues[0]?.message ?? 'El backend no produjo un diff importable para este artefacto.'
            });
            this.artifactPreviewStatusSignal.set('error');
          }
        },
        error: (error: unknown) => this.applyError(error, this.artifactPreviewStatusSignal, this.artifactPreviewErrorSignal)
      });
  }

  importArtifactDraft(
    versionLabel: string,
    notes: string,
    operationName?: string | null,
    standaloneXsdSoapAction?: string | null
  ): void {
    const file = this.artifactFileSignal();
    const methodId = this.methodIdSignal();
    const preview = this.artifactPreviewSignal();
    if (!this.permissions().canManage) {
      this.artifactImportErrorSignal.set({ kind: 'forbidden', status: 403, message: 'No tiene permisos para importar contratos.' });
      this.artifactImportStatusSignal.set('forbidden');
      return;
    }
    if (!file || methodId == null || !preview?.analysis.importable || !preview.diff) {
      this.artifactImportErrorSignal.set({
        kind: 'unknown',
        status: null,
        message: 'Revise primero el análisis y el diff backend antes de crear el borrador.'
      });
      this.artifactImportStatusSignal.set('error');
      return;
    }

    const normalizedLabel = versionLabel.trim();
    if (!normalizedLabel) {
      this.artifactImportErrorSignal.set({ kind: 'unknown', status: null, message: 'La versión del nuevo borrador es obligatoria.' });
      this.artifactImportStatusSignal.set('error');
      return;
    }

    this.artifactImportStatusSignal.set('loading');
    this.artifactImportErrorSignal.set(null);
    this.artifactImportResultSignal.set(null);
    this.api.import(methodId, {
      file,
      versionLabel: normalizedLabel,
      notes: notes.trim() || null,
      operationName,
      standaloneXsdSoapAction
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (result.contract.contract.status !== 'Draft') {
          this.artifactImportErrorSignal.set({
            kind: 'unknown',
            status: null,
            message: 'El backend no confirmó estado Draft; la UI no considerará exitosa esta importación.'
          });
          this.artifactImportStatusSignal.set('error');
          return;
        }

        this.artifactImportResultSignal.set(result);
        this.versionsSignal.update((versions) => [
          result.contract.contract,
          ...versions.filter((version) => version.id !== result.contract.contract.id)
        ]);
        this.artifactImportStatusSignal.set('ready');
      },
      error: (error: unknown) => this.applyError(error, this.artifactImportStatusSignal, this.artifactImportErrorSignal)
    });
  }

  resetArtifactImport(): void {
    this.selectArtifact(null);
  }

  clearParameterSaveState(): void {
    this.parameterSaveStatusSignal.set('idle');
    this.parameterSaveErrorSignal.set(null);
  }

  clearDetail(): void {
    this.resetDetailState();
  }

  reload(): void {
    const operationKey = this.operationKeySignal();
    if (operationKey) {
      this.loadForOperation(operationKey);
      return;
    }

    const methodId = this.methodIdSignal();
    if (methodId != null) {
      this.loadVersions(methodId);
    }
  }

  private applyList(versions: IntegrationContractVersionDto[], mappings: IntegrationMappingSetDto[]): void {
    this.versionsSignal.set(versions);
    this.mappingsSignal.set(mappings);
    this.listStatusSignal.set(versions.length === 0 ? 'empty' : 'ready');
  }

  private resetListState(): void {
    this.methodSignal.set(null);
    this.methodIdSignal.set(null);
    this.versionsSignal.set([]);
    this.mappingsSignal.set([]);
    this.listErrorSignal.set(null);
    this.listStatusSignal.set('idle');
    this.resetDetailState();
    this.resetArtifactImport();
  }

  private resetDetailState(): void {
    this.detailSignal.set(null);
    this.detailStatusSignal.set('idle');
    this.detailErrorSignal.set(null);
    this.clearParameterSaveState();
  }

  private applyForbidden(message: string): void {
    this.listErrorSignal.set({ kind: 'forbidden', status: 403, message });
    this.listStatusSignal.set('forbidden');
  }

  private applyError(
    error: unknown,
    target: { set(value: IntegrationLoadStatus): void },
    errorTarget: { set(value: IntegrationErrorState | null): void }
  ): void {
    const state = isIntegrationErrorState(error) ? error : toIntegrationErrorState(error);
    errorTarget.set(state);
    target.set(statusFromError(state));
  }
}

function isIntegrationErrorState(value: unknown): value is IntegrationErrorState {
  if (!value || typeof value !== 'object') return false;
  const record = value as Partial<IntegrationErrorState>;
  return typeof record.kind === 'string' && typeof record.message === 'string';
}
