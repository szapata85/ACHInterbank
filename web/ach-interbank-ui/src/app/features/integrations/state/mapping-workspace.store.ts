import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, switchMap } from 'rxjs/operators';
import { IntegrationMappingPreviewResultDto, IntegrationMappingSetDto, IntegrationMappingWorkspaceDto, IntegrationMethodDto } from '../data-access/integration-api.models';
import { IntegrationMappingApi } from '../data-access/integration-mapping.api';
import { IntegrationWorkspaceApi } from '../data-access/integration-workspace.api';
import {
  IntegrationErrorState,
  IntegrationLoadStatus,
  statusFromError,
  toIntegrationErrorState
} from './integration-state.models';

export interface ParameterMappingSelection {
  sourceCatalogFieldId: number;
  transformationCode: string | null;
  formatMask: string | null;
}

export type MappingRevisionConflictOperation = 'parameter' | 'publication';

export interface MappingRevisionConflictState {
  operation: MappingRevisionConflictOperation;
  functionalMessage: string;
  technicalMessage: string;
  expectedRevision: number | null;
  currentRevision: number | null;
}

@Injectable()
export class MappingWorkspaceStore {
  private readonly api = inject(IntegrationMappingApi);
  private readonly workspaceApi = inject(IntegrationWorkspaceApi);
  private readonly destroyRef = inject(DestroyRef);

  private readonly methodsSignal = signal<IntegrationMethodDto[]>([]);
  private readonly methodIdSignal = signal<number | null>(null);
  private readonly mappingSetsSignal = signal<IntegrationMappingSetDto[]>([]);
  private readonly workspaceSignal = signal<IntegrationMappingWorkspaceDto | null>(null);
  private readonly previewSignal = signal<IntegrationMappingPreviewResultDto | null>(null);
  private readonly previewStatusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly previewErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly statusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly errorSignal = signal<IntegrationErrorState | null>(null);
  private readonly savingParameterIdSignal = signal<number | null>(null);
  private readonly saveErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly saveSucceededParameterIdSignal = signal<number | null>(null);
  private readonly revisionConflictSignal = signal<MappingRevisionConflictState | null>(null);
  private readonly publishingSignal = signal(false);
  private readonly publicationErrorSignal = signal<IntegrationErrorState | null>(null);
  private readonly publicationSucceededSignal = signal(false);
  private requestVersion = 0;
  private previewRequestVersion = 0;
  private previewAuthorized = false;

  readonly methods = this.methodsSignal.asReadonly();
  readonly methodId = this.methodIdSignal.asReadonly();
  readonly mappingSets = this.mappingSetsSignal.asReadonly();
  readonly workspace = this.workspaceSignal.asReadonly();
  readonly preview = this.previewSignal.asReadonly();
  readonly previewStatus = this.previewStatusSignal.asReadonly();
  readonly previewError = this.previewErrorSignal.asReadonly();
  readonly status = this.statusSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly savingParameterId = this.savingParameterIdSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();
  readonly saveSucceededParameterId = this.saveSucceededParameterIdSignal.asReadonly();
  readonly revisionConflict = this.revisionConflictSignal.asReadonly();
  readonly publishing = this.publishingSignal.asReadonly();
  readonly publicationError = this.publicationErrorSignal.asReadonly();
  readonly publicationSucceeded = this.publicationSucceededSignal.asReadonly();
  readonly loading = computed(() => this.statusSignal() === 'loading');
  readonly previewLoading = computed(() => this.previewStatusSignal() === 'loading');
  readonly empty = computed(() => this.statusSignal() === 'empty');
  readonly selectedMethod = computed(() => {
    const methodId = this.methodIdSignal();
    return this.methodsSignal().find((method) => method.id === methodId) ?? null;
  });
  readonly coverage = computed(() => this.workspaceSignal()?.coverage ?? null);
  readonly parameters = computed(() => this.workspaceSignal()?.parameters ?? []);
  readonly canPublish = computed(() => this.workspaceSignal()?.publication.canPublish === true);

