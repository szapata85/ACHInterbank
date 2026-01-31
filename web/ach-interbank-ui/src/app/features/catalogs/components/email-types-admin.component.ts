import { ChangeDetectionStrategy, Component } from '@angular/core';
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
  readonly rows: EmailTypeRow[] = [
    { code: 'PERSONAL', name: 'Personal', description: 'Correo personal.' },
    { code: 'TRABAJO', name: 'Trabajo', description: 'Correo laboral.' },
    { code: 'OTRO', name: 'Otro', description: 'Otro correo.' }
  ];
}
