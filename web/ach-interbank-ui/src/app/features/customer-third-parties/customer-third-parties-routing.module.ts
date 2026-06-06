import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerThirdPartiesComponent } from './components/customer-third-parties.component';

const routes: Routes = [
  {
    path: '',
    component: CustomerThirdPartiesComponent,
    data: { breadcrumb: 'Terceros prenotificación', title: 'Terceros de prenotificación' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CustomerThirdPartiesRoutingModule {}
