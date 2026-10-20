import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Observable, catchError, forkJoin, map, of, switchMap, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { IntegrationConnectionApi } from '../data-access/integration-connection.api';
import { IntegrationContractApi } from '../data-access/integration-contract.api';
import { IntegrationMappingApi } from '../data-access/integration-mapping.api';
import {
  IntegrationConnectionMethodDto,
  IntegrationConnectionSettingsDto,
  IntegrationContractVersionDto,
  IntegrationMappingCoverageSummaryDto,
  IntegrationMappingSetDto,
  IntegrationMethodDto
} from '../data-access/integration-api.models';
import { IntegrationServiceReadModel, toIntegrationServiceReadModel } from '../data-access/integration-read.models';
import { IntegrationWorkspaceApi } from '../data-access/integration-workspace.api';
import {
  IntegrationErrorState,
  IntegrationLoadStatus,
  IntegrationPermissionsState,
  statusFromError,
  toIntegrationErrorState
} from './integration-state.models';

interface PublishedMappingSnapshot {
  mapping: IntegrationMappingSetDto | null;
  coverage: IntegrationMappingCoverageSummaryDto | null;
}

const CFA_SERVICE_ORDER = [
  'Proc_Transacciones',
  'Proc_Contrapartidas',
  'RegistrarRespuestaTransaccion'
] as const;

const EMPTY_CONNECTION_SETTINGS: IntegrationConnectionSettingsDto = {
  revision: '',
  wscfaachMappings: [],
  wsAxonRespuestaTransaccionesMappings: []
};

@Injectable()
export class IntegrationCenterStore {
  private readonly api = inject(IntegrationWorkspaceApi);
  private readonly contractApi = inject(IntegrationContractApi);
  private readonly mappingApi = inject(IntegrationMappingApi);
  private readonly connectionApi = inject(IntegrationConnectionApi);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly user = toSignal(this.auth.user$, { initialValue: this.auth.getCurrentUser() });

  private readonly methodsSignal = signal<IntegrationMethodDto[]>([]);
  private readonly servicesSignal = signal<IntegrationServiceReadModel[]>([]);
  private readonly statusSignal = signal<IntegrationLoadStatus>('idle');
  private readonly errorSignal = signal<IntegrationErrorState | null>(null);
  private readonly selectedMethodIdSignal = signal<number | null>(null);

  readonly methods = this.methodsSignal.asReadonly();
  readonly services = this.servicesSignal.asReadonly();
  readonly status = this.statusSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly selectedMethodId = this.selectedMethodIdSignal.asReadonly();
  readonly loading = computed(() => this.statusSignal() === 'loading');
  readonly empty = computed(() => this.statusSignal() === 'empty');
  readonly forbidden = computed(() => this.statusSignal() === 'forbidden');
  readonly permissions = computed<IntegrationPermissionsState>(() => {
    const permissions = this.user()?.permissions ?? [];
    return {
      canRead: permissions.includes('Integrations.Read') || permissions.includes('CanReadAch'),
      canManage: permissions.includes('CanManageAch')
    };
  });
  readonly selectedService = computed(() =>
    this.servicesSignal().find((service) => service.methodId === this.selectedMethodIdSignal()) ?? null
  );

  load(): void {
    if (!this.permissions().canRead) {
      this.methodsSignal.set([]);
      this.servicesSignal.set([]);
      this.errorSignal.set({
        kind: 'forbidden',
        status: 403,
        message: 'No tiene permisos para consultar el Centro de integración CFA.'
      });
      this.statusSignal.set('forbidden');
      return;
    }

    this.statusSignal.set('loading');
    this.errorSignal.set(null);

    this.api.getMethods().pipe(
      map((methods) => this.selectCfaServices(methods)),
      switchMap((methods) => {
        this.methodsSignal.set(methods);
        if (methods.length === 0) {
          return of([] as IntegrationServiceReadModel[]);
        }

        return this.optionalRead(this.connectionApi.getSettings(), EMPTY_CONNECTION_SETTINGS).pipe(
          switchMap((settings) => forkJoin(methods.map((method) => this.loadService(method, settings))))
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (services) => {
        this.servicesSignal.set(services);
        this.statusSignal.set(services.length === 0 ? 'empty' : 'ready');
      },
      error: (error: unknown) => {
        const state = toIntegrationErrorState(error);
        this.methodsSignal.set([]);
        this.servicesSignal.set([]);
        this.errorSignal.set(state);
        this.statusSignal.set(statusFromError(state));
      }
    });
  }

  selectMethod(methodId: number | null): void {
    this.selectedMethodIdSignal.set(methodId);
  }

  private loadService(
    method: IntegrationMethodDto,
    settings: IntegrationConnectionSettingsDto
  ): Observable<IntegrationServiceReadModel> {
    return forkJoin({
      contract: this.loadActiveContract(method.id),
      mappingSnapshot: this.loadPublishedMapping(method.id)
    }).pipe(
      map(({ contract, mappingSnapshot }) => toIntegrationServiceReadModel(method, {
        contract,
        mapping: mappingSnapshot.mapping,
        mappingCoverage: mappingSnapshot.coverage,
        connection: this.findConnection(settings, method.operationKey)
      }))
    );
  }

  private loadActiveContract(methodId: number): Observable<IntegrationContractVersionDto | null> {
    return this.optionalRead(this.contractApi.getByMethod(methodId), [] as IntegrationContractVersionDto[]).pipe(
      map((contracts) => contracts.find((contract) => contract.status === 'Active') ?? null)
    );
  }

  private loadPublishedMapping(methodId: number): Observable<PublishedMappingSnapshot> {
    return this.optionalRead(this.mappingApi.getPublished(methodId), null).pipe(
      switchMap((mapping) => {
        if (!mapping) {
          return of({ mapping: null, coverage: null });
        }

        return this.optionalRead(this.mappingApi.validate(mapping.id), null).pipe(
          map((validation) => ({
            mapping,
            coverage: validation?.coverage ?? null
          }))
        );
      })
    );
  }

  private findConnection(
    settings: IntegrationConnectionSettingsDto,
    methodName: string
  ): IntegrationConnectionMethodDto | null {
    return [
      ...settings.wscfaachMappings,
      ...settings.wsAxonRespuestaTransaccionesMappings
    ].find((mapping) => mapping.methodName === methodName) ?? null;
  }

  private selectCfaServices(methods: IntegrationMethodDto[]): IntegrationMethodDto[] {
    const order = new Map<string, number>(CFA_SERVICE_ORDER.map((methodName, index) => [methodName, index]));
    return methods
      .filter((method) => method.isActive && method.integrationKey === 'CFA' && order.has(method.operationKey))
      .sort((left, right) => (order.get(left.operationKey) ?? 99) - (order.get(right.operationKey) ?? 99));
  }

  private optionalRead<T>(request: Observable<T>, fallback: T): Observable<T> {
    return request.pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && (error.status === 403 || error.status === 404)) {
          return of(fallback);
        }
        return throwError(() => error);
      })
    );
  }
}
