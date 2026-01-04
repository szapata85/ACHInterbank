import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NachaCertificateManagerComponent } from './components/nacha-certificate-manager.component';
import { DigitalEnvelopeToolComponent } from './components/digital-envelope-tool.component';

const routes: Routes = [
  {
    path: 'certificates',
    component: NachaCertificateManagerComponent,
    data: {
      title: 'Seguridad NACHA',
      breadcrumb: 'Seguridad NACHA'
    }
  },
  {
    path: 'sobre-digital',
    component: DigitalEnvelopeToolComponent,
    data: {
      title: 'Sobre digital',
      breadcrumb: 'Sobre digital'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NachaSecurityRoutingModule {}
