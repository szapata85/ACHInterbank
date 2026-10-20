import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterModule } from '@angular/router';
import { IntegrationServiceReadModel } from '../data-access/integration-read.models';
import { presentIntegrationStatus } from './mapping-status.presenter';

@Component({
  selector: 'app-integration-service-card',
  standalone: true,
  imports: [RouterModule, MatButtonModule],
  templateUrl: './integration-service-card.component.html',
  styleUrls: ['./integration-service-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IntegrationServiceCardComponent {
  @Input({ required: true }) service!: IntegrationServiceReadModel;
  @Input() canManage = false;

  readonly presentStatus = presentIntegrationStatus;
}
