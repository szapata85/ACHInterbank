import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AliasesListComponent } from './aliases-list.component';
import { AliasesApiService } from '../services/aliases-api.service';
import { NotificationService } from '../../../core/services/notification.service';

describe('AliasesListComponent', () => {
  let fixture: ComponentFixture<AliasesListComponent>;
  let component: AliasesListComponent;
  let api: jasmine.SpyObj<AliasesApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<AliasesApiService>('AliasesApiService', ['search']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    api.search.and.returnValue(of({ items: [], total: 0 } as any));

    await TestBed.configureTestingModule({
      imports: [AliasesListComponent],
      providers: [
        { provide: AliasesApiService, useValue: api },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigate']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AliasesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('updates loading/data state on successful load', () => {
    component.load();

    expect(component.loading).toBeFalse();
    expect(component.hasLoaded).toBeTrue();
    expect(component.loadError).toBeNull();
    expect(component.aliases.length).toBe(0);
  });

  it('updates error state on failed load', () => {
    api.search.and.returnValue(throwError(() => new Error('boom')));

    component.load();

    expect(component.loading).toBeFalse();
    expect(component.hasLoaded).toBeTrue();
    expect(component.loadError).toContain('No fue posible cargar los alias registrados');
    expect(notifications.error).toHaveBeenCalled();
  });
});
