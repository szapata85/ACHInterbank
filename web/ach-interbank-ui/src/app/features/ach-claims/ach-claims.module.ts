import { NgModule } from '@angular/core';
import { AchClaimsRoutingModule } from './ach-claims-routing.module';
import { AchClaimsDetailComponent } from './ach-claims-detail.component';
import { AchClaimsListComponent } from './ach-claims-list.component';

@NgModule({
  imports: [AchClaimsRoutingModule, AchClaimsListComponent, AchClaimsDetailComponent]
})
export class AchClaimsModule {}
