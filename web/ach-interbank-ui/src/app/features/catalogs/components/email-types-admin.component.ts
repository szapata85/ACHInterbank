import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ColDef } from 'ag-grid-community';
import { SharedModule } from '../../../shared/shared.module';

interface EmailTypeRow {
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-email-types-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './email-types-admin.component.html',
  styleUrls: ['./catalog-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})

export class EmailTypesAdminComponent {
  readonly columnas: ColDef[] = [
    { field: 'code', headerName: 'Código', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'name', headerName: 'Nombre', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'description', headerName: 'Descripción', sortable: true, filter: 'agTextColumnFilter', flex: 1 }
  ];


  readonly rows: EmailTypeRow[] = [
    { code: 'PERSONAL', name: 'Personal', description: 'Correo personal.' },
    { code: 'TRABAJO', name: 'Trabajo', description: 'Correo laboral.' },
    { code: 'OTRO', name: 'Otro', description: 'Otro correo.' }
  ];
}
