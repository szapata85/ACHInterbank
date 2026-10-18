import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { finalize, switchMap, tap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  IntegrationConnectionChangePreviewDto,
  IntegrationConnectionChangePreviewRequestDto,
  IntegrationConnectionInputMappingDto,
  IntegrationConnectionMethodDto,
  IntegrationConnectionSettingsDto,
  IntegrationConnectionState
} from '../data-access/integration-api.models';
import { IntegrationConnectionApi } from '../data-access/integration-connection.api';

type SoapClientKey = 'wscfaachMappings' | 'wsAxonRespuestaTransaccionesMappings';

interface SoapMethodView {
  clientKey: SoapClientKey;
  clientName: string;
  index: number;
  group: FormGroup;
}

interface SoapServiceCopy {
  title: string;
  description: string;
  nature: string;
}

export interface RealSendConfirmationDialogData {
  serviceName: string;
  technicalName: string;
  sanitizedDestination: string;
}

const SERVICE_COPY: Record<string, SoapServiceCopy> = {
  Proc_Transacciones: {
    title: 'Créditos recibidos en CFA',
    description: 'Conexión técnica usada por el servicio de créditos monetarios recibidos desde otra entidad.',
    nature: 'Servicio monetario'
  },
  Proc_Contrapartidas: {
    title: 'Débitos originados en CFA',
    description: 'Conexión técnica usada por el servicio de débitos monetarios de contrapartida.',
    nature: 'Servicio monetario'
  },
  RegistrarRespuestaTransaccion: {
    title: 'Respuestas de transacciones',
    description: 'Conexión técnica usada para registrar respuestas, rechazos o notificaciones no monetarias.',
    nature: 'Servicio no monetario'
  }
};

