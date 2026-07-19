import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent password visibility', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('starts hidden with a visible eye icon and a non-submit button', () => {
    const input = passwordInput();
    const button = toggleButton();
    const icon = button.querySelector('app-ui-icon') as HTMLElement;

    expect(input.type).toBe('password');
    expect(button.type).toBe('button');
    expect(button.getAttribute('aria-label')).toBe('Mostrar contraseña');
    expect(button.getAttribute('title')).toBe('Mostrar contraseña');
    expect(button.getAttribute('aria-pressed')).toBe('false');
    expect(icon.dataset['iconKey']).toBe('visibility');
  });

  it('toggles both icon and accessible state without losing the FormControl value or submitting', () => {
    const submitSpy = spyOn(component, 'submit').and.callThrough();
    component.form.controls.password.setValue('Clave-Ficticia-123!');
    fixture.detectChanges();

    toggleButton().click();
    fixture.detectChanges();

    expect(passwordInput().type).toBe('text');
    expect(component.form.controls.password.value).toBe('Clave-Ficticia-123!');
    expect(toggleButton().getAttribute('aria-label')).toBe('Ocultar contraseña');
    expect(toggleButton().getAttribute('title')).toBe('Ocultar contraseña');
    expect(toggleButton().getAttribute('aria-pressed')).toBe('true');
    expect((toggleButton().querySelector('app-ui-icon') as HTMLElement).dataset['iconKey']).toBe('visibility_off');
    expect(submitSpy).not.toHaveBeenCalled();
    expect(authService.login).not.toHaveBeenCalled();

    toggleButton().click();
    fixture.detectChanges();

    expect(passwordInput().type).toBe('password');
    expect(component.form.controls.password.value).toBe('Clave-Ficticia-123!');
    expect((toggleButton().querySelector('app-ui-icon') as HTMLElement).dataset['iconKey']).toBe('visibility');
    expect(submitSpy).not.toHaveBeenCalled();
  });

  function passwordInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input[formControlName="password"]') as HTMLInputElement;
  }

  function toggleButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button.password-toggle') as HTMLButtonElement;
  }
});
