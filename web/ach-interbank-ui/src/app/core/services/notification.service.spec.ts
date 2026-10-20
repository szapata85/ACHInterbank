import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('retira los errores anteriores cuando una consulta posterior se recupera', async () => {
    const service = TestBed.inject(NotificationService);
    service.error('Error recuperable');
    service.info('Contexto vigente');

    service.dismissType('error');

    const messages = await firstValueFrom(service.messages$);
    expect(messages.some((message) => message.type === 'error')).toBeFalse();
    expect(messages.some((message) => message.type === 'info')).toBeTrue();
  });
});
