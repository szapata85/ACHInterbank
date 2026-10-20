import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { BrandingSettings } from '../core/models/branding.model';
import { BrandingService } from '../core/services/branding.service';
import { LoginLayoutComponent } from './login-layout.component';

describe('LoginLayoutComponent', () => {
  let fixture: ComponentFixture<LoginLayoutComponent>;
  let brandingSource: BehaviorSubject<BrandingSettings>;

  beforeEach(async () => {
    brandingSource = new BehaviorSubject<BrandingSettings>({
      publicLogo: 'data:image/png;base64,public-logo'
    });

    await TestBed.configureTestingModule({
      imports: [LoginLayoutComponent],
      providers: [
        provideRouter([]),
        {
          provide: BrandingService,
          useValue: {
            branding$: brandingSource.asObservable(),
            productName: 'ACH Interbank'
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginLayoutComponent);
    fixture.detectChanges();
  });

  it('renders the public logo and centralized product fallback from BrandingService', () => {
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('.logo img')).not.toBeNull();
    expect(host.querySelector('.title')?.textContent).toContain('ACH Interbank');
    expect(host.querySelector('main')?.getAttribute('aria-label')).toBe('Acceso a ACH Interbank');
  });
});
