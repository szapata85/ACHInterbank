import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-integration-workspace',
  standalone: true,
  imports: [RouterModule, SharedModule],
  templateUrl: './integration-workspace.component.html',
  styleUrls: ['./integration-workspace.component.scss']
})
export class IntegrationWorkspaceComponent {
  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones' }
  ];
}
