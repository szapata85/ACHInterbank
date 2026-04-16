import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ColDef } from 'ag-grid-community';
import { SharedModule } from '../../../shared/shared.module';

interface PersonTypeRow {
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-person-types-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './person-types-admin.component.html',
  styleUrls: ['./catalog-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})

export class PersonTypesAdminComponent {
  readonly columnas: ColDef[] = [
    { field: 'code', headerName: 'Código', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'name', headerName: 'Nombre', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'description', headerName: 'Descripción', sortable: true, filter: 'agTextColumnFilter', flex: 1 }
  ];


  readonly rows: PersonTypeRow[] = [
    { code: 'PN', name: 'Persona natural', description: 'Cliente persona natural.' },
    { code: 'PJ', name: 'Persona jurídica', description: 'Cliente empresa o persona jurídica.' }
  ];
}
