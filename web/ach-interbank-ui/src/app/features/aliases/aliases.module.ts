import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AliasesRoutingModule } from './aliases-routing.module';
import { AliasesListComponent } from './components/aliases-list.component';
import { AliasFormComponent } from './components/alias-form.component';

@NgModule({
  imports: [SharedModule, AliasesRoutingModule, AliasesListComponent, AliasFormComponent]
})
export class AliasesModule {}
