import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ColDef } from 'ag-grid-community';
import { SharedModule } from '../../../shared/shared.module';

interface AddressTypeRow {
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-address-types-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './address-types-admin.component.html',
  styleUrls: ['./catalog-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})

export class AddressTypesAdminComponent {
  readonly columnas: ColDef[] = [
    { field: 'code', headerName: 'Código', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'name', headerName: 'Nombre', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'description', headerName: 'Descripción', sortable: true, filter: 'agTextColumnFilter', flex: 1 }
  ];


  readonly rows: AddressTypeRow[] = [
    { code: 'CASA', name: 'Casa', description: 'Dirección residencial.' },
    { code: 'TRABAJO', name: 'Trabajo', description: 'Dirección laboral.' },
    { code: 'FINCA', name: 'Finca', description: 'Dirección de finca.' },
    { code: 'CORRESPONDENCIA', name: 'Correspondencia', description: 'Dirección para notificaciones.' },
    { code: 'PERSONAL', name: 'Personal', description: 'Dirección personal.' }
  ];
}
