import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { CertificateManagementApiService } from '../services/certificate-management-api.service';
import { CertificateVersion } from '../models/certificate-management.model';

@Component({
  selector: 'app-certificate-versions',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="page-header"><h1>Versiones de certificado</h1></section>
    <table class="table" *ngIf="versions.length">
      <thead><tr><th>versión</th><th>estado</th><th>subject</th><th>vigencia</th><th>secretRef</th></tr></thead>
      <tbody>
        <tr *ngFor="let item of versions">
          <td>{{ item.versionNumber }}</td>
          <td>{{ item.status }}</td>
          <td>{{ item.subject }}</td>
          <td>{{ item.notBefore | date:'shortDate' }} - {{ item.notAfter | date:'shortDate' }}</td>
          <td>{{ item.secretRefMasked || '-' }}</td>
        </tr>
      </tbody>
    </table>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CertificateVersionsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CertificateManagementApiService);

  versions: CertificateVersion[] = [];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isNaN(id)) {
      this.api.getVersions(id).subscribe({ next: (items) => this.versions = items });
    }
  }
}
