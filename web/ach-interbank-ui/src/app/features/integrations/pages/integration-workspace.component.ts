import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { IntegrationCenterShellComponent } from '../presentation/integration-center-shell.component';
import { ContractWorkspaceStore } from '../state/contract-workspace.store';
import { IntegrationCenterStore } from '../state/integration-center.store';
import { MappingWorkspaceStore } from '../state/mapping-workspace.store';

@Component({
  selector: 'app-integration-workspace',
  standalone: true,
  imports: [IntegrationCenterShellComponent],
  templateUrl: './integration-workspace.component.html',
  styleUrls: ['./integration-workspace.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [IntegrationCenterStore, ContractWorkspaceStore, MappingWorkspaceStore]
})
export class IntegrationWorkspaceComponent {
  readonly center = inject(IntegrationCenterStore);

  constructor() {
    this.center.load();
  }
}
