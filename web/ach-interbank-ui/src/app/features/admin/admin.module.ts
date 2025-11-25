import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AdminRoutingModule } from './admin-routing.module';
import { UsersListComponent } from './components/users-list.component';
import { UserFormComponent } from './components/user-form.component';
import { UserRolesComponent } from './components/user-roles.component';

@NgModule({
  declarations: [UsersListComponent, UserFormComponent, UserRolesComponent],
  imports: [SharedModule, AdminRoutingModule]
})
export class AdminModule {}
