import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { NachaConfigLayoutField, NachaConfigLayoutVariant } from '../../../core/models/nacha-config.model';

export interface NachaConfigFieldListItem {
  field: NachaConfigLayoutField;
  variant: NachaConfigLayoutVariant;
}

@Component({
  selector: 'app-nacha-config-field-list',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule],
  templateUrl: './nacha-config-field-list.component.html',
  styleUrls: ['./nacha-config-field-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigFieldListComponent {
  @Input() items: NachaConfigFieldListItem[] = [];
  @Input() selectedFieldId: number | null = null;
  @Output() selectedFieldIdChange = new EventEmitter<number>();

  readonly columns = ['field', 'variant', 'position', 'length'];

  select(item: NachaConfigFieldListItem): void {
    this.selectedFieldIdChange.emit(item.field.id);
  }

  endPosition(field: NachaConfigLayoutField): number {
    return field.startPosition + field.length - 1;
  }

  rowAria(item: NachaConfigFieldListItem): string {
    return `${item.field.fieldNameEs}, ${item.variant.nombreEs}, posiciones ${item.field.startPosition} a ${this.endPosition(item.field)}, longitud ${item.field.length}`;
  }
}
