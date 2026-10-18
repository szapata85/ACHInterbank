import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { UsersListComponent } from './users-list.component';
import { UsersApiService } from '../services/users-api.service';
import { RolesApiService } from '../../../core/services/roles-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NavigationNamingService } from '../../../core/services/navigation-naming.service';

describe('UsersListComponent', () => {
  let fixture: ComponentFixture<UsersListComponent>;
  let component: UsersListComponent;
  let usersApi: jasmine.SpyObj<UsersApiService>;
  let rolesApi: jasmine.SpyObj<RolesApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let navigationNaming: jasmine.SpyObj<NavigationNamingService>;

  beforeEach(async () => {
    usersApi = jasmine.createSpyObj<UsersApiService>('UsersApiService', ['getUsers', 'deactivateUser']);
    rolesApi = jasmine.createSpyObj<RolesApiService>('RolesApiService', ['getRoles']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error', 'warning']);
    navigationNaming = jasmine.createSpyObj<NavigationNamingService>('NavigationNamingService', ['canonicalLabelForRoute'], { menu$: of([]) });
    navigationNaming.canonicalLabelForRoute.and.callFake((_menu, _route, fallback) => fallback);

    usersApi.getUsers.and.returnValue(of({ items: [], total: 0 } as any));
    rolesApi.getRoles.and.returnValue(of([]));
    usersApi.deactivateUser.and.returnValue(of({} as any));

    await TestBed.configureTestingModule({
      imports: [UsersListComponent],
      providers: [
        { provide: UsersApiService, useValue: usersApi },
        { provide: RolesApiService, useValue: rolesApi },
        { provide: NotificationService, useValue: notifications },
        { provide: NavigationNamingService, useValue: navigationNaming },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { title: 'Usuarios y acceso' } } } },
        { provide: Router, useValue: { url: '/users', navigate: jasmine.createSpy('navigate') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UsersListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('uses route metadata as the initial heading fallback', () => {
    expect(component.pageHeading).toBe('Usuarios y acceso');
    expect(fixture.nativeElement.querySelector('#users-title')?.textContent).toContain('Usuarios y acceso');
  });

  it('sets loaded state when API resolves without data', () => {
    component.loadUsers();

    expect(component.loading).toBeFalse();
    expect(component.hasLoaded).toBeTrue();
    expect(component.loadError).toBeNull();
    expect(component.users.length).toBe(0);
  });

  it('sets error state when API fails', () => {
    usersApi.getUsers.and.returnValue(throwError(() => new Error('boom')));

    component.loadUsers();

    expect(component.loading).toBeFalse();
    expect(component.hasLoaded).toBeTrue();
    expect(component.loadError).toContain('No fue posible cargar los usuarios');
    expect(notifications.error).toHaveBeenCalled();
  });
});
