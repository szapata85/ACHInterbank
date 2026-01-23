import { ChangeDetectionStrategy, Component } from '@angular/core';
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
  readonly rows: PersonTypeRow[] = [
    { code: 'PN', name: 'Persona natural', description: 'Cliente persona natural.' },
    { code: 'PJ', name: 'Persona jurídica', description: 'Cliente empresa o persona jurídica.' }
  ];
}
