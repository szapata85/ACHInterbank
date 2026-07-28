import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  SoapEndpointMethodMapping,
  SoapInputParameterMapping,
  SoapIntegrationSettings,
  SoapIntegrationSettingsService
} from '../../../core/services/soap-integration-settings.service';
import { SharedModule } from '../../../shared/shared.module';

type SoapClientKey = 'wscfaachMappings' | 'wsAxonRespuestaTransaccionesMappings';
type ModalMode = 'help' | null;

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

const SERVICE_COPY: Record<string, SoapServiceCopy> = {
  Proc_Transacciones: {
    title: 'Crédito entrante hacia CFA',
    description: 'Configura el endpoint técnico usado para preparar créditos monetarios originados por otra entidad.',
    nature: 'Monetario controlado'
  },
  Proc_Contrapartidas: {
    title: 'Débito originado por CFA',
    description: 'Configura el endpoint técnico usado para preparar débitos monetarios de contrapartida.',
    nature: 'Monetario controlado'
  },
  RegistrarRespuestaTransaccion: {
    title: 'Respuesta diferencial',
    description: 'Configura el endpoint técnico para registrar respuestas, rechazos o notificaciones no monetarias.',
    nature: 'No monetario'
  }
};

@Component({
  selector: 'app-soap-integration-settings',
  standalone: true,
  imports: [
    SharedModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTabsModule,
    MatTooltipModule
  ],
  templateUrl: './soap-integration-settings.component.html',
  styleUrls: ['./soap-integration-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SoapIntegrationSettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(SoapIntegrationSettingsService);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones', ruta: '/integraciones' },
    { etiqueta: 'Configuración de servicios SOAP' }
  ];

  readonly form = this.fb.group({
    wscfaachMappings: this.fb.array<FormGroup>([]),
    wsAxonRespuestaTransaccionesMappings: this.fb.array<FormGroup>([])
  });
  readonly editorForm = this.fb.group({
    endpoint: ['', [Validators.required, Validators.pattern(/^https?:\/\/\S+$/i)]],
    soapAction: ['', [Validators.required, Validators.pattern(/^https?:\/\/\S+$/i)]],
    operatingMode: ['DryRun' as SoapEndpointMethodMapping['operatingMode'], [Validators.required]],
    enabled: [true]
  });
  readonly canManage = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);

  loading = true;
  saving = false;
  reloading = false;
  modalMode: ModalMode = null;
  selectedClientKey: SoapClientKey | null = null;
  selectedMethodName: string | null = null;
  editingMethodCode: string | null = null;
  private persistedSettings: SoapIntegrationSettings | null = null;

  constructor() {
    this.editorForm.disable({ emitEvent: false });
    this.loadSettings(false);
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
      title: 'Servicio SOAP',
      description: 'Configura endpoint, SOAP Action y estado técnico.',
      nature: 'Configuración técnica'
    };
  }

  get enabledCount(): number {
    return this.allMethods.filter((item) => item.group.get('enabled')?.value).length;
  }

  selectMethod(method: SoapMethodView): void {
    this.cancelEdit(false);
    this.selectedClientKey = method.clientKey;
    this.selectedMethodName = this.methodNameFor(method.group);
    this.syncEditor();
    this.cdr.markForCheck();
  }

  isSelected(method: SoapMethodView): boolean {
    return this.selectedClientKey === method.clientKey && this.selectedMethodName === this.methodNameFor(method.group);
  }

  get isEditingSelected(): boolean {
    const method = this.selectedMethod;
    return method !== null && this.editingMethodCode === this.methodCodeFor(method);
  }

  beginEdit(): void {
    const method = this.selectedMethod;
    if (!method || !this.canManage) {
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

  openHelp(): void {
    this.modalMode = 'help';
    this.cdr.markForCheck();
  }

  closeModal(): void {
    this.modalMode = null;
    this.cdr.markForCheck();
  }

  save(): void {
    if (!this.canManage || !this.isEditingSelected || this.saving) {
      return;
    }
    if (this.editorForm.invalid) {
      this.editorForm.markAllAsTouched();
      this.notifications.error('Corrige los campos requeridos antes de guardar.');
      return;
    }
    if (!this.editorForm.dirty || !this.persistedSettings) {
      return;
    }

    const currentSelection = this.captureSelection();
    const payload = this.buildPayload();
    this.saving = true;
    this.service.updateSettings(payload)
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (settings) => {
          this.hydrate(settings, currentSelection);
          this.form.markAsPristine();
          this.editingMethodCode = null;
          this.editorForm.markAsPristine();
          this.notifications.success('Configuración SOAP guardada.');
        },
        error: () => this.notifications.error('No fue posible guardar la configuración SOAP. Conservamos los cambios para que puedas volver a intentar.')
      });
  }

  reload(): void {
    this.loadSettings(true);
  }

  endpointFor(group: FormGroup): string {
    return group.get('endpoint')?.value || 'Sin endpoint';
  }

  methodNameFor(group: FormGroup): string {
    return group.get('methodName')?.value || 'Método SOAP';
  }

  statusFor(group: FormGroup): string {
    return group.get('enabled')?.value ? 'Activo' : 'Inactivo';
  }

  operatingModeFor(group: FormGroup): string {
    const mode = group.get('operatingMode')?.value;
    if (mode === 'Live') {
      return 'LIVE';
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
      && !this.saving;
  }

  get selectedIndex(): number {
    const selected = this.selectedMethod;
    return selected ? this.allMethods.findIndex((method) => this.methodCodeFor(method) === this.methodCodeFor(selected)) : 0;
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
    this.service.refreshFromServer()
      .pipe(finalize(() => {
        this.loading = false;
        this.reloading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (settings) => {
          this.hydrate(settings, currentSelection);
          this.form.markAsPristine();
          if (showSuccess) {
            this.notifications.success('Configuración SOAP actualizada.');
          }
        },
        error: () => this.notifications.error('No fue posible cargar la configuración SOAP.')
      });
  }

  private hydrate(settings: SoapIntegrationSettings, selection = this.captureSelection()): void {
    this.persistedSettings = this.cloneSettings(settings);
    this.setMappings(this.wscfaachMappings, this.persistedSettings.wscfaachMappings);
    this.setMappings(this.wsAxonMappings, this.persistedSettings.wsAxonRespuestaTransaccionesMappings);
    this.restoreSelection(selection);
    this.syncEditor();
    this.editorForm.disable({ emitEvent: false });
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
    const preferred = existing
      ?? this.allMethods.find((method) => this.methodNameFor(method.group) === 'Proc_Transacciones')
      ?? this.allMethods[0]
      ?? null;

    this.selectedClientKey = preferred?.clientKey ?? null;
    this.selectedMethodName = preferred ? this.methodNameFor(preferred.group) : null;
  }

  private buildPayload(): SoapIntegrationSettings {
    const payload = this.cloneSettings(this.persistedSettings!);
    const selected = this.selectedMethod!;
    const collection = payload[selected.clientKey];
    const methodName = this.methodNameFor(selected.group);
    const index = collection.findIndex((item) => item.methodName === methodName);
    if (index < 0) {
      return payload;
    }

    collection[index] = {
      ...collection[index],
      ...this.editorForm.getRawValue(),
      methodName,
      endpoint: this.editorForm.controls.endpoint.value!.trim(),
      soapAction: this.editorForm.controls.soapAction.value!.trim(),
      operatingMode: this.editorForm.controls.operatingMode.value!,
      enabled: this.editorForm.controls.enabled.value ?? false,
      inputParameterMappings: collection[index].inputParameterMappings.map((item) => ({ ...item }))
    };
    return payload;
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

    const value = method.group.getRawValue() as SoapEndpointMethodMapping;
    this.editorForm.reset({
      endpoint: value.endpoint,
      soapAction: value.soapAction,
      operatingMode: value.operatingMode,
      enabled: value.enabled
    }, { emitEvent: false });
    this.editorForm.markAsPristine();
  }

  private cloneSettings(settings: SoapIntegrationSettings): SoapIntegrationSettings {
    return {
      wscfaachMappings: settings.wscfaachMappings.map((item) => this.cloneMethod(item)),
      wsAxonRespuestaTransaccionesMappings: settings.wsAxonRespuestaTransaccionesMappings.map((item) => this.cloneMethod(item))
    };
  }

  private cloneMethod(method: SoapEndpointMethodMapping): SoapEndpointMethodMapping {
    return {
      ...method,
      inputParameterMappings: (method.inputParameterMappings ?? []).map((item) => ({ ...item }))
    };
  }

  private getInputMappings(mappingGroup: FormGroup): FormArray<FormGroup> {
    return mappingGroup.get('inputParameterMappings') as FormArray<FormGroup>;
  }

  private setMappings(target: FormArray<FormGroup>, mappings: SoapEndpointMethodMapping[]): void {
    target.clear();
    mappings.forEach((mapping) => target.push(this.createMappingGroup(mapping)));
  }

  private createMappingGroup(mapping: SoapEndpointMethodMapping): FormGroup {
    const inputMappings = this.fb.array<FormGroup>(
      (mapping.inputParameterMappings ?? []).map((item) => this.createInputMappingGroup(item))
    );

    return this.fb.group({
      methodName: [mapping.methodName, [Validators.required]],
      endpoint: [mapping.endpoint, [Validators.required]],
      soapAction: [mapping.soapAction, [Validators.required]],
      operatingMode: [mapping.operatingMode ?? 'DryRun', [Validators.required]],
      enabled: [mapping.enabled],
      inputParameterMappings: inputMappings
    });
  }

  private createInputMappingGroup(item?: SoapInputParameterMapping): FormGroup {
    return this.fb.group({
      inputName: [item?.inputName ?? '', [Validators.required]],
      soapParameterName: [item?.soapParameterName ?? '', [Validators.required]],
      defaultValue: [item?.defaultValue ?? ''],
      required: [item?.required ?? true]
    });
  }
}
