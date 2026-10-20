import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  CreateThirdPartyAccountCommand,
  CreateThirdPartyCommand,
  RequestThirdPartyAccountRevalidationCommand,
  SetThirdPartyAdministrativeStatusCommand,
  ThirdPartyAccountPage,
  ThirdPartyAccountQuery,
  ThirdPartyAccountReadDto,
  ThirdPartyAccountRevalidationResult,
  ThirdPartyAccountValidationHistoryDto,
  ThirdPartyAdministrationDto,
  ThirdPartyAdministrationQuery,
  ThirdPartyPage,
  UpdateThirdPartyAccountCommand,
  UpdateThirdPartyCommand
} from './third-party-administration.models';

@Injectable({ providedIn: 'root' })
export class ThirdPartyAdministrationApiService {
  private readonly api = inject(ApiService);

  listThirdParties(customerId: number, query: ThirdPartyAdministrationQuery = {}): Observable<ThirdPartyPage> {
    const params: Record<string, string | number> = {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 20
    };
    if (query.search?.trim()) params['search'] = query.search.trim();
    if (query.status !== null && query.status !== undefined) params['status'] = query.status;

    return this.api.get<ThirdPartyPage>(this.basePath(customerId), { params });
  }

  getThirdParty(customerId: number, thirdPartyId: number): Observable<ThirdPartyAdministrationDto> {
    return this.api.get<ThirdPartyAdministrationDto>(`${this.basePath(customerId)}/${thirdPartyId}`);
  }

  createThirdParty(customerId: number, command: CreateThirdPartyCommand): Observable<ThirdPartyAdministrationDto> {
    return this.api.post<ThirdPartyAdministrationDto>(this.basePath(customerId), command);
  }

  updateThirdParty(customerId: number, thirdPartyId: number, command: UpdateThirdPartyCommand): Observable<ThirdPartyAdministrationDto> {
    return this.api.put<ThirdPartyAdministrationDto>(`${this.basePath(customerId)}/${thirdPartyId}`, command);
  }

  setThirdPartyStatus(
    customerId: number,
    thirdPartyId: number,
    command: SetThirdPartyAdministrativeStatusCommand
  ): Observable<ThirdPartyAdministrationDto> {
    return this.api.put<ThirdPartyAdministrationDto>(`${this.basePath(customerId)}/${thirdPartyId}/status`, command);
  }

  listAccounts(customerId: number, thirdPartyId: number, query: ThirdPartyAccountQuery = {}): Observable<ThirdPartyAccountPage> {
    const params: Record<string, number> = {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 50
    };
    if (query.status !== null && query.status !== undefined) params['status'] = query.status;

    return this.api.get<ThirdPartyAccountPage>(`${this.basePath(customerId)}/${thirdPartyId}/accounts`, { params });
  }

  createAccount(
    customerId: number,
    thirdPartyId: number,
    command: CreateThirdPartyAccountCommand
  ): Observable<ThirdPartyAccountReadDto> {
    return this.api.post<ThirdPartyAccountReadDto>(`${this.basePath(customerId)}/${thirdPartyId}/accounts`, command);
  }

  updateAccount(
    customerId: number,
    thirdPartyId: number,
    accountId: number,
    command: UpdateThirdPartyAccountCommand
  ): Observable<ThirdPartyAccountReadDto> {
    return this.api.put<ThirdPartyAccountReadDto>(`${this.basePath(customerId)}/${thirdPartyId}/accounts/${accountId}`, command);
  }

  setAccountStatus(
    customerId: number,
    thirdPartyId: number,
    accountId: number,
    command: SetThirdPartyAdministrativeStatusCommand
  ): Observable<ThirdPartyAccountReadDto> {
    return this.api.put<ThirdPartyAccountReadDto>(`${this.basePath(customerId)}/${thirdPartyId}/accounts/${accountId}/status`, command);
  }

  getValidationHistory(
    customerId: number,
    thirdPartyId: number,
    accountId: number
  ): Observable<ThirdPartyAccountValidationHistoryDto[]> {
    return this.api.get<ThirdPartyAccountValidationHistoryDto[]>(
      `${this.basePath(customerId)}/${thirdPartyId}/accounts/${accountId}/validation-history`
    );
  }

  requestRevalidation(
    customerId: number,
    thirdPartyId: number,
    accountId: number,
    command: RequestThirdPartyAccountRevalidationCommand
  ): Observable<ThirdPartyAccountRevalidationResult> {
    return this.api.post<ThirdPartyAccountRevalidationResult>(
      `${this.basePath(customerId)}/${thirdPartyId}/accounts/${accountId}/revalidations`,
      command
    );
  }

  private basePath(customerId: number): string {
    return `api/customers/${customerId}/third-parties`;
  }
}
