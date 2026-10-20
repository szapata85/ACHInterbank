import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NachaConfigLayoutField, NachaConfigLayoutVariant } from '../../../core/models/nacha-config.model';

@Component({
  selector: 'app-nacha-config-position-ruler',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTooltipModule],
  templateUrl: './nacha-config-position-ruler.component.html',
  styleUrls: ['./nacha-config-position-ruler.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigPositionRulerComponent {
  @Input() variant: NachaConfigLayoutVariant | null = null;
  @Input() selectedFieldId: number | null = null;
  @Output() selectedFieldIdChange = new EventEmitter<number>();

  get fields(): NachaConfigLayoutField[] {
    return [...(this.variant?.fields ?? [])].sort((a, b) => a.startPosition - b.startPosition || a.id - b.id);
  }

  get totalLength(): number {
    return Math.max(1, this.variant?.totalLength ?? 1);
  }

  gridColumn(field: NachaConfigLayoutField): string {
    const start = Math.max(1, field.startPosition);
    const endExclusive = Math.min(this.totalLength + 1, field.startPosition + field.length);
    return `${start} / ${Math.max(start + 1, endExclusive)}`;
  }

  endPosition(field: NachaConfigLayoutField): number {
    return field.startPosition + field.length - 1;
  }

  hasOverlap(field: NachaConfigLayoutField): boolean {
    const start = field.startPosition;
    const end = this.endPosition(field);
    return this.fields.some((other) =>
      other.id !== field.id
      && other.startPosition <= end
      && this.endPosition(other) >= start
    );
  }

  ariaLabel(field: NachaConfigLayoutField): string {
    const state = field.isEnabled ? 'activo' : 'inactivo';
    const overlap = this.hasOverlap(field) ? ', con superposición' : '';
    return `${field.fieldNameEs}, posiciones ${field.startPosition} a ${this.endPosition(field)}, ${state}${overlap}`;
  }
}
