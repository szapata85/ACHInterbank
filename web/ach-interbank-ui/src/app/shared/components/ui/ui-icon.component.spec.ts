import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UiIconComponent } from './ui-icon.component';

describe('UiIconComponent', () => {
  let fixture: ComponentFixture<UiIconComponent>;
  let component: UiIconComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [UiIconComponent] }).compileComponents();
    fixture = TestBed.createComponent(UiIconComponent);
    component = fixture.componentInstance;
  });

  it('renders a configured Material Symbol key', () => {
    component.name = 'schedule';
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.dataset['iconKey']).toBe('schedule');
    expect(host.dataset['iconResolved']).toBe('schedule');
    expect(host.querySelector('.glyph')?.textContent?.trim()).toBe('schedule');
  });

  it('renders the operational search icon used by the NACHA-M menu', () => {
    component.name = 'manage_search';
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.dataset['iconResolved']).toBe('manage_search');
    expect(host.querySelector('.glyph')?.textContent?.trim()).toBe('manage_search');
  });

  it('uses a controlled fallback for an unknown key', () => {
    component.name = 'not_a_configured_symbol';
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.dataset['iconKey']).toBe('not_a_configured_symbol');
    expect(host.dataset['iconResolved']).toBe('help');
    expect(host.querySelector('.glyph')?.textContent?.trim()).toBe('help');
  });
});
