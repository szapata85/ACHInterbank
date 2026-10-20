import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaOperationSummaryComponent } from './nacha-operation-summary.component';

@Component({
  selector: 'app-nacha-generate-encrypted-operation',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    PageHeaderComponent,
    NachaOperationSummaryComponent
  ],
  templateUrl: './nacha-generate-encrypted-operation.component.html',
  styleUrls: ['./nacha-operation-page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaGenerateEncryptedOperationComponent {
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

    this.service.generateEncrypted({ cycleId: this.form.value.cycleId! })
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          this.operation = response;
          this.cdr.markForCheck();
        },
        error: () => this.notifications.error('No fue posible generar NACHA-M cifrado.')
      });
  }
}
