import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { NachaCertificateManagerComponent } from './components/nacha-certificate-manager.component';
import { CertificateVersionsComponent } from './components/certificate-versions.component';
import { DigitalEnvelopeToolComponent } from './components/digital-envelope-tool.component';
import { NachaGenerateOperationComponent } from './components/nacha-generate-operation.component';
import { NachaGenerateEncryptedOperationComponent } from './components/nacha-generate-encrypted-operation.component';
import { ManualEncryptOperationComponent } from './components/manual-encrypt-operation.component';
import { ManualDecryptOperationComponent } from './components/manual-decrypt-operation.component';
import { NACHA_SECURITY_PERMISSIONS } from './nacha-security-permissions';

export const NACHA_SECURITY_ROUTES: Routes = [
  {
    path: '',
    canActivate: [permissionGuard],
    canActivateChild: [permissionGuard],
    data: { breadcrumb: 'Seguridad de archivos', title: 'Seguridad de archivos' },
    children: [
      { path: 'dashboard', pathMatch: 'full', redirectTo: 'certificates' },
      { path: 'certificates', component: NachaCertificateManagerComponent, data: { breadcrumb: 'Certificados digitales', title: 'Certificados digitales', permissions: [NACHA_SECURITY_PERMISSIONS.certificatesRead, NACHA_SECURITY_PERMISSIONS.canManageCertificates, NACHA_SECURITY_PERMISSIONS.canReadAch] } },
      { path: 'certificates/:id/versions', component: CertificateVersionsComponent, data: { breadcrumb: 'Versiones', title: 'Versiones de certificado', permissions: [NACHA_SECURITY_PERMISSIONS.certificatesRead, NACHA_SECURITY_PERMISSIONS.canManageCertificates, NACHA_SECURITY_PERMISSIONS.canReadAch] } },
      { path: 'nacha/generate', component: NachaGenerateOperationComponent, data: { breadcrumb: 'Generar NACHA', title: 'Generar NACHA-M', permissions: [NACHA_SECURITY_PERMISSIONS.canGenerateNacha, NACHA_SECURITY_PERMISSIONS.canManageAch, NACHA_SECURITY_PERMISSIONS.canReadAch] } },
      { path: 'nacha/generate-encrypted', component: NachaGenerateEncryptedOperationComponent, data: { breadcrumb: 'Generar cifrado', title: 'Generar NACHA-M cifrado', permissions: [NACHA_SECURITY_PERMISSIONS.canGenerateEncryptedNacha, NACHA_SECURITY_PERMISSIONS.canManageAch, NACHA_SECURITY_PERMISSIONS.canReadAch] } },
      { path: 'digital-envelope/manual-encrypt', component: ManualEncryptOperationComponent, data: { breadcrumb: 'Cifrado manual', title: 'Cifrado manual sobre digital', permissions: [NACHA_SECURITY_PERMISSIONS.canManualEncryptEnvelope, NACHA_SECURITY_PERMISSIONS.canManageAch, NACHA_SECURITY_PERMISSIONS.canReadAch] } },
      { path: 'digital-envelope/manual-decrypt', component: ManualDecryptOperationComponent, data: { breadcrumb: 'Descifrado manual', title: 'Descifrado manual sobre digital', permissions: [NACHA_SECURITY_PERMISSIONS.canManualDecryptEnvelope, NACHA_SECURITY_PERMISSIONS.canManageAch, NACHA_SECURITY_PERMISSIONS.canReadAch] } },
      { path: 'sobre-digital', component: DigitalEnvelopeToolComponent, data: { breadcrumb: 'Cifrar y descifrar archivos', title: 'Cifrar y descifrar archivos', permissions: [NACHA_SECURITY_PERMISSIONS.canReadAch] } },
      { path: '', pathMatch: 'full', redirectTo: 'certificates' }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(NACHA_SECURITY_ROUTES)],
  exports: [RouterModule]
})
export class NachaSecurityRoutingModule {}
