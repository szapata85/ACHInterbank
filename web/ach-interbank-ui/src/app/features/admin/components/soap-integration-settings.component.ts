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
    { etiqueta: 'Configuracion SOAP' }
  ];

  readonly form = this.fb.group({
    wscfaachMappings: this.fb.array<FormGroup>([]),
    wsAxonRespuestaTransaccionesMappings: this.fb.array<FormGroup>([])
  });

  saving = false;
  testing = false;
  reloading = false;
  lastTestResult: { status: 'OK' | 'ERROR'; message: string; checkedAt: Date } | null = null;

  get wscfaachMappings(): FormArray<FormGroup> {
    return this.form.get('wscfaachMappings') as FormArray<FormGroup>;
  }

  get wsAxonMappings(): FormArray<FormGroup> {
    return this.form.get('wsAxonRespuestaTransaccionesMappings') as FormArray<FormGroup>;
  }

  constructor() {
    this.service.settings$.pipe(take(1)).subscribe((settings) => {
      this.setMappings(this.wscfaachMappings, settings.wscfaachMappings);
      this.setMappings(this.wsAxonMappings, settings.wsAxonRespuestaTransaccionesMappings);
      this.cdr.markForCheck();
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Completa los valores requeridos de endpoint, SOAP Action y mapeos.');
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

  testConnection(): void {
    this.testing = true;
    const allMappings = [
      ...(this.wscfaachMappings.getRawValue() as SoapEndpointMethodMapping[]),
      ...(this.wsAxonMappings.getRawValue() as SoapEndpointMethodMapping[])
    ];
    const enabled = allMappings.filter((item) => item.enabled);
    const invalid = enabled.find((item) => !item.endpoint?.trim() || !item.soapAction?.trim());
    this.lastTestResult = invalid
      ? { status: 'ERROR', message: 'Hay metodos habilitados sin endpoint o SOAP Action.', checkedAt: new Date() }
      : { status: 'OK', message: 'Validacion local correcta. No se ejecuto llamada SOAP externa desde esta pantalla.', checkedAt: new Date() };
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