  load(requestedMethodCode?: string | null, previewAuthorized = false): void {
    this.previewAuthorized = previewAuthorized;
    const requestVersion = ++this.requestVersion;
    this.statusSignal.set('loading');
    this.errorSignal.set(null);
    this.saveErrorSignal.set(null);
    this.saveSucceededParameterIdSignal.set(null);
    this.revisionConflictSignal.set(null);
    this.publicationErrorSignal.set(null);
    this.publicationSucceededSignal.set(false);
    this.workspaceSignal.set(null);
    this.previewRequestVersion++;
    this.previewSignal.set(null);
    this.previewStatusSignal.set('idle');
    this.previewErrorSignal.set(null);
    this.mappingSetsSignal.set([]);

    this.workspaceApi.getMethods()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (methods) => {
          if (requestVersion !== this.requestVersion) {
            return;
          }

          const activeMethods = (methods ?? []).filter((method) => method.isActive);
          this.methodsSignal.set(activeMethods);
          if (activeMethods.length === 0) {
            this.methodIdSignal.set(null);
            this.statusSignal.set('empty');
            return;
          }

          const requested = this.normalize(requestedMethodCode);
          const selected = activeMethods.find((method) =>
            this.normalize(method.operationKey) === requested || this.normalize(method.code) === requested)
            ?? activeMethods[0];
          this.loadMethod(selected.id, requestVersion);
        },
        error: (error: unknown) => {
          if (requestVersion !== this.requestVersion) {
            return;
          }
          this.setError(error);
        }
      });
  }

  selectMethod(methodId: number): void {
    if (!this.methodsSignal().some((method) => method.id === methodId)) {
      return;
    }

    const requestVersion = ++this.requestVersion;
    this.loadMethod(methodId, requestVersion);
  }

  reload(): void {
    const selected = this.selectedMethod();
    if (!selected) {
      this.load(undefined, this.previewAuthorized);
      return;
    }

    const requestVersion = ++this.requestVersion;
    this.loadMethod(selected.id, requestVersion);
  }

  saveParameter(parameterId: number, selection: ParameterMappingSelection, updatedBy: string): void {
    const workspace = this.workspaceSignal();
    if (!workspace || this.savingParameterIdSignal() !== null) {
      return;
    }

    const compatibility = workspace.sourceCompatibility.find(item =>
      item.parameterId === parameterId && item.sourceCatalogFieldId === selection.sourceCatalogFieldId);
    if (!compatibility) {
      this.saveErrorSignal.set(toIntegrationErrorState(new Error('Seleccione un origen disponible para este dato CFA.')));
      return;
    }

    const transformationCode = selection.transformationCode?.trim() || null;
    if (transformationCode === null && !compatibility.allowsNoTransformation) {
      this.saveErrorSignal.set(toIntegrationErrorState(new Error('Seleccione un tratamiento compatible informado por el servicio.')));
      return;
    }

    const transformation = transformationCode === null
      ? null
      : compatibility.transformations.find(item => item.code === transformationCode) ?? null;
    if (transformationCode !== null && !transformation) {
      this.saveErrorSignal.set(toIntegrationErrorState(new Error('El tratamiento seleccionado ya no está disponible para este origen.')));
      return;
    }

    if (transformation?.requiresFormatMask && !selection.formatMask?.trim()) {
      this.saveErrorSignal.set(toIntegrationErrorState(new Error('Complete la configuración requerida por el tratamiento seleccionado.')));
      return;
    }

    this.saveErrorSignal.set(null);
    this.saveSucceededParameterIdSignal.set(null);
    this.revisionConflictSignal.set(null);
    this.publicationSucceededSignal.set(false);
    this.savingParameterIdSignal.set(parameterId);

    this.api.updateParameterMapping(workspace.mappingSet.id, parameterId, {
      expectedRevision: workspace.mappingSet.revision,
      sourceCatalogFieldId: selection.sourceCatalogFieldId,
      transformationCode,
      formatMask: selection.formatMask?.trim() || null,
      updatedBy: updatedBy.trim() || 'usuario'
    }).pipe(
      switchMap(() => this.api.getWorkspace(workspace.mappingSet.id)),
      finalize(() => this.savingParameterIdSignal.set(null)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (refreshed) => {
        this.workspaceSignal.set(refreshed);
        this.statusSignal.set(refreshed.parameters.length === 0 ? 'empty' : 'ready');
        this.saveSucceededParameterIdSignal.set(parameterId);
        if (refreshed.parameters.length > 0) {
          this.loadPreview(refreshed.mappingSet.id, this.requestVersion);
        }
      },
      error: (error: unknown) => {
        const conflict = toRevisionConflictState(error, 'parameter');
        if (conflict) {
          this.revisionConflictSignal.set(conflict);
          this.saveErrorSignal.set(null);
          return;
        }
        this.saveErrorSignal.set(toIntegrationErrorState(error));
      }
    });
  }

  clearSaveState(): void {
    this.saveErrorSignal.set(null);
    this.saveSucceededParameterIdSignal.set(null);
  }

  reloadAfterRevisionConflict(): void {
    this.revisionConflictSignal.set(null);
    this.reload();
  }

  publish(publishedBy: string, publishNote?: string | null): void {
    const workspace = this.workspaceSignal();
    if (!workspace || this.publishingSignal() || this.savingParameterIdSignal() !== null) {
      return;
    }

    this.publicationErrorSignal.set(null);
    this.publicationSucceededSignal.set(false);
    this.revisionConflictSignal.set(null);

    if (workspace.mappingSet.status !== 'Draft'
        || workspace.publication.pendingRequired > 0
        || !workspace.publication.canPublish) {
      this.publicationErrorSignal.set(toIntegrationErrorState(new Error(
        workspace.publication.pendingRequired === 1
          ? 'Falta 1 dato requerido por CFA antes de publicar.'
          : workspace.publication.pendingRequired > 1
            ? `Faltan ${workspace.publication.pendingRequired} datos requeridos por CFA antes de publicar.`
            : 'El servicio indica que esta configuración todavía no es publicable.'
      )));
      return;
    }

    this.publishingSignal.set(true);
    this.api.publishWithRevision(workspace.mappingSet.id, {
      expectedRevision: workspace.mappingSet.revision,
      publishedBy: publishedBy.trim() || 'usuario',
      publishNote: publishNote?.trim() || null
    }).pipe(
      switchMap(() => this.api.getWorkspace(workspace.mappingSet.id)),
      finalize(() => this.publishingSignal.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (refreshed) => {
        this.workspaceSignal.set(refreshed);
        this.statusSignal.set(refreshed.parameters.length === 0 ? 'empty' : 'ready');
        this.publicationSucceededSignal.set(true);
        if (refreshed.parameters.length > 0) {
          this.loadPreview(refreshed.mappingSet.id, this.requestVersion);
        }
      },
      error: (error: unknown) => {
        const conflict = toRevisionConflictState(error, 'publication');
        if (conflict) {
          this.revisionConflictSignal.set(conflict);
          this.publicationErrorSignal.set(null);
          return;
        }
        this.publicationErrorSignal.set(toIntegrationErrorState(error));
      }
    });
  }

  refreshPreview(): void {
    const workspace = this.workspaceSignal();
    if (!workspace || workspace.parameters.length === 0) {
      return;
    }

    this.loadPreview(workspace.mappingSet.id, this.requestVersion);
  }

  private loadMethod(methodId: number, requestVersion: number): void {
    this.methodIdSignal.set(methodId);
    this.statusSignal.set('loading');
    this.errorSignal.set(null);
    this.saveErrorSignal.set(null);
    this.saveSucceededParameterIdSignal.set(null);
    this.revisionConflictSignal.set(null);
    this.publicationErrorSignal.set(null);
    this.publicationSucceededSignal.set(false);
    this.workspaceSignal.set(null);
    this.previewRequestVersion++;
    this.previewSignal.set(null);
    this.previewStatusSignal.set('idle');
    this.previewErrorSignal.set(null);
    this.mappingSetsSignal.set([]);

    this.api.getByMethod(methodId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (mappingSets) => {
          if (requestVersion !== this.requestVersion) {
            return;
          }

          const available = mappingSets ?? [];
          this.mappingSetsSignal.set(available);
          const selectedMapping = this.selectReadableMapping(available);
          if (!selectedMapping) {
            this.statusSignal.set('empty');
            return;
          }

          this.api.getWorkspace(selectedMapping.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (workspace) => {
                if (requestVersion !== this.requestVersion) {
                  return;
                }
                this.workspaceSignal.set(workspace);
                this.statusSignal.set(workspace.parameters.length === 0 ? 'empty' : 'ready');
                if (workspace.parameters.length > 0) {
                  this.loadPreview(workspace.mappingSet.id, requestVersion);
                }
              },
              error: (error: unknown) => {
                if (requestVersion !== this.requestVersion) {
                  return;
                }
                this.setError(error);
              }
            });
        },
        error: (error: unknown) => {
          if (requestVersion !== this.requestVersion) {
            return;
          }
          this.setError(error);
        }
      });
  }

  private loadPreview(mappingSetId: string, requestVersion: number): void {
    if (!this.previewAuthorized) {
      this.previewRequestVersion++;
      this.previewSignal.set(null);
      this.previewStatusSignal.set('forbidden');
      this.previewErrorSignal.set(null);
      return;
    }

    const workspace = this.workspaceSignal();
    const previewRequestVersion = ++this.previewRequestVersion;
    this.previewStatusSignal.set('loading');
    this.previewErrorSignal.set(null);
    this.previewSignal.set(null);

    this.api.preview(mappingSetId, {
      maxItems: Math.max(workspace?.parameters.length ?? 1, 1),
      useControlledSample: false
    }).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (preview) => {
          if (requestVersion !== this.requestVersion
              || previewRequestVersion !== this.previewRequestVersion
              || this.workspaceSignal()?.mappingSet.id !== mappingSetId) {
            return;
          }

          this.previewSignal.set(preview);
          this.previewStatusSignal.set('ready');
        },
        error: (error: unknown) => {
          if (requestVersion !== this.requestVersion
              || previewRequestVersion !== this.previewRequestVersion
              || this.workspaceSignal()?.mappingSet.id !== mappingSetId) {
            return;
          }

          const state = toIntegrationErrorState(error);
          this.previewErrorSignal.set(state);
          this.previewStatusSignal.set(statusFromError(state));
        }
      });
  }

  private selectReadableMapping(mappingSets: IntegrationMappingSetDto[]): IntegrationMappingSetDto | null {
    return mappingSets.find((mapping) => mapping.status === 'Draft' && mapping.isActive)
      ?? mappingSets.find((mapping) => mapping.status === 'Draft')
      ?? mappingSets.find((mapping) => mapping.status === 'Published' && mapping.isActive)
      ?? mappingSets.find((mapping) => mapping.status === 'Published')
      ?? mappingSets[0]
      ?? null;
  }

  private setError(error: unknown): void {
    const state = toIntegrationErrorState(error);
    this.workspaceSignal.set(null);
    this.errorSignal.set(state);
    this.statusSignal.set(statusFromError(state));
  }

  private normalize(value: string | null | undefined): string {
    return (value ?? '').trim().toLocaleLowerCase('es');
  }
}


function toRevisionConflictState(
  error: unknown,
  operation: MappingRevisionConflictOperation
): MappingRevisionConflictState | null {
  if (!(error instanceof HttpErrorResponse) || error.status !== 409) {
    return null;
  }

  const body = error.error && typeof error.error === 'object'
    ? error.error as Record<string, unknown>
    : {};
  const technicalMessage = typeof body['detail'] === 'string' && body['detail'].trim()
    ? body['detail'].trim()
    : error.message;

  return {
    operation,
    functionalMessage: operation === 'publication'
      ? 'Otra persona actualizó esta configuración antes de publicarla. Recargue los datos vigentes y revise los cambios antes de intentar de nuevo.'
      : 'Otra persona actualizó esta configuración mientras usted editaba. No se sobrescribieron esos cambios. Recargue los datos vigentes antes de continuar.',
    technicalMessage,
    expectedRevision: readRevision(body['expectedRevision']),
    currentRevision: readRevision(body['currentRevision'])
  };
}

function readRevision(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}
