import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';
import { IntegrationServiceReadModel } from '../data-access/integration-read.models';
import { IntegrationErrorState, IntegrationLoadStatus, IntegrationPermissionsState } from '../state/integration-state.models';
import { INTEGRATION_COPY } from './integration-copy';
import { IntegrationServiceCardComponent } from './integration-service-card.component';

@Component({
  selector: 'app-integration-center-shell',
  standalone: true,
  imports: [SharedModule, IntegrationServiceCardComponent],
  templateUrl: './integration-center-shell.component.html',
  styleUrls: ['./integration-center-shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IntegrationCenterShellComponent {
  @Input({ required: true }) status: IntegrationLoadStatus = 'idle';
  @Input() services: IntegrationServiceReadModel[] = [];
  @Input() error: IntegrationErrorState | null = null;
  @Input() permissions: IntegrationPermissionsState = { canRead: false, canManage: false };
  @Output() retry = new EventEmitter<void>();

  readonly copy = INTEGRATION_COPY;
}
