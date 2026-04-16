import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { DashboardComponent } from './dashboard.component';

const routes: Routes = [{ path: '', component: DashboardComponent, data: { breadcrumb: 'Inicio', title: 'Inicio' } }];

@NgModule({
  imports: [SharedModule, RouterModule.forChild(routes), DashboardComponent]
})
export class DashboardModule {}
