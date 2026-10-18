import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  IntegrationMethodDto,
  IntegrationMethodParameterDto,
  IntegrationSourceCatalogFieldDto,
  IntegrationTransformationCatalogDto
} from './integration-api.models';

@Injectable({ providedIn: 'root' })
export class IntegrationWorkspaceApi {
  private readonly api = inject(ApiService);

  getMethods(): Observable<IntegrationMethodDto[]> {
    return this.api.get<IntegrationMethodDto[]>('api/integrations/methods');
  }

  getMethodParameters(methodId: number): Observable<IntegrationMethodParameterDto[]> {
    return this.api.get<IntegrationMethodParameterDto[]>(`api/integrations/methods/${methodId}/parameters`);
  }

  getSourceCatalog(methodId?: number): Observable<IntegrationSourceCatalogFieldDto[]> {
    const query = methodId == null ? '' : `?methodId=${methodId}`;
    return this.api.get<IntegrationSourceCatalogFieldDto[]>(`api/integrations/source-catalog${query}`);
  }

  getTransformations(): Observable<IntegrationTransformationCatalogDto[]> {
    return this.api.get<IntegrationTransformationCatalogDto[]>('api/integrations/transformations');
  }
}
