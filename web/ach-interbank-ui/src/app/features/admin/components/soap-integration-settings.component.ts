import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { take } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import {
  SoapEndpointMethodMapping,
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

  readonly form = this.fb.group({
    wscfaachMappings: this.fb.array<FormGroup>([]),
    wsAxonRespuestaTransaccionesMappings: this.fb.array<FormGroup>([])
  });

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
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Completa los valores requeridos de endpoint y SOAP Action.');
      return;
    }

    this.service
      .updateSettings({
        wscfaachMappings: this.wscfaachMappings.getRawValue() as SoapEndpointMethodMapping[],
        wsAxonRespuestaTransaccionesMappings: this.wsAxonMappings.getRawValue() as SoapEndpointMethodMapping[]
      })
      .subscribe(() => {
        this.form.markAsPristine();
        this.notifications.success('Configuración SOAP guardada.');
      });
  }

  private setMappings(target: FormArray<FormGroup>, mappings: SoapEndpointMethodMapping[]): void {
    target.clear();
    mappings.forEach((mapping) => target.push(this.createMappingGroup(mapping)));
  }

  private createMappingGroup(mapping: SoapEndpointMethodMapping): FormGroup {
    return this.fb.group({
      methodName: [mapping.methodName, [Validators.required]],
      endpoint: [mapping.endpoint, [Validators.required]],
      soapAction: [mapping.soapAction, [Validators.required]],
      enabled: [mapping.enabled]
    });
  }
}
