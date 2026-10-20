import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigLegacyWorkspaceRedirectComponent } from './nacha-config-legacy-workspace-redirect.component';

describe('NachaConfigLegacyWorkspaceRedirectComponent UX2', () => {
  it('redirects legacy routes to the unified workspace preserving the profile context', () => {
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.resolveTo(true);

    TestBed.configureTestingModule({
      imports: [NachaConfigLegacyWorkspaceRedirectComponent],
      providers: [
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {
          snapshot: {
            queryParamMap: convertToParamMap({ profileId: '27', recordCode: '6' }),
            queryParams: { profileId: '27', recordCode: '6' }
          }
        } },
        { provide: NachaConfigQueryService, useValue: { perfiles: () => of([]) } }
      ]
    });

    TestBed.createComponent(NachaConfigLegacyWorkspaceRedirectComponent).detectChanges();

    expect(router.navigate).toHaveBeenCalledWith(
      ['/nacha-config-admin/perfiles', 27],
      { queryParams: { recordCode: '6' }, replaceUrl: true }
    );
  });

  it('uses the first available profile when an old menu route has no profileId', () => {
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.resolveTo(true);

    TestBed.configureTestingModule({
      imports: [NachaConfigLegacyWorkspaceRedirectComponent],
      providers: [
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {
          snapshot: { queryParamMap: convertToParamMap({}), queryParams: {} }
        } },
        { provide: NachaConfigQueryService, useValue: { perfiles: () => of([{ id: 31 }]) } }
      ]
    });

    TestBed.createComponent(NachaConfigLegacyWorkspaceRedirectComponent).detectChanges();

    expect(router.navigate).toHaveBeenCalledWith(
      ['/nacha-config-admin/perfiles', 31],
      { queryParams: {}, replaceUrl: true }
    );
  });
});
