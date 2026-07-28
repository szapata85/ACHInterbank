import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-integration-workspace',
  standalone: true,
  imports: [RouterModule, SharedModule, MatButtonModule, MatIconModule],
  templateUrl: './integration-workspace.component.html',
  styleUrls: ['./integration-workspace.component.scss']
})
export class IntegrationWorkspaceComponent {
  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones' }
  ];
}
