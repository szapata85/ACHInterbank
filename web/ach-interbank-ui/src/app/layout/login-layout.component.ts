import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SharedModule } from '../shared/shared.module';

@Component({
  selector: 'app-login-layout',
  templateUrl: './login-layout.component.html',
  styleUrls: ['./login-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginLayoutComponent {}
