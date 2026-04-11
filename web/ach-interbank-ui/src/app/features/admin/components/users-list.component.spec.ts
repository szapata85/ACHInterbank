import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { UsersListComponent } from './users-list.component';
import { RolesApiService, UsersApiService } from '../services/users-api.service';
import { NotificationService } from '../../../core/services/notification.service';

describe('UsersListComponent', () => {
  let fixture: ComponentFixture<UsersListComponent>;
  let component: UsersListComponent;
  let usersApi: jasmine.SpyObj<UsersApiService>;
  let rolesApi: jasmine.SpyObj<RolesApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    usersApi = jasmine.createSpyObj<UsersApiService>('UsersApiService', ['getUsers', 'deactivateUser']);
    rolesApi = jasmine.createSpyObj<RolesApiService>('RolesApiService', ['getRoles']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error', 'warning']);

    usersApi.getUsers.and.returnValue(of({ items: [], total: 0 } as any));
    rolesApi.getRoles.and.returnValue(of([]));
    usersApi.deactivateUser.and.returnValue(of({} as any));

    await TestBed.configureTestingModule({
      imports: [UsersListComponent],
      providers: [
        { provide: UsersApiService, useValue: usersApi },
        { provide: RolesApiService, useValue: rolesApi },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigate']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UsersListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
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
