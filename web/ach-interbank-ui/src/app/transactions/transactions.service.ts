import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, map, of, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { DestinationInstitution, TransactionDraft, TransactionResponse } from './transactions.models';

@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/transactions`;
  private readonly institutionsUrl = `${environment.apiBaseUrl}/FinancialInstitution`;

  registerTransaction(payload: TransactionDraft) {
    return this.http.post<TransactionResponse>(this.baseUrl, payload).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 400) {
          return throwError(() => new Error(error.error?.message ?? 'Solicitud inválida'));
        }
        if (error.status === 401) {
          return throwError(() => new Error('No autorizado, inicie sesión nuevamente.'));
        }
        return throwError(() => new Error('No fue posible crear la transacción.'));
      })
    );
  }

  getDestinationInstitutions() {
    return this.http
      .get<Array<{ id: number; name: string; routingNumber: string; status: number }>>(
        `${this.institutionsUrl}?includeInactive=false`
      )
      .pipe(
        map((institutions) =>
          (institutions ?? [])
            .filter((institution) => institution.status === 1)
            .map((institution) => ({
              id: institution.id,
              name: institution.name,
              routingNumber: institution.routingNumber
            }))
        ),
        catchError(() => of([]))
      );
  }
}
