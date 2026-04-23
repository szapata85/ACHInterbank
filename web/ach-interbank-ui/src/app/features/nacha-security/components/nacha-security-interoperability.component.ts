import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InteroperabilityApiService } from '../services/interoperability-api.service';
import { InteroperabilityStatus } from '../models/nacha-security-operation.model';

@Component({
  selector: 'app-nacha-security-interoperability',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="page-header"><h1>Interoperabilidad / vector oficial</h1></section>
    <ng-container *ngIf="status as s">
      <p><b>Vector oficial:</b> {{ s.officialVectorStatus }}</p>
      <p><b>Metadata cargada:</b> {{ s.officialMetadataLoaded ? 'Sí' : 'No' }}</p>
      <p><b>Go/No-Go:</b> {{ s.goNoGo }}</p>
      <p><b>identifier/IV hardening:</b> {{ s.identifierIvHardening.allowed ? 'Permitido' : 'Bloqueado' }} - {{ s.identifierIvHardening.reason }}</p>
    </ng-container>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaSecurityInteroperabilityComponent implements OnInit {
  private readonly api = inject(InteroperabilityApiService);
  status?: InteroperabilityStatus;

  ngOnInit(): void {
    this.api.getStatus().subscribe({ next: (status) => this.status = status });
  }
}
