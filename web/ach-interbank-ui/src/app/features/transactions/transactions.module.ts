import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TransactionFormComponent } from './transaction-form.component';

const routes: Routes = [{ path: 'new', component: TransactionFormComponent }];

@NgModule({
  declarations: [TransactionFormComponent],
  imports: [SharedModule, RouterModule.forChild(routes)]
})
export class TransactionsModule {}
