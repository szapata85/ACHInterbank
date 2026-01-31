import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';

interface PhoneTypeRow {
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-phone-types-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './phone-types-admin.component.html',
  styleUrls: ['./catalog-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PhoneTypesAdminComponent {
  readonly rows: PhoneTypeRow[] = [
    { code: 'FIJO', name: 'Fijo', description: 'Línea fija.' },
    { code: 'TRABAJO', name: 'Trabajo', description: 'Teléfono laboral.' },
    { code: 'MOVIL', name: 'Móvil', description: 'Teléfono móvil.' }
  ];
}
