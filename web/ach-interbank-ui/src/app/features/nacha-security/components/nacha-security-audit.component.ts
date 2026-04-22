import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';

@Component({
  selector: 'app-nacha-security-audit',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="page-header"><h1>Auditoría operacional</h1></section>
    <table class="table" *ngIf="rows.length">
      <thead><tr><th>operationId</th><th>tipo</th><th>estado</th><th>usuario</th><th>fecha</th></tr></thead>
      <tbody>
        <tr *ngFor="let row of rows">
          <td>{{ row.operationId }}</td>
          <td>{{ row.operationType }}</td>
          <td>{{ row.status }}</td>
          <td>{{ row.requestedBy }}</td>
          <td>{{ row.requestedAtUtc | date:'short' }}</td>
        </tr>
      </tbody>
    </table>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaSecurityAuditComponent implements OnInit {
  private readonly service = inject(NachaSecurityOperationsApiService);
  rows: NachaSecurityOperationResponse[] = [];

  ngOnInit(): void {
    this.service.getAudit(100).subscribe({ next: (rows) => this.rows = rows });
  }
}
