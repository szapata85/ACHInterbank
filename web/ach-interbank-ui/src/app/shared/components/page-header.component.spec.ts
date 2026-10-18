import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PageHeaderComponent } from './page-header.component';

describe('PageHeaderComponent', () => {
  let fixture: ComponentFixture<PageHeaderComponent>;
  let component: PageHeaderComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PageHeaderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(PageHeaderComponent);
    component = fixture.componentInstance;
  });

  it('renders the canonical page title and contextual description', () => {
    component.title = 'Ciclos de compensación';
    component.description = 'Consulta y administra la operación disponible.';

    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('h2')?.textContent).toContain('Ciclos de compensación');
    expect(host.querySelector('.eyebrow')?.textContent).toContain(
      'Consulta y administra la operación disponible.'
    );
  });
});
