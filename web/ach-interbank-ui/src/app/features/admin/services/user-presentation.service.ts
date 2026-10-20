import { Injectable } from '@angular/core';
import { RoleSummary } from '../../../core/models/role.model';

@Injectable({ providedIn: 'root' })
export class UserPresentationService {
  roleLabel(role: Pick<RoleSummary, 'name' | 'description'>): string {
    const knownLabels: Record<string, string> = {
      Admin: 'Administrador',
      'ACH.Operator': 'Operador ACH'
    };

    return knownLabels[role.name] ?? role.description ?? role.name;
  }
}
