import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

export interface AchColombiaReturnCandidate {
  id: number;
  originalTransactionId: number;
  reference: string;
  amount: number;
  transactionType: string;
  originalCycleId: string;
  suggestedReturnCycleId: string | null;
  reasonCode: string;
  status: string;
  preparedAtUtc: string;
  canApprove: boolean;
  blockingReason: string | null;
  canGenerateFile: boolean;
}

export interface AchColombiaGeneratedReturnFile {
  fileName: string;
  contentType: string;
  content: string; // byte[] de .NET se serializa como base64.
  totalRecords: number;
  totalReturns: number;
}

export interface AchColombiaReturnDecisionResult {
  id: number;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class AchColombiaReturnReviewApiService {
  private readonly api = inject(ApiService);
  private readonly path = 'api/ach-colombia/devoluciones-automaticas/candidatas';

  list(): Observable<AchColombiaReturnCandidate[]> {
    return this.api.get<AchColombiaReturnCandidate[]>(`${this.path}?limit=50`);
  }

  recoverGeneratedFile(candidateId: number): Observable<AchColombiaGeneratedReturnFile> {
    return this.api.get<AchColombiaGeneratedReturnFile>(`${this.path}/${encodeURIComponent(candidateId)}/archivo`);
  }

  generateApprovedFile(candidateId: number): Observable<AchColombiaGeneratedReturnFile> {
    return this.api.post<AchColombiaGeneratedReturnFile>(
      `${this.path}/${encodeURIComponent(candidateId)}/generar-archivo`, {}
    );
  }

  decide(candidateId: number, action: 'APROBAR' | 'DESCARTAR'): Observable<AchColombiaReturnDecisionResult> {
    return this.api.post<AchColombiaReturnDecisionResult>(
      `${this.path}/${encodeURIComponent(candidateId)}/decision`, { action }
    );
  }
}
