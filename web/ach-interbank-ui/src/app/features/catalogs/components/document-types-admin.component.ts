import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';

interface DocumentTypeRow {
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-document-types-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './document-types-admin.component.html',
  styleUrls: ['./catalog-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DocumentTypesAdminComponent {
  readonly rows: DocumentTypeRow[] = [
    { code: 'CC', name: 'Cédula de Ciudadanía', description: 'Documento de identidad nacional.' },
    { code: 'CE', name: 'Cédula de Extranjería', description: 'Documento para extranjeros residentes.' },
    { code: 'NIT', name: 'Número de Identificación Tributaria', description: 'Identificación tributaria.' },
    { code: 'PAS', name: 'Pasaporte', description: 'Documento de viaje.' },
    { code: 'TI', name: 'Tarjeta de Identidad', description: 'Documento para menores de edad.' },
    { code: 'OTRO', name: 'Otro', description: 'Otro tipo de documento.' }
  ];
}
