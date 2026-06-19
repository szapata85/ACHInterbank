import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize, take } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import {
  SoapEndpointMethodMapping,
  SoapInputParameterMapping,
  SoapIntegrationSettingsService
} from '../../../core/services/soap-integration-settings.service';
import { SharedModule } from '../../../shared/shared.module';

type SoapClientKey = 'wscfaachMappings' | 'wsAxonRespuestaTransaccionesMappings';
type ModalMode = 'detail' | 'edit' | 'test' | 'help' | null;

interface SoapMethodView {
  clientKey: SoapClientKey;
  clientName: string;
  index: number;
  group: FormGroup;
}

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

  saving = false;
  testing = false;
  reloading = false;
  modalMode: ModalMode = null;
  selectedMethod: SoapMethodView | null = null;
  lastTestResult: { status: 'OK' | 'ERROR'; message: string; checkedAt: Date; methodName?: string } | null = null;

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

  get enabledCount(): number {
    return this.allMethods.filter((item) => item.group.get('enabled')?.value).length;
  }

  get safeModeCount(): number {
    return this.allMethods.length;
  }

  constructor() {
    this.service.settings$.pipe(take(1)).subscribe((settings) => {
      this.setMappings(this.wscfaachMappings, settings.wscfaachMappings);
      this.setMappings(this.wsAxonMappings, settings.wsAxonRespuestaTransaccionesMappings);
      this.cdr.markForCheck();
    });
  }

  openDetail(method: SoapMethodView): void {
    this.selectedMethod = method;
    this.modalMode = 'detail';
    this.cdr.markForCheck();
  }

  openEdit(method: SoapMethodView): void {
    this.selectedMethod = method;
    this.modalMode = 'edit';
    this.cdr.markForCheck();
  }

  openTest(method: SoapMethodView): void {
    this.selectedMethod = method;
    this.modalMode = 'test';
    this.cdr.markForCheck();
  }

  openHelp(): void {
    this.selectedMethod = null;
    this.modalMode = 'help';
    this.cdr.markForCheck();
  }

  closeModal(): void {
    this.modalMode = null;
    this.selectedMethod = null;
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Completa endpoint, SOAP Action y mapeos requeridos.');
      return;
    }

    this.saving = true;
    this.service.updateSettings({
      wscfaachMappings: this.wscfaachMappings.getRawValue() as SoapEndpointMethodMapping[],
      wsAxonRespuestaTransaccionesMappings: this.wsAxonMappings.getRawValue() as SoapEndpointMethodMapping[]
    })
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe(() => {
        this.form.markAsPristine();
        this.notifications.success('Configuracion SOAP guardada.');
        this.closeModal();
        this.cdr.markForCheck();
      });
  }

  reload(): void {
    this.reloading = true;
    this.service.refreshFromServer()
      .pipe(finalize(() => {
        this.reloading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (settings) => {
          this.setMappings(this.wscfaachMappings, settings.wscfaachMappings);
          this.setMappings(this.wsAxonMappings, settings.wsAxonRespuestaTransaccionesMappings);
          this.form.markAsPristine();
          this.notifications.success('Configuracion SOAP recargada.');
        },
        error: () => this.notifications.error('No fue posible recargar la configuracion SOAP.')
      });
  }

  runConnectionTest(): void {
    if (!this.selectedMethod) {
      return;
    }

    this.testing = true;
    const method = this.selectedMethod.group.getRawValue() as SoapEndpointMethodMapping;
    const missingRequired = method.enabled && (!method.endpoint?.trim() || !method.soapAction?.trim());
    this.lastTestResult = missingRequired
      ? {
          status: 'ERROR',
          methodName: method.methodName,
          message: 'El metodo habilitado no tiene endpoint o SOAP Action configurado.',
          checkedAt: new Date()
        }
      : {
          status: 'OK',
          methodName: method.methodName,
          message: 'Validacion local correcta. No se ejecuto llamada SOAP externa desde esta pantalla.',
          checkedAt: new Date()
        };
    this.testing = false;
    this.cdr.markForCheck();
  }

  getInputMappings(mappingGroup: FormGroup): FormArray<FormGroup> {
    return mappingGroup.get('inputParameterMappings') as FormArray<FormGroup>;
  }

  addInputMapping(mappingGroup: FormGroup): void {
    this.getInputMappings(mappingGroup).push(this.createInputMappingGroup());
  }

  removeInputMapping(mappingGroup: FormGroup, index: number): void {
    this.getInputMappings(mappingGroup).removeAt(index);
  }

  endpointFor(group: FormGroup): string {
    return group.get('endpoint')?.value || 'Sin endpoint';
  }

  methodNameFor(group: FormGroup): string {
    return group.get('methodName')?.value || 'Metodo SOAP';
  }

  statusFor(group: FormGroup): string {
    return group.get('enabled')?.value ? 'Habilitado' : 'Deshabilitado';
  }

  modeFor(group: FormGroup): string {
    const method = this.methodNameFor(group);
    if (method === 'Proc_Contrapartidas') {
      return 'DryRun/UAT-local';
    }
    return 'Configurado';
  }

  methodCodeFor(method: SoapMethodView): string {
    const integrationKey = method.clientKey === 'wscfaachMappings' ? 'WSCFAACH' : 'WSAXON';
    return `${integrationKey}.${this.methodNameFor(method.group)}`;
  }

  mappingCountFor(group: FormGroup): number {
    return this.getInputMappings(group).length;
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
