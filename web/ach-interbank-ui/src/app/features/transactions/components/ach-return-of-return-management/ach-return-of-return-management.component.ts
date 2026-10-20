import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { SharedModule } from '../../../../shared/shared.module';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { AchReturnOfReturnEligibilityResult } from '../../transactions.models';

const RETURN_OF_RETURN_GENERATE_PERMISSIONS = ['Returns.GenerateFile', 'CanManageAch'];

@Component({
  selector: 'app-ach-return-of-return-management',
  standalone: true,
  imports: [SharedModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './ach-return-of-return-management.component.html',
  styleUrls: ['./ach-return-of-return-management.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchReturnOfReturnManagementComponent {
  private readonly fb = inject(FormBuilder);
  private readonly returnsApi = inject(AchReturnsApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loadingEvaluate = false;
  loadingAudit = false;
  loadingNacha = false;
  eligibilityResult: AchReturnOfReturnEligibilityResult | null = null;
  generationFailures: Array<{ code?: string; message?: string; field?: string | null }> = [];

  readonly evaluateForm = this.fb.group({
    sourceReturnTransactionId: [null as number | null, [Validators.required, Validators.min(1)]],
    newReturnReasonCode: ['', Validators.required]
  });

  readonly generationForm = this.fb.group({
    flowIds: ['', Validators.required]
  });

  evaluate(): void {
    if (this.loadingEvaluate) {
      return;
    }
    if (this.evaluateForm.invalid) {
      this.notifications.warning('Complete los datos requeridos para evaluar elegibilidad.');
      return;
    }

    this.loadingEvaluate = true;
    const value = this.evaluateForm.getRawValue();
    this.returnsApi.evaluateReturnOfReturn({
      sourceReturnTransactionId: Number(value.sourceReturnTransactionId),
      newReturnReasonCode: String(value.newReturnReasonCode ?? '').trim(),
      source: 'spa-angular-ror'
    }).subscribe({
      next: (result) => {
        this.eligibilityResult = result;
        this.generationFailures = [];
        this.loadingEvaluate = false;
        if (result.isEligible) {
          this.notifications.success('La devolución de devolución es elegible.');
        } else {
          this.notifications.warning('La devolución de devolución no es elegible.');
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loadingEvaluate = false;
        this.notifications.error(err?.error?.message ?? 'No fue posible evaluar elegibilidad de devolución de devolución.');
        this.cdr.markForCheck();
      }
    });
  }

  generateAudit(): void {
    if (!this.ensureCanGenerateFiles()) {
      return;
    }

    const flowIds = this.parseFlowIds();
    if (flowIds.length === 0 || this.loadingAudit || this.loadingNacha) {
      this.notifications.warning('Ingrese al menos un ID de flujo válido.');
      return;
    }

    this.loadingAudit = true;
    this.generationFailures = [];
    this.returnsApi.generateReturnOfReturnAuditFile({ flowIds, source: 'spa-angular-ror' }).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `ROR_AUDIT_${Date.now()}.ach`);
        this.loadingAudit = false;
        this.notifications.success('Archivo de auditoría ROR generado correctamente.');
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loadingAudit = false;
        this.notifyFunctionalError(err, 'No fue posible generar el archivo de auditoría ROR.');
        this.cdr.markForCheck();
      }
    });
  }

  generateNacha(): void {
    if (!this.ensureCanGenerateFiles()) {
      return;
    }

    const flowIds = this.parseFlowIds();
    if (flowIds.length === 0 || this.loadingNacha || this.loadingAudit) {
      this.notifications.warning('Ingrese al menos un ID de flujo válido.');
      return;
    }
    if (!this.eligibilityResult) {
      this.notifications.warning('Debe evaluar elegibilidad antes de generar NACHA-M productivo.');
      return;
    }
    if (!this.eligibilityResult.isEligible) {
      this.notifications.warning('La evaluación actual no es elegible. No se puede generar NACHA-M productivo.');
      return;
    }
    const confirmed = window.confirm(
      'Se solicitará la generación de un archivo NACHA-M productivo de devolución de una devolución. ' +
      'Confirma que los identificadores de flujo corresponden a la devolución evaluada; el sistema validará nuevamente la elegibilidad antes de generar el archivo. ¿Deseas continuar?'
    );
    if (!confirmed) {
      return;
    }

    this.loadingNacha = true;
    this.generationFailures = [];
    this.returnsApi.generateReturnOfReturnNachaFile({ flowIds, source: 'spa-angular-ror' }).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `RORNACHA_${Date.now()}.ach`);
        this.loadingNacha = false;
        this.notifications.success('Archivo NACHA-M productivo de devolución de devolución generado correctamente.');
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loadingNacha = false;
        this.notifyFunctionalError(err, 'No fue posible generar el archivo NACHA-M productivo.');
        this.cdr.markForCheck();
      }
    });
  }

  clearEligibility(): void {
    this.eligibilityResult = null;
    this.generationFailures = [];
    this.cdr.markForCheck();
  }

  get canGenerateFiles(): boolean {
    return this.auth.hasPermission(RETURN_OF_RETURN_GENERATE_PERMISSIONS);
  }

  get canGenerateNacha(): boolean {
    return this.canGenerateFiles
      && !this.loadingNacha
      && !this.loadingAudit
      && this.parseFlowIds().length > 0
      && !!this.eligibilityResult
      && this.eligibilityResult.isEligible;
  }

  private ensureCanGenerateFiles(): boolean {
    if (this.canGenerateFiles) {
      return true;
    }

    this.notifications.error('No tiene permisos para generar archivos de devolución de devolución.');
    return false;
  }

  private parseFlowIds(): number[] {
    const raw = String(this.generationForm.controls.flowIds.value ?? '').trim();
    if (!raw) {
      return [];
    }
    return raw
      .split(',')
      .map((x) => Number(x.trim()))
      .filter((x) => Number.isInteger(x) && x > 0);
  }

  private notifyFunctionalError(err: any, fallback: string): void {
    const failures = err?.error?.failures;
    if (Array.isArray(failures) && failures.length > 0) {
      this.generationFailures = failures.map((f: any) => ({ code: f?.code, message: f?.message, field: f?.field ?? null }));
      const first = failures[0];
      this.notifications.error(first?.message ?? fallback);
      return;
    }
    this.generationFailures = [];
    this.notifications.error(err?.error?.message ?? fallback);
  }

  private downloadBlob(blob: Blob, fallbackFileName: string): void {
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = fallbackFileName;
    link.click();
    URL.revokeObjectURL(link.href);
  }
}
