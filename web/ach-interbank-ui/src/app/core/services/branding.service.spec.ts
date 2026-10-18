import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { BrandingSettings } from '../models/branding.model';
import { ApiService } from './api.service';
import { BrandingService } from './branding.service';

describe('BrandingService', () => {
  let api: jasmine.SpyObj<ApiService>;
  let service: BrandingService;
  const runtimeTokens = [
    '--brand-primary',
    '--brand-on-primary',
    '--brand-primary-hover',
    '--brand-public-background',
    '--brand-private-background',
    '--brand-sidebar-background',
    '--brand-sidebar-on-background',
    '--mat-sys-primary',
    '--mat-sys-on-primary',
    '--button-color'
  ];

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'put']);
    api.get.and.returnValue(of({}));

    TestBed.configureTestingModule({
      providers: [
        BrandingService,
        { provide: ApiService, useValue: api }
      ]
    });

    service = TestBed.inject(BrandingService);
  });

  afterEach(() => {
    for (const token of runtimeTokens) {
      document.documentElement.style.removeProperty(token);
    }
    TestBed.resetTestingModule();
  });

  it('applies the branding returned by the server and preserves logo configuration', () => {
    const settings: BrandingSettings = {
      publicLogo: 'data:image/png;base64,public-logo',
      privateLogo: 'data:image/png;base64,private-logo',
      publicBackground: 'linear-gradient(135deg, #102030, #304050)',
      privateBackground: '#f1f5f9',
      sidebarBackground: '#172554',
      buttonColor: '#0ea5e9'
    };
    api.get.and.returnValue(of(settings));

    service.refreshFromServer().subscribe();

    expect(service.getBrandingSnapshot()).toEqual(settings);
    expect(rootToken('--brand-public-background')).toBe(settings.publicBackground);
    expect(rootToken('--brand-private-background')).toBe(settings.privateBackground);
    expect(rootToken('--brand-sidebar-background')).toBe(settings.sidebarBackground);
  });

  it('updates runtime design tokens and Material primary tokens from buttonColor without recompiling', () => {
    api.put.and.returnValue(of({ buttonColor: '#abc' }));

    service.updateBranding({ buttonColor: '#abc' }).subscribe();

    expect(rootToken('--brand-primary')).toBe('#aabbcc');
    expect(rootToken('--mat-sys-primary')).toBe('#aabbcc');
    expect(rootToken('--mat-sys-on-primary')).toBe(service.getContrastColor('#aabbcc'));
    expect(rootToken('--button-color')).toBe('#aabbcc');
  });

  it('refreshFromServer reapplies runtime branding after the visible tokens changed', () => {
    api.get.and.returnValue(of({ buttonColor: '#1d4ed8' }));
    service.refreshFromServer().subscribe();
    document.documentElement.style.setProperty('--mat-sys-primary', '#ffffff');

    api.get.and.returnValue(of({ buttonColor: '#15803d' }));
    service.refreshFromServer().subscribe();

    expect(rootToken('--brand-primary')).toBe('#15803d');
    expect(rootToken('--mat-sys-primary')).toBe('#15803d');
  });

  it('uses deterministic readable contrast for light and dark primary colors', () => {
    expect(service.getContrastColor('#ffffff')).toBe('#0f172a');
    expect(service.getContrastColor('#000000')).toBe('#ffffff');
  });

  it('falls back to valid defaults and clears optional runtime values when configuration is missing or invalid', () => {
    document.documentElement.style.setProperty('--brand-public-background', '#000000');
    api.get.and.returnValue(of({
      buttonColor: 'not-a-color',
      publicBackground: '   ',
      privateBackground: null,
      sidebarBackground: null
    }));

    service.refreshFromServer().subscribe();

    expect(rootToken('--brand-primary')).toBe('#2563eb');
    expect(rootToken('--mat-sys-primary')).toBe('#2563eb');
    expect(rootToken('--brand-public-background')).toBe('');
    expect(rootToken('--brand-private-background')).toBe('');
    expect(rootToken('--brand-sidebar-background')).toBe('');
    expect(rootToken('--brand-sidebar-on-background')).toBe('#ffffff');
  });

  function rootToken(name: string): string {
    return document.documentElement.style.getPropertyValue(name).trim();
  }
});
