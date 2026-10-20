import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationContainerComponent } from './notification-container.component';

describe('NotificationContainerComponent', () => {
  let fixture: ComponentFixture<NotificationContainerComponent>;
  let notifications: NotificationService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationContainerComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationContainerComponent);
    notifications = TestBed.inject(NotificationService);
    fixture.detectChanges();
  });

  it('renders an asynchronously received ProblemDetails message with OnPush', () => {
    notifications.show('error', 'El perfil diferencial no está publicado.', { autoCloseMs: 0 });
    fixture.detectChanges();

    const toast = fixture.nativeElement.querySelector('.toast.error') as HTMLElement | null;
    expect(toast?.textContent).toContain('El perfil diferencial no está publicado.');
  });
});
