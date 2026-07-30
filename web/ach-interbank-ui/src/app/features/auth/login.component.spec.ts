import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { LoginRequestModel, UserSession } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  const session: UserSession = {
    token: 'test-token',
    username: 'operador',
    fullName: 'Operador de pruebas',
    roles: [],
    permissions: []
  };

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('creates a non-nullable form with the required validators', () => {
    expect(component.form.controls.username.value).toBe('');
    expect(component.form.controls.password.value).toBe('');
    expect(component.form.invalid).toBeTrue();

    component.form.controls.username.setValue('ab');
    component.form.controls.password.setValue('12345');

    expect(component.form.controls.username.hasError('minlength')).toBeTrue();
    expect(component.form.controls.password.hasError('minlength')).toBeTrue();

    component.form.setValue(validCredentials());

    expect(component.form.valid).toBeTrue();
  });

  it('renders local SVG account, lock and password visibility icons', () => {
    const account = fixture.nativeElement.querySelector(
      'svg[data-login-icon="account"]'
    ) as SVGElement;
    const lock = fixture.nativeElement.querySelector(
      'svg[data-login-icon="lock"]'
    ) as SVGElement;
    const visibility = fixture.nativeElement.querySelector(
      'svg[data-login-icon="visibility"]'
    ) as SVGElement;

    expect(account).not.toBeNull();
    expect(lock).not.toBeNull();
    expect(visibility).not.toBeNull();
    expect(account.getAttribute('viewBox')).toBe('0 0 24 24');
    expect(lock.getAttribute('viewBox')).toBe('0 0 24 24');
    expect(fixture.nativeElement.querySelector('mat-icon')).toBeNull();
  });

  it('does not submit an invalid form and exposes validation feedback after the attempt', () => {
    component.submit();
    fixture.detectChanges();

    expect(authService.login).not.toHaveBeenCalled();
    expect(component.form.controls.username.touched).toBeTrue();
    expect(component.form.controls.password.touched).toBeTrue();
    expect(fixture.nativeElement.querySelectorAll('mat-error').length).toBeGreaterThan(0);
    expect(submitButton().disabled).toBeTrue();
  });

  it('calls the existing authentication service with the form DTO and redirects to root', () => {
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);
    authService.login.and.returnValue(of(session));
    component.form.setValue(validCredentials());

    component.submit();

    expect(authService.login).toHaveBeenCalledOnceWith(validCredentials());
    expect(navigateSpy).toHaveBeenCalledOnceWith(['/']);
    expect(component.isSubmitting).toBeFalse();
  });

  it('submits the valid reactive form with Enter through the native form event', () => {
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);
    authService.login.and.returnValue(of(session));
    component.form.setValue(validCredentials());
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    expect(authService.login).toHaveBeenCalledOnceWith(validCredentials());
    expect(navigateSpy).toHaveBeenCalledOnceWith(['/']);
  });

  it('shows loading and prevents a duplicate submission while authentication is pending', () => {
    const pendingLogin = new Subject<UserSession>();
    authService.login.and.returnValue(pendingLogin.asObservable());
    component.form.setValue(validCredentials());

    component.submit();
    component.submit();
    fixture.detectChanges();

    expect(authService.login).toHaveBeenCalledTimes(1);
    expect(component.isSubmitting).toBeTrue();
    expect(submitButton().disabled).toBeTrue();
    expect(fixture.nativeElement.querySelector('mat-spinner')).not.toBeNull();

    pendingLogin.next(session);
    pendingLogin.complete();
    fixture.detectChanges();

    expect(component.isSubmitting).toBeFalse();
    expect(fixture.nativeElement.querySelector('mat-spinner')).toBeNull();
  });

  it('shows a safe credentials message for an unauthorized response', () => {
    authService.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }))
    );
    component.form.setValue(validCredentials());

    component.submit();
    fixture.detectChanges();

    expect(component.errorMessage).toContain('Usuario o contraseña incorrectos');
    expect(alertText()).toContain('Usuario o contraseña incorrectos');
    expect(component.isSubmitting).toBeFalse();
  });

  it('distinguishes a connectivity failure from invalid credentials', () => {
    authService.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' }))
    );
    component.form.setValue(validCredentials());

    component.submit();
    fixture.detectChanges();

    expect(component.errorMessage).toContain('conectar con el servicio');
  });

  it('shows a server availability message for a server failure', () => {
    authService.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 503, statusText: 'Service Unavailable' }))
    );
    component.form.setValue(validCredentials());

    component.submit();
    fixture.detectChanges();

    expect(component.errorMessage).toContain('servicio de autenticación no está disponible');
    expect(alertText()).not.toContain('Service Unavailable');
  });

  it('toggles password visibility without clearing the value or submitting the form', () => {
    component.form.controls.password.setValue('Clave-Ficticia-123!');
    fixture.detectChanges();

    expect(passwordInput().type).toBe('password');
    expect(toggleButton().type).toBe('button');
    expect(toggleButton().getAttribute('aria-label')).toBe('Mostrar contraseña');
    expect(toggleButton().getAttribute('aria-pressed')).toBe('false');
    expect(
      toggleButton().querySelector('svg[data-login-icon="visibility"]')
    ).not.toBeNull();

    toggleButton().click();
    fixture.detectChanges();

    expect(passwordInput().type).toBe('text');
    expect(component.form.controls.password.value).toBe('Clave-Ficticia-123!');
    expect(toggleButton().getAttribute('aria-label')).toBe('Ocultar contraseña');
    expect(toggleButton().getAttribute('aria-pressed')).toBe('true');
    expect(
      toggleButton().querySelector('svg[data-login-icon="visibility-off"]')
    ).not.toBeNull();
    expect(authService.login).not.toHaveBeenCalled();

    toggleButton().click();
    fixture.detectChanges();

    expect(passwordInput().type).toBe('password');
  });

  function validCredentials(): LoginRequestModel {
    return {
      username: 'operador',
      password: 'Clave-Segura-123!'
    };
  }

  function passwordInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector(
      'input[formControlName="password"]'
    ) as HTMLInputElement;
  }

  function toggleButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button.password-toggle') as HTMLButtonElement;
  }

  function submitButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
  }

  function alertText(): string {
    return (fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement).textContent ?? '';
  }
});
