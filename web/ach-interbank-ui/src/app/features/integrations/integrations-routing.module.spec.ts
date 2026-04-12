import { TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { IntegrationsRoutingModule } from './integrations-routing.module';

describe('IntegrationsRoutingModule', () => {
  it('should create module', async () => {
    await TestBed.configureTestingModule({
      imports: [IntegrationsRoutingModule]
    }).compileComponents();

    const module = TestBed.inject(RouterModule);
    expect(module).toBeTruthy();
  });
});