@Component({
  selector: 'app-real-send-confirmation-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Confirmar configuración de envío real</h2>
    <mat-dialog-content>
      <p>
        Vas a habilitar o mantener un modo que permite transmisión real para
        <strong>{{ data.serviceName }}</strong>.
      </p>
      <p>Esto habilita comunicación externa desde la aplicación cuando el backend permita la ejecución.</p>
      <dl class="real-send-dialog__details">
        <div>
          <dt>Servicio</dt>
          <dd>{{ data.serviceName }}</dd>
        </div>
        <div>
          <dt>Nombre técnico</dt>
          <dd><code>{{ data.technicalName }}</code></dd>
        </div>
        <div>
          <dt>Destino sanitizado</dt>
          <dd><code data-testid="real-send-sanitized-destination">{{ data.sanitizedDestination }}</code></dd>
        </div>
      </dl>
      <p class="real-send-dialog__warning">
        Confirma solo si reconoces el servicio y el destino mostrados. La seguridad del endpoint se evalúa exclusivamente en backend.
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="false">Cancelar</button>
      <button mat-flat-button type="button" [mat-dialog-close]="true" data-testid="confirm-real-send">
        Confirmar envío real
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    :host { display: block; min-width: 0; max-width: 100%; }
    mat-dialog-content { max-width: 100%; overflow-wrap: anywhere; }
    .real-send-dialog__details { display: grid; gap: .75rem; margin: 1rem 0; min-width: 0; }
    .real-send-dialog__details div { display: grid; gap: .2rem; }
    .real-send-dialog__details dt { color: var(--mat-sys-on-surface-variant); font-size: .78rem; }
    .real-send-dialog__details dd { margin: 0; overflow-wrap: anywhere; }
    .real-send-dialog__warning { color: var(--mat-sys-on-surface-variant); }
    button { min-height: 2.75rem; white-space: normal; }
    button:focus-visible { outline: 3px solid var(--mat-sys-primary); outline-offset: 3px; }
    @media (max-width: 767px) {
      mat-dialog-actions { display: grid; grid-template-columns: 1fr; }
      button { width: 100%; min-height: 3rem; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RealSendConfirmationDialogComponent {
  readonly data = inject<RealSendConfirmationDialogData>(MAT_DIALOG_DATA);
}

@Component({
  selector: 'app-soap-integration-settings',
  standalone: true,
  imports: [
    SharedModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTabsModule
  ],
  templateUrl: './soap-integration-settings.component.html',
  styleUrls: ['./soap-integration-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SoapIntegrationSettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(IntegrationConnectionApi);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Conexiones', ruta: '/integraciones' },
    { etiqueta: 'Conexión entidad participante' }
  ];

  readonly form = this.fb.group({
    wscfaachMappings: this.fb.array<FormGroup>([]),
    wsAxonRespuestaTransaccionesMappings: this.fb.array<FormGroup>([])
  });

  readonly editorForm = this.fb.group({
    endpoint: ['', [Validators.required]],
    soapAction: ['', [Validators.required]],
    operatingMode: ['DryRun' as IntegrationConnectionMethodDto['operatingMode'], [Validators.required]],
    enabled: [true]
  });

  readonly canManage = this.auth.hasPermission(['Integrations.ManageConnections', 'CanManageAch']);
  readonly canRead = this.auth.hasPermission(['Integrations.Read', 'CanReadAch']);

  loading = this.canRead;
  saving = false;
  previewing = false;
  reloading = false;
  confirmationOpen = false;
  forbidden = !this.canRead;
  loadFailed = false;
  requiresReload = false;
  backendStateIncomplete = false;
  selectedClientKey: SoapClientKey | null = null;
  selectedMethodName: string | null = null;
  editingMethodCode: string | null = null;
  private persistedSettings: IntegrationConnectionSettingsDto | null = null;

  constructor() {
    this.editorForm.disable({ emitEvent: false });
    if (this.canRead) {
      this.loadSettings(false);
    }
  }

  get wscfaachMappings(): FormArray<FormGroup> {
    return this.form.get('wscfaachMappings') as FormArray<FormGroup>;
  }

  get wsAxonMappings(): FormArray<FormGroup> {
    return this.form.get('wsAxonRespuestaTransaccionesMappings') as FormArray<FormGroup>;
  }

  get allMethods(): SoapMethodView[] {
    return [
      ...this.wscfaachMappings.controls.map((group, index) => ({
        clientKey: 'wscfaachMappings' as const,
        clientName: 'WscfaachSoapClient',
        index,
        group
      })),
      ...this.wsAxonMappings.controls.map((group, index) => ({
        clientKey: 'wsAxonRespuestaTransaccionesMappings' as const,
        clientName: 'WsAxonRespuestaTransaccionesSoapClient',
        index,
        group
      }))
    ];
  }

  get selectedMethod(): SoapMethodView | null {
    return this.allMethods.find((method) => this.isSelected(method)) ?? this.allMethods[0] ?? null;
  }

  get selectedCopy(): SoapServiceCopy {
    const methodName = this.selectedMethod ? this.methodNameFor(this.selectedMethod.group) : '';
    return SERVICE_COPY[methodName] ?? {
      title: 'Servicio CFA',
      description: 'Conexión técnica administrada por backend.',
      nature: 'Configuración técnica'
    };
  }

  serviceCopyFor(method: SoapMethodView): SoapServiceCopy {
    return SERVICE_COPY[this.methodNameFor(method.group)] ?? {
      title: 'Servicio CFA',
      description: 'Conexión técnica administrada por backend.',
      nature: 'Configuración técnica'
    };
  }

  selectMethod(method: SoapMethodView): void {
    this.cancelEdit(false);
    this.selectedClientKey = method.clientKey;
    this.selectedMethodName = this.methodNameFor(method.group);
    this.syncEditor();
    this.cdr.markForCheck();
  }

  isSelected(method: SoapMethodView): boolean {
    return this.selectedClientKey === method.clientKey
      && this.selectedMethodName === this.methodNameFor(method.group);
  }

  get isEditingSelected(): boolean {
    const method = this.selectedMethod;
    return method !== null && this.editingMethodCode === this.methodCodeFor(method);
  }

  get canEditSelected(): boolean {
    const method = this.selectedMethod;
    return this.canManage
      && !this.requiresReload
      && !this.backendStateIncomplete
      && method !== null
      && method.group.get('configurationStateKnown')?.value === true;
  }

  beginEdit(): void {
    const method = this.selectedMethod;
    if (!method || !this.canEditSelected) {
      this.notifications.error('No es posible editar porque no se pudo determinar el permiso o el estado vigente.');
      return;
    }

    this.editingMethodCode = this.methodCodeFor(method);
    this.syncEditor();
    this.editorForm.enable({ emitEvent: false });
    this.editorForm.markAsPristine();
    this.cdr.markForCheck();
  }

  cancelEdit(showNotification = true): void {
    this.editingMethodCode = null;
    this.syncEditor();
    this.editorForm.disable({ emitEvent: false });
    if (showNotification) {
      this.notifications.success('Edición cancelada. No se guardaron cambios.');
    }
    this.cdr.markForCheck();
  }

  save(): void {
    if (!this.canManage || !this.isEditingSelected || this.saving || this.previewing || this.confirmationOpen || this.requiresReload) {
      return;
    }
    if (this.editorForm.invalid) {
      this.editorForm.markAllAsTouched();
      this.notifications.error('Completa los campos requeridos antes de guardar.');
      return;
    }
    if (!this.editorForm.dirty || !this.persistedSettings) {
      return;
    }

    const method = this.selectedMethod;
    const revision = this.persistedSettings.revision?.trim();
    const stateKnown = method?.group.get('configurationStateKnown')?.value === true;
    if (!method || !revision || !stateKnown || !this.canManage) {
      this.failClosed('No es posible guardar porque no se pudo determinar servicio, permiso, Revision o estado vigente.');
      return;
    }

    const payload = this.buildPayload();
    if (this.requiresRealSendConfirmation(method)) {
      this.previewAndConfirmRealSend(method, payload);
      return;
    }

    this.persistAndRefresh(payload);
  }

  reload(): void {
    if (!this.canRead || this.reloading || this.saving || this.previewing) {
      return;
    }
    this.loadSettings(true);
  }

  endpointFor(group: FormGroup): string {
    return group.get('sanitizedEndpoint')?.value || 'Destino no disponible';
  }

  technicalActionFor(group: FormGroup): string {
    return group.get('sanitizedSoapAction')?.value || 'Acción no disponible';
  }

  methodNameFor(group: FormGroup): string {
    return group.get('methodName')?.value || 'Método SOAP';
  }

  statusFor(group: FormGroup): string {
    const state = group.get('configurationState')?.value as IntegrationConnectionState | null;
    if (state === 'RealSendEnabled') {
      return 'Envío real habilitado';
    }
    if (state === 'Simulation') {
      return 'Simulación sin envío';
    }
    if (state === 'Disabled') {
      return 'Inactivo';
    }
    return 'Estado no determinado';
  }

  operatingModeFor(group: FormGroup): string {
    const mode = group.get('operatingMode')?.value;
    if (mode === 'Live') {
      return 'Envío real';
    }
    if (mode === 'Disabled') {
      return 'Inactivo';
    }
    return 'Simulación';
  }

  methodCodeFor(method: SoapMethodView): string {
    const integrationKey = method.clientKey === 'wscfaachMappings' ? 'WSCFAACH' : 'WSAXON';
    return `${integrationKey}.${this.methodNameFor(method.group)}`;
  }

  mappingCountFor(group: FormGroup): number {
    return this.getInputMappings(group).length;
  }

  readonly trackMethod = (_: number, method: SoapMethodView): string =>
    `${method.clientKey}.${method.group.get('methodName')?.value ?? method.index}`;

  get canSave(): boolean {
    return this.canManage
      && this.isEditingSelected
      && this.editorForm.valid
      && this.editorForm.dirty
      && !this.saving
      && !this.previewing
      && !this.confirmationOpen
      && !this.requiresReload
      && !this.backendStateIncomplete;
  }

  get selectedIndex(): number {
    const selected = this.selectedMethod;
    return selected
      ? this.allMethods.findIndex((method) => this.methodCodeFor(method) === this.methodCodeFor(selected))
      : 0;
  }

  onSelectedIndexChange(index: number): void {
    const method = this.allMethods[index];
    if (method) {
      this.selectMethod(method);
    }
  }

  private loadSettings(showSuccess: boolean): void {
    const currentSelection = this.captureSelection();
    this.loading = !showSuccess;
    this.reloading = showSuccess;
    this.loadFailed = false;

    this.api.getSettings()
      .pipe(finalize(() => {
        this.loading = false;
        this.reloading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (settings) => {
          this.forbidden = false;
          this.requiresReload = false;
          this.hydrate(settings, currentSelection);
          this.form.markAsPristine();
          if (showSuccess) {
            this.notifications.success('Configuración actualizada con la información vigente.');
          }
        },
        error: (error: unknown) => this.handleLoadError(error)
      });
  }

  private previewAndConfirmRealSend(
    method: SoapMethodView,
    payload: IntegrationConnectionSettingsDto
  ): void {
    const request = this.buildPreviewRequest(method);
    this.previewing = true;

    this.api.previewChange(request)
      .pipe(finalize(() => {
        this.previewing = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (preview) => this.openRealSendConfirmation(method, payload, preview),
        error: (error: unknown) => this.handleMutationError(error, false)
      });
  }

  private openRealSendConfirmation(
    method: SoapMethodView,
    payload: IntegrationConnectionSettingsDto,
    preview: IntegrationConnectionChangePreviewDto
  ): void {
    const revision = this.persistedSettings?.revision ?? '';
    const previewMatches = preview.methodName === this.methodNameFor(method.group)
      && preview.revision === revision
      && preview.currentConfigurationState !== 'Unknown'
      && preview.requestedConfigurationState !== 'Unknown';

    if (!previewMatches || !preview.canApply || !preview.sanitizedEndpoint) {
      this.failClosed(preview.blockingReason || 'Backend no pudo confirmar servicio, destino sanitizado o estado vigente.');
      return;
    }

    this.confirmationOpen = true;
    const ref = this.dialog.open(RealSendConfirmationDialogComponent, {
      data: {
        serviceName: this.serviceCopyFor(method).title,
        technicalName: this.methodNameFor(method.group),
        sanitizedDestination: preview.sanitizedEndpoint
      } satisfies RealSendConfirmationDialogData,
      width: 'min(520px, calc(100vw - 32px))',
      maxWidth: '100vw',
      maxHeight: 'calc(100dvh - 32px)',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      disableClose: false,
      closeOnNavigation: true,
      role: 'alertdialog',
      ariaLabel: `Confirmar envío real para ${this.serviceCopyFor(method).title}`
    });

    ref.afterClosed().subscribe((confirmed) => {
      this.confirmationOpen = false;
      if (confirmed === true) {
        this.persistAndRefresh(payload);
      }
      this.cdr.markForCheck();
    });
  }

  private persistAndRefresh(payload: IntegrationConnectionSettingsDto): void {
    const selection = this.captureSelection();
    let persisted = false;
    this.saving = true;

    this.api.updateSettings(payload)
      .pipe(
        tap(() => { persisted = true; }),
        switchMap(() => this.api.getSettings()),
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (freshSettings) => {
          this.hydrate(freshSettings, selection);
          this.form.markAsPristine();
          this.editingMethodCode = null;
          this.editorForm.markAsPristine();
          this.notifications.success('Configuración técnica guardada y actualizada con el estado vigente.');
        },
        error: (error: unknown) => this.handleMutationError(error, persisted)
      });
  }

  private handleLoadError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 403) {
      this.forbidden = true;
      this.loadFailed = false;
      this.notifications.error('No tienes permiso para consultar la conexión de la entidad participante.');
      return;
    }

    this.loadFailed = true;
    this.notifications.error('No fue posible cargar la conexión de la entidad participante.');
  }

  private handleMutationError(error: unknown, persistedBeforeRefresh: boolean): void {
    if (error instanceof HttpErrorResponse && error.status === 409) {
      this.requiresReload = true;
      this.editorForm.disable({ emitEvent: false });
      this.notifications.error('La configuración cambió en el sistema. Recarga antes de intentar guardar nuevamente.');
      return;
    }

    if (error instanceof HttpErrorResponse && error.status === 403) {
      this.forbidden = true;
      this.editorForm.disable({ emitEvent: false });
      this.notifications.error('No tienes permiso para modificar la conexión de la entidad participante.');
      return;
    }

    if (persistedBeforeRefresh) {
      this.requiresReload = true;
      this.editorForm.disable({ emitEvent: false });
      this.notifications.error('El cambio fue recibido por el sistema, pero no fue posible confirmar el estado actualizado. Recarga la configuración.');
      return;
    }

    this.notifications.error('No fue posible guardar la conexión de la entidad participante. Conservamos los cambios para que puedas volver a intentar.');
  }

  private failClosed(message: string): void {
    this.notifications.error(message);
    this.cdr.markForCheck();
  }

  private hydrate(
    settings: IntegrationConnectionSettingsDto,
    selection = this.captureSelection()
  ): void {
    this.persistedSettings = this.cloneSettings(settings);
    this.backendStateIncomplete = !settings.revision?.trim();
    this.setMappings(this.wscfaachMappings, this.persistedSettings.wscfaachMappings);
    this.setMappings(this.wsAxonMappings, this.persistedSettings.wsAxonRespuestaTransaccionesMappings);
    this.restoreSelection(selection);
    this.syncEditor();
    this.editorForm.disable({ emitEvent: false });
    this.editingMethodCode = null;
  }

  private captureSelection(): { clientKey: SoapClientKey | null; methodName: string | null } {
    return {
      clientKey: this.selectedClientKey,
      methodName: this.selectedMethodName
    };
  }

  private restoreSelection(selection: { clientKey: SoapClientKey | null; methodName: string | null }): void {
    const existing = this.allMethods.find((method) =>
      method.clientKey === selection.clientKey && this.methodNameFor(method.group) === selection.methodName
    );
    const requestedMethodName = this.route.snapshot.queryParamMap.get('method');
    const requested = requestedMethodName
      ? this.allMethods.find((method) =>
        this.methodNameFor(method.group) === requestedMethodName || this.methodCodeFor(method) === requestedMethodName
      )
      : null;
    const preferred = existing
      ?? requested
      ?? this.allMethods.find((method) => this.methodNameFor(method.group) === 'Proc_Transacciones')
      ?? this.allMethods[0]
      ?? null;

    this.selectedClientKey = preferred?.clientKey ?? null;
    this.selectedMethodName = preferred ? this.methodNameFor(preferred.group) : null;
  }

  private buildPayload(): IntegrationConnectionSettingsDto {
    const payload = this.cloneSettings(this.persistedSettings!);
    payload.expectedRevision = this.persistedSettings!.revision;
    const selected = this.selectedMethod!;
    const collection = payload[selected.clientKey];
    const methodName = this.methodNameFor(selected.group);
    const index = collection.findIndex((item) => item.methodName === methodName);
    if (index < 0) {
      return payload;
    }

    collection[index] = {
      ...collection[index],
      methodName,
      endpoint: this.editorForm.controls.endpoint.value!.trim(),
      soapAction: this.editorForm.controls.soapAction.value!.trim(),
      operatingMode: this.editorForm.controls.operatingMode.value!,
      enabled: this.editorForm.controls.enabled.value ?? false,
      inputParameterMappings: collection[index].inputParameterMappings.map((item) => ({ ...item }))
    };
    return payload;
  }

  private buildPreviewRequest(method: SoapMethodView): IntegrationConnectionChangePreviewRequestDto {
    return {
      methodName: this.methodNameFor(method.group),
      endpoint: this.editorForm.controls.endpoint.value!.trim(),
      soapAction: this.editorForm.controls.soapAction.value!.trim(),
      operatingMode: this.editorForm.controls.operatingMode.value!,
      enabled: this.editorForm.controls.enabled.value ?? false,
      expectedRevision: this.persistedSettings!.revision
    };
  }

  private requiresRealSendConfirmation(method: SoapMethodView): boolean {
    const targetReal = this.editorForm.controls.operatingMode.value === 'Live'
      && this.editorForm.controls.enabled.value === true;
    if (!targetReal) {
      return false;
    }

    const currentReal = method.group.get('operatingMode')?.value === 'Live'
      && method.group.get('enabled')?.value === true;
    const endpointChanged = this.editorForm.controls.endpoint.value?.trim() !== method.group.get('endpoint')?.value;
    const actionChanged = this.editorForm.controls.soapAction.value?.trim() !== method.group.get('soapAction')?.value;
    return !currentReal || endpointChanged || actionChanged;
  }

  private syncEditor(): void {
    const method = this.selectedMethod;
    if (!method) {
      this.editorForm.reset({
        endpoint: '',
        soapAction: '',
        operatingMode: 'DryRun',
        enabled: false
      }, { emitEvent: false });
      return;
    }

    const value = method.group.getRawValue() as IntegrationConnectionMethodDto;
    this.editorForm.reset({
      endpoint: value.endpoint,
      soapAction: value.soapAction,
      operatingMode: value.operatingMode,
      enabled: value.enabled
    }, { emitEvent: false });
    this.editorForm.markAsPristine();
  }

  private cloneSettings(settings: IntegrationConnectionSettingsDto): IntegrationConnectionSettingsDto {
    return {
      ...settings,
      wscfaachMappings: settings.wscfaachMappings.map((item) => this.cloneMethod(item)),
      wsAxonRespuestaTransaccionesMappings: settings.wsAxonRespuestaTransaccionesMappings.map((item) => this.cloneMethod(item))
    };
  }

  private cloneMethod(method: IntegrationConnectionMethodDto): IntegrationConnectionMethodDto {
    return {
      ...method,
      capabilities: method.capabilities ? { ...method.capabilities } : method.capabilities,
      inputParameterMappings: (method.inputParameterMappings ?? []).map((item) => ({ ...item }))
    };
  }

  private getInputMappings(mappingGroup: FormGroup): FormArray<FormGroup> {
    return mappingGroup.get('inputParameterMappings') as FormArray<FormGroup>;
  }

  private setMappings(target: FormArray<FormGroup>, mappings: IntegrationConnectionMethodDto[]): void {
    target.clear();
    mappings.forEach((mapping) => target.push(this.createMappingGroup(mapping)));
  }

  private createMappingGroup(mapping: IntegrationConnectionMethodDto): FormGroup {
    const inputMappings = this.fb.array<FormGroup>(
      (mapping.inputParameterMappings ?? []).map((item) => this.createInputMappingGroup(item))
    );

    return this.fb.group({
      methodName: [mapping.methodName, [Validators.required]],
      endpoint: [mapping.endpoint, [Validators.required]],
      soapAction: [mapping.soapAction, [Validators.required]],
      operatingMode: [mapping.operatingMode ?? 'DryRun', [Validators.required]],
      enabled: [mapping.enabled],
      sanitizedEndpoint: [mapping.sanitizedEndpoint],
      sanitizedSoapAction: [mapping.sanitizedSoapAction],
      configurationState: [mapping.configurationState ?? 'Unknown'],
      configurationStateKnown: [mapping.capabilities?.configurationStateKnown === true],
      canRequestRealSend: [mapping.capabilities?.canRequestRealSend === true],
      capabilityBlockingReason: [mapping.capabilities?.blockingReason ?? null],
      inputParameterMappings: inputMappings
    });
  }

  private createInputMappingGroup(item?: IntegrationConnectionInputMappingDto): FormGroup {
    return this.fb.group({
      inputName: [item?.inputName ?? '', [Validators.required]],
      soapParameterName: [item?.soapParameterName ?? '', [Validators.required]],
      defaultValue: [item?.defaultValue ?? ''],
      required: [item?.required ?? true]
    });
  }
}
