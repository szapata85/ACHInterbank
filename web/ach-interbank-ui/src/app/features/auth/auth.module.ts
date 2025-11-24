import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../../shared/shared.module';
import { LoginComponent } from './login.component';

const routes: Routes = [{ path: '', component: LoginComponent }];

@NgModule({
  declarations: [LoginComponent],
  imports: [SharedModule, ReactiveFormsModule, RouterModule.forChild(routes)]
})
export class AuthModule {}
