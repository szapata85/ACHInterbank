import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
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
    title: 'Credito entrante hacia CFA',
    description: 'Configura el endpoint tecnico usado para preparar creditos monetarios originados por otra entidad.',
    nature: 'Monetario controlado'
  },
  Proc_Contrapartidas: {
    title: 'Debito originado por CFA',
    description: 'Configura el endpoint tecnico usado para preparar debitos monetarios de contrapartida.',
    nature: 'Monetario controlado'
  },
  RegistrarRespuestaTransaccion: {
    title: 'Respuesta diferencial',
    description: 'Configura el endpoint tecnico para registrar respuestas, rechazos o notificaciones no monetarias.',
    nature: 'No monetario'
  }
};

@Component({
  selector: 'app-soap-integration-settings',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './soap-integration-settings.component.html',
  styleUrls: ['./soap-integration-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SoapIntegrationSettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(SoapIntegrationSettingsService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones', ruta: '/integraciones' },
    { etiqueta: 'Configuracion de servicios SOAP' }
  ];

  readonly form = this.fb.group({
    wscfaachMappings: this.fb.array<FormGroup>([]),
    wsAxonRespuestaTransaccionesMappings: this.fb.array<FormGroup>([])
  });

  loading = true;
  saving = false;
  reloading = false;
  modalMode: ModalMode = null;
  selectedClientKey: SoapClientKey | null = null;
  selectedMethodName: string | null = null;

  constructor() {
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
      description: 'Configura endpoint, SOAP Action y estado tecnico.',
      nature: 'Configuracion tecnica'
    };
  }

  get enabledCount(): number {
    return this.allMethods.filter((item) => item.group.get('enabled')?.value).length;
  }

  selectMethod(method: SoapMethodView): void {
    this.selectedClientKey = method.clientKey;
    this.selectedMethodName = this.methodNameFor(method.group);
    this.cdr.markForCheck();
  }

  isSelected(method: SoapMethodView): boolean {
    return this.selectedClientKey === method.clientKey && this.selectedMethodName === this.methodNameFor(method.group);
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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Completa endpoint y SOAP Action requeridos.');
      return;
    }

    const currentSelection = this.captureSelection();
    this.saving = true;
    this.service.updateSettings(this.buildPayload())
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (settings) => {
          this.hydrate(settings, currentSelection);
          this.form.markAsPristine();
          this.notifications.success('Configuracion SOAP guardada.');
        },
        error: () => this.notifications.error('No fue posible guardar la configuracion SOAP.')
      });
  }

  reload(): void {
    this.loadSettings(true);
  }

  endpointFor(group: FormGroup): string {
    return group.get('endpoint')?.value || 'Sin endpoint';
  }

  methodNameFor(group: FormGroup): string {
    return group.get('methodName')?.value || 'Metodo SOAP';
  }

  statusFor(group: FormGroup): string {
    return group.get('enabled')?.value ? 'Activo' : 'Inactivo';
  }

  methodCodeFor(method: SoapMethodView): string {
    const integrationKey = method.clientKey === 'wscfaachMappings' ? 'WSCFAACH' : 'WSAXON';
    return `${integrationKey}.${this.methodNameFor(method.group)}`;
  }

  mappingCountFor(group: FormGroup): number {
    return this.getInputMappings(group).length;
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
            this.notifications.success('Configuracion SOAP recargada.');
          }
        },
        error: () => this.notifications.error('No fue posible cargar la configuracion SOAP.')
      });
  }

  private hydrate(settings: SoapIntegrationSettings, selection = this.captureSelection()): void {
    this.setMappings(this.wscfaachMappings, settings.wscfaachMappings);
    this.setMappings(this.wsAxonMappings, settings.wsAxonRespuestaTransaccionesMappings);
    this.restoreSelection(selection);
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
    return {
      wscfaachMappings: this.wscfaachMappings.getRawValue() as SoapEndpointMethodMapping[],
      wsAxonRespuestaTransaccionesMappings: this.wsAxonMappings.getRawValue() as SoapEndpointMethodMapping[]
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
