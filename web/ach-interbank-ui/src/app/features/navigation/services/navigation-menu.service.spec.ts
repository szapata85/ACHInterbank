import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { NavigationMenuService } from './navigation-menu.service';

describe('NavigationMenuService', () => {
  it('filters the retired /aliases item from navigation administration data', (done) => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of([
      {
        id: 1,
        label: 'Operación',
        route: '/operations',
        order: 1,
        exact: false,
        isActive: true,
        roleIds: [],
        permissionIds: [],
        children: [
          {
            id: 2,
            parentId: 1,
            label: 'Alias',
            route: '/aliases',
            order: 1,
            exact: true,
            isActive: true,
            roleIds: [],
            permissionIds: []
          }
        ]
      }
    ]));

    TestBed.configureTestingModule({
      providers: [
        NavigationMenuService,
        { provide: ApiService, useValue: api }
      ]
    });

    TestBed.inject(NavigationMenuService).getMenuItems().subscribe((items) => {
      expect(items[0].children).toEqual([]);
      expect(JSON.stringify(items)).not.toContain('/aliases');
      done();
    });
  });
});
