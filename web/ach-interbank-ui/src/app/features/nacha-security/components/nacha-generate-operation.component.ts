import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';

@Component({
  selector: 'app-nacha-generate-operation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './nacha-generate-operation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaGenerateOperationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(NachaSecurityOperationsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  operation?: NachaSecurityOperationResponse;

  readonly form = this.fb.group({
    cycleId: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.operation = undefined;
    this.cdr.markForCheck();

    this.service.generatePlain({ cycleId: this.form.value.cycleId! })
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => this.operation = response,
        error: () => this.notifications.error('No fue posible iniciar la generación NACHA-M.')
      });
  }
}
