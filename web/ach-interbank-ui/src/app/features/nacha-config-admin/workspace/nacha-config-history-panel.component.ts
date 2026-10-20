import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { NachaConfigHistoryItem, NachaConfigWorkspace } from '../../../core/models/nacha-config.model';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';

@Component({
  selector: 'app-nacha-config-history-panel',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './nacha-config-history-panel.component.html',
  styleUrls: ['./nacha-config-history-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigHistoryPanelComponent implements OnChanges {
  private readonly query = inject(NachaConfigQueryService);

  @Input({ required: true }) perfil!: NachaConfigWorkspace;

  items: NachaConfigHistoryItem[] = [];
  loading = false;
  errorMessage = '';
  expandedId: number | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['perfil']?.currentValue?.id) {
      this.load();
    }
  }

  load(): void {
    if (!this.perfil?.id || this.loading) {
      return;
    }
    this.loading = true;
    this.errorMessage = '';
    this.query.historial(this.perfil.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: (items) => this.items = items,
      error: (error: unknown) => {
        this.items = [];
        this.errorMessage = nachaConfigUserMessage(error, 'No fue posible consultar el historial. Intenta nuevamente.');
      }
    });
  }

  toggle(item: NachaConfigHistoryItem): void {
    this.expandedId = this.expandedId === item.id ? null : item.id;
  }
}
