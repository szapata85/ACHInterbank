import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { NachaCertificateManagerComponent } from './components/nacha-certificate-manager.component';
import { CertificateVersionsComponent } from './components/certificate-versions.component';
import { DigitalEnvelopeToolComponent } from './components/digital-envelope-tool.component';
import { NachaSecurityDashboardComponent } from './components/nacha-security-dashboard.component';
import { NachaGenerateOperationComponent } from './components/nacha-generate-operation.component';
import { NachaGenerateEncryptedOperationComponent } from './components/nacha-generate-encrypted-operation.component';
import { ManualEncryptOperationComponent } from './components/manual-encrypt-operation.component';
import { ManualDecryptOperationComponent } from './components/manual-decrypt-operation.component';
import { NachaSecurityAuditComponent } from './components/nacha-security-audit.component';
import { NachaSecurityInteroperabilityComponent } from './components/nacha-security-interoperability.component';

const routes: Routes = [
  {
    path: '',
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Seguridad NACHA', title: 'Seguridad NACHA' },
    children: [
      { path: 'dashboard', component: NachaSecurityDashboardComponent, data: { breadcrumb: 'Dashboard', title: 'Dashboard seguridad NACHA' } },
      { path: 'certificates', component: NachaCertificateManagerComponent, data: { breadcrumb: 'Certificados', title: 'Gobierno de certificados' } },
      { path: 'certificates/:id/versions', component: CertificateVersionsComponent, data: { breadcrumb: 'Versiones', title: 'Versiones de certificado' } },
      { path: 'nacha/generate', component: NachaGenerateOperationComponent, data: { breadcrumb: 'Generar NACHA', title: 'Generar NACHA-M' } },
      { path: 'nacha/generate-encrypted', component: NachaGenerateEncryptedOperationComponent, data: { breadcrumb: 'Generar cifrado', title: 'Generar NACHA-M cifrado' } },
      { path: 'digital-envelope/manual-encrypt', component: ManualEncryptOperationComponent, data: { breadcrumb: 'Cifrado manual', title: 'Cifrado manual sobre digital' } },
      { path: 'digital-envelope/manual-decrypt', component: ManualDecryptOperationComponent, data: { breadcrumb: 'Descifrado manual', title: 'Descifrado manual sobre digital' } },
      { path: 'digital-envelope/audit', component: NachaSecurityAuditComponent, data: { breadcrumb: 'Auditoría', title: 'Auditoría operacional' } },
      { path: 'digital-envelope/interoperability', component: NachaSecurityInteroperabilityComponent, data: { breadcrumb: 'Interoperabilidad', title: 'Interoperabilidad y vector oficial' } },
      { path: 'sobre-digital', component: DigitalEnvelopeToolComponent, data: { breadcrumb: 'Sobre digital (legacy)', title: 'Sobre digital (legacy)' } },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NachaSecurityRoutingModule {}
