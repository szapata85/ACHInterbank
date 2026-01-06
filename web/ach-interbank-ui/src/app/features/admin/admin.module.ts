import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AdminRoutingModule } from './admin-routing.module';
import { UsersListComponent } from './components/users-list.component';
import { UserFormComponent } from './components/user-form.component';
import { UserRolesComponent } from './components/user-roles.component';
import { BrandingSettingsComponent } from './components/branding-settings.component';
import { PasswordRulesSettingsComponent } from './components/password-rules-settings.component';

@NgModule({
  imports: [
    SharedModule,
    AdminRoutingModule,
    UsersListComponent,
    UserFormComponent,
    UserRolesComponent,
    BrandingSettingsComponent,
    PasswordRulesSettingsComponent
  ]
})
export class AdminModule {}
