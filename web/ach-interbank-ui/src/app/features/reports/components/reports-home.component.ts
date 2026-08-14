import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { AccountingReviewExportComponent } from './accounting-review-export.component';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

interface ReportLink {
  title: string;
  description: string;
  route: string;
  icon: string;
}

interface ReportGroup {
  id: string;
  title: string;
  description: string;
  reports: ReportLink[];
}

@Component({
  selector: 'app-reports-home',
  standalone: true,
  imports: [SharedModule, RouterModule, AccountingReviewExportComponent, MatButtonModule, MatCardModule, MatIconModule],
  templateUrl: './reports-home.component.html',
  styleUrls: ['./reports-home.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsHomeComponent {
  readonly groups: ReportGroup[] = [
    {
      id: 'operacion-transaccional',
      title: 'Operación transaccional',
      description: 'Consulta el movimiento y los resultados de las operaciones ACH.',
      reports: [
        { title: 'Transacciones enviadas', description: 'Operaciones originadas y enviadas a otras entidades.', route: '/reports/sent', icon: 'north_east' },
        { title: 'Transacciones recibidas', description: 'Operaciones recibidas desde otras entidades.', route: '/reports/received', icon: 'south_west' },
        { title: 'Devoluciones', description: 'Operaciones devueltas y sus causales.', route: '/reports/returns', icon: 'assignment_return' },
        { title: 'Rechazos', description: 'Operaciones rechazadas durante el procesamiento.', route: '/reports/rejections', icon: 'block' }
      ]
    },
    {
      id: 'procesamiento',
      title: 'Procesamiento',
      description: 'Revisa archivos, ciclos y diferencias operativas.',
      reports: [
        { title: 'Archivos NACHA-M', description: 'Archivos generados y sus totales de procesamiento.', route: '/reports/files', icon: 'description' },
        { title: 'Ciclos', description: 'Estado, horario y totales de los ciclos ACH.', route: '/reports/cycles', icon: 'schedule' },
        { title: 'Conciliación', description: 'Comparativo de enviados, recibidos y devueltos.', route: '/reports/reconciliation', icon: 'balance' }
      ]
    },
    {
      id: 'seguimiento-control',
      title: 'Seguimiento y control',
      description: 'Encuentra cambios, acciones y trazabilidad de la operación.',
      reports: [
        { title: 'Trazabilidad ACH', description: 'Consolidado descargable por estado, ciclo y fecha.', route: '/reports/traceability', icon: 'route' },
        { title: 'Histórico', description: 'Cambios de estado registrados por transacción.', route: '/reports/history', icon: 'history' },
        { title: 'Auditoría', description: 'Acciones realizadas por usuario y entidad.', route: '/reports/audit', icon: 'fact_check' }
      ]
    }
  ];
}
