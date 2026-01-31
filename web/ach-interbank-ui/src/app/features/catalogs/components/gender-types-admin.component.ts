import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';

interface GenderTypeRow {
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-gender-types-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './gender-types-admin.component.html',
  styleUrls: ['./catalog-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GenderTypesAdminComponent {
  readonly rows: GenderTypeRow[] = [
    { code: 'MASCULINO', name: 'Masculino', description: 'Identidad masculina.' },
    { code: 'FEMENINO', name: 'Femenino', description: 'Identidad femenina.' },
    { code: 'NO_BINARIO', name: 'No binario', description: 'Identidad no binaria.' },
    { code: 'OTRO', name: 'Otro', description: 'Otra identidad.' },
    { code: 'NO_ESPECIFICA', name: 'No especifica', description: 'No desea especificar.' }
  ];
}
