import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';

interface CenitSectionLink {
  titulo: string;
  descripcion: string;
  ruta: string;
}

@Component({
  selector: 'app-cenit-home',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
  templateUrl: './cenit-home.component.html',
  styleUrls: ['./cenit-home.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CenitHomeComponent {
  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'CENIT' }
  ];

  readonly regulatorio: CenitSectionLink[] = [
    { titulo: 'Causales de devolución', descripcion: 'Catálogo Rxx y aplicabilidad normativa.', ruta: '/cenit/regulatorio/causales-devolucion' },
    { titulo: 'Causales de rechazo', descripcion: 'Catálogo Dxx por severidad y etapa.', ruta: '/cenit/regulatorio/causales-rechazo' },
    { titulo: 'Políticas de transacción', descripcion: 'Prioridad, naturaleza monetaria y reglas de retorno.', ruta: '/cenit/regulatorio/politicas-transaccion' },
    { titulo: 'Políticas de devolución', descripcion: 'Códigos permitidos, plazo y estado origen.', ruta: '/cenit/regulatorio/politicas-devolucion' },
    { titulo: 'Políticas de prenotificación', descripcion: 'Reglas de obligatoriedad y bloqueo.', ruta: '/cenit/regulatorio/politicas-prenotificacion' }
  ];

  readonly operacion: CenitSectionLink[] = [
    { titulo: 'Ciclos del día', descripcion: 'Visibilidad de ejecución y volumen.', ruta: '/cenit/operacion/ciclos' },
    { titulo: 'Cola operativa', descripcion: 'Transacciones encoladas y diferidas.', ruta: '/cenit/operacion/cola' },
    { titulo: 'Neteo', descripcion: 'Posiciones netas por entidad.', ruta: '/cenit/operacion/neteo' },
    { titulo: 'Optimización', descripcion: 'Decisiones de liquidez por prioridad y ciclo.', ruta: '/cenit/operacion/optimizacion' },
    { titulo: 'Devoluciones', descripcion: 'Causales y estado para monitoreo operativo.', ruta: '/cenit/operacion/devoluciones' },
    { titulo: 'Trazabilidad', descripcion: 'Ciclo, lote, archivo y causal aplicada.', ruta: '/cenit/operacion/trazabilidad' },
    { titulo: 'Respuestas de cámara', descripcion: 'ACK, NACK, rechazos del operador y salidas de sesión.', ruta: '/cenit/operacion/respuestas-camara' }
  ];
}
