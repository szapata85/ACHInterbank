import { TestBed } from '@angular/core/testing';
import { ApiService } from '../../../core/services/api.service';
import { BankHoliday } from '../models/bank-holiday.model';
import { BankHolidaysAdminService } from './bank-holidays-admin.service';

describe('BankHolidaysAdminService', () => {
  let service: BankHolidaysAdminService;
  let api: jasmine.SpyObj<ApiService>;
  const payload: BankHoliday = {
    id: 4,
    date: '2026-12-31',
    description: 'Fin de año',
    countryCode: 'CO'
  };

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'put', 'delete']);
    TestBed.configureTestingModule({
      providers: [
        BankHolidaysAdminService,
        { provide: ApiService, useValue: api }
      ]
    });
    service = TestBed.inject(BankHolidaysAdminService);
  });

  it('uses the existing endpoint and sends the selected year as a query parameter', () => {
    service.list(2026);

    expect(api.get).toHaveBeenCalledOnceWith('bank-holidays', { params: { year: 2026 } });
  });

  it('preserves the existing create, update and delete contracts', () => {
    service.create(payload);
    service.update(payload);
    service.delete(payload.id);

    expect(api.post).toHaveBeenCalledOnceWith('bank-holidays', payload);
    expect(api.put).toHaveBeenCalledOnceWith('bank-holidays/4', payload);
    expect(api.delete).toHaveBeenCalledOnceWith('bank-holidays/4');
  });
});
