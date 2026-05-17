import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { TransactionsRoutingModule } from './transactions-routing.module';

describe('TransactionsRoutingModule', () => {
  it('ruta /transactions/returns-ror existe', async () => {
    await TestBed.configureTestingModule({
      imports: [RouterTestingModule, TransactionsRoutingModule]
    }).compileComponents();

    const router = TestBed.inject(Router);
    const hasRoute = router.config.some((r) => r.path === 'returns-ror');
    expect(hasRoute).toBeTrue();
  });
});
