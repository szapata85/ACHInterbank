import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  IntegrationConnectionChangePreviewDto,
  IntegrationConnectionChangePreviewRequestDto,
  IntegrationConnectionSettingsDto
} from './integration-api.models';

@Injectable({ providedIn: 'root' })
export class IntegrationConnectionApi {
  private readonly api = inject(ApiService);

  getSettings(): Observable<IntegrationConnectionSettingsDto> {
    return this.api.get<IntegrationConnectionSettingsDto>('api/users/soap-integrations');
  }

  previewChange(request: IntegrationConnectionChangePreviewRequestDto): Observable<IntegrationConnectionChangePreviewDto> {
    return this.api.post<IntegrationConnectionChangePreviewDto>('api/users/soap-integrations/preview-change', request);
  }

  updateSettings(settings: IntegrationConnectionSettingsDto): Observable<IntegrationConnectionSettingsDto> {
    return this.api.put<IntegrationConnectionSettingsDto>('api/users/soap-integrations', settings);
  }
}
