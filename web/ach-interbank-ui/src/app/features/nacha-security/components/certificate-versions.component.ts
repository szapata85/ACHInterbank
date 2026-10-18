import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { CertificateVersion } from '../models/certificate-management.model';
import { certificateStatusLabel } from '../presentation/certificate-presentation';
import { CertificateManagementApiService } from '../services/certificate-management-api.service';

@Component({
  selector: 'app-certificate-versions',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    PageHeaderComponent
  ],
  template: `
    <app-page-header
      title="Versiones de certificado"
      subtitle="Consulta el historial y la vigencia de las versiones registradas para este certificado."
    ></app-page-header>

    @if (loading) {
      <mat-card class="state-card" appearance="outlined" role="status">
        <mat-spinner diameter="32"></mat-spinner>
        <div>
          <strong>Cargando versiones</strong>
          <p>Consultando el historial del certificado.</p>
        </div>
      </mat-card>
    } @else if (loadError) {
      <mat-card class="state-card state-card--error" appearance="outlined" role="alert">
        <mat-icon aria-hidden="true">error</mat-icon>
        <div>
          <strong>No fue posible consultar las versiones.</strong>
          <p>Intenta nuevamente. Si el problema continúa, contacta a soporte.</p>
          <button mat-stroked-button type="button" (click)="load()">Reintentar</button>
        </div>
      </mat-card>
    } @else if (versions.length === 0) {
      <mat-card class="state-card" appearance="outlined">
        <mat-icon aria-hidden="true">history</mat-icon>
        <div>
          <strong>Este certificado aún no tiene versiones registradas.</strong>
          <p>Las nuevas versiones aparecerán aquí cuando se carguen o reemplacen certificados.</p>
        </div>
      </mat-card>
    } @else {
      <mat-card class="table-card" appearance="outlined">
        <div class="table-scroll">
          <table mat-table [dataSource]="versions">
            <ng-container matColumnDef="version">
              <th mat-header-cell *matHeaderCellDef>Versión</th>
              <td mat-cell *matCellDef="let item">{{ item.versionNumber }}</td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Estado</th>
              <td mat-cell *matCellDef="let item">
                <mat-chip>{{ statusLabel(item) }}</mat-chip>
              </td>
            </ng-container>

            <ng-container matColumnDef="subject">
              <th mat-header-cell *matHeaderCellDef>Titular</th>
              <td mat-cell *matCellDef="let item">{{ item.subject }}</td>
            </ng-container>

            <ng-container matColumnDef="validity">
              <th mat-header-cell *matHeaderCellDef>Vigencia</th>
              <td mat-cell *matCellDef="let item">
                {{ item.notBefore | date:'dd/MM/yyyy':'UTC' }} – {{ item.notAfter | date:'dd/MM/yyyy':'UTC' }}
              </td>
            </ng-container>

            <ng-container matColumnDef="secretRef">
              <th mat-header-cell *matHeaderCellDef>Referencia protegida</th>
              <td mat-cell *matCellDef="let item" class="technical-value">{{ item.secretRefMasked || 'No disponible' }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
          </table>
        </div>
      </mat-card>
    }
  `,
  styles: [`
    :host { display:block; }
    .table-card { border-color:var(--color-border); background:var(--color-surface); overflow:hidden; }
    .table-scroll { overflow:auto; }
    table { width:100%; min-width:760px; }
    th { color:var(--color-text-muted); font-weight:700; }
    td { color:var(--color-text); }
    .technical-value { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:.8rem; }
    .state-card { display:flex; align-items:center; gap:var(--space-3); padding:var(--space-4); border-color:var(--color-border); background:var(--color-surface-muted); color:var(--color-text); }
    .state-card strong, .state-card p { display:block; margin:0; }
    .state-card p { margin-top:var(--space-1); color:var(--color-text-muted); }
    .state-card button { margin-top:var(--space-2); }
    .state-card--error { border-color:var(--color-error-border); background:var(--color-error-surface); color:var(--color-error-text); }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CertificateVersionsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CertificateManagementApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns = ['version', 'status', 'subject', 'validity', 'secretRef'];
  versions: CertificateVersion[] = [];
  loading = false;
  loadError = false;
  private certificateId: number | null = null;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isNaN(id)) {
      this.certificateId = id;
      this.load();
    }
  }

  load(): void {
    if (this.certificateId === null) {
      return;
    }

    this.loading = true;
    this.loadError = false;
    this.api.getVersions(this.certificateId)
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (items) => {
          this.versions = items;
          this.cdr.markForCheck();
        },
        error: () => {
          this.loadError = true;
          this.cdr.markForCheck();
        }
      });
  }

  statusLabel(item: CertificateVersion): string {
    return certificateStatusLabel(item.status);
  }
}
