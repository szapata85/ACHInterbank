import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { NachaConfigProfileRecord } from '../../../core/models/nacha-config.model';

@Component({
  selector: 'app-nacha-config-record-list',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatChipsModule, MatListModule],
  templateUrl: './nacha-config-record-list.component.html',
  styleUrls: ['./nacha-config-record-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigRecordListComponent {
  @Input() records: NachaConfigProfileRecord[] = [];
  @Input() selectedRecordCode: string | null = null;
  @Output() selectedRecordCodeChange = new EventEmitter<string>();

  sortedRecords(): NachaConfigProfileRecord[] {
    return [...this.records].sort((a, b) => a.sequence - b.sequence || a.recordCode.localeCompare(b.recordCode));
  }

  label(record: NachaConfigProfileRecord): string {
    return record.recordNameEs?.trim() || record.recordCode;
  }
}
