import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface ApplicationProblemDetails {
  title?: string;
  detail?: string;
  message?: string;
  mensaje?: string;
  code?: string;
  errorCode?: string;
  codigo?: string;
  traceId?: string;
  instance?: string;
  ruleId?: string;
  recordType?: string;
  fieldCode?: string;
  fieldName?: string;
  fieldDisplayName?: string;
  startPosition?: number;
  expectedLength?: number;
  reason?: string;
  cause?: string;
  errors?: Record<string, string[] | string>;
}

export class ApplicationDownloadError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly errorCode?: string,
    readonly traceId?: string,
    readonly title?: string,
    readonly problem?: ApplicationProblemDetails
  ) {
    super(message);
    this.name = 'ApplicationDownloadError';
  }
}

export interface SavedDownload {
  fileName: string;
  size: number;
  contentType: string;
}

@Injectable({ providedIn: 'root' })
export class BlobDownloadService {
  private readonly document = inject(DOCUMENT);

  async save(response: HttpResponse<Blob>): Promise<SavedDownload> {
    const blob = response.body;
    if (!blob || blob.size === 0) {
      throw new ApplicationDownloadError('El servidor devolvió un archivo vacío.', response.status, 'DOWNLOAD_EMPTY');
    }

    const contentType = (response.headers.get('content-type') || blob.type || 'application/octet-stream').toLowerCase();
    if (this.isJsonContentType(contentType)) {
      throw await this.problemFromBlob(blob, response.status);
    }
    if (contentType.includes('text/html')) {
      throw new ApplicationDownloadError(
        'El servidor devolvió una página HTML en lugar del archivo solicitado.',
        response.status,
        'DOWNLOAD_CONTENT_TYPE_INVALID'
      );
    }

    const fileName = extractContentDispositionFileName(response.headers.get('content-disposition'));
    if (!fileName) {
      throw new ApplicationDownloadError(
        'El servidor no informó el nombre normativo del archivo.',
        response.status,
        'DOWNLOAD_FILENAME_MISSING'
      );
    }

    const url = URL.createObjectURL(blob);
    const link = this.document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.style.display = 'none';
    this.document.body.appendChild(link);
    try {
      link.click();
    } finally {
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 0);
    }

    return { fileName, size: blob.size, contentType };
  }

  async fromHttpError(error: unknown, fallback: string): Promise<ApplicationDownloadError> {
    if (error instanceof ApplicationDownloadError) {
      return error;
    }

    if (error instanceof HttpErrorResponse) {
      if (error.error instanceof Blob) {
        return this.problemFromBlob(error.error, error.status, fallback);
      }
      return this.problemFromUnknown(error.error, error.status, fallback);
    }

    return new ApplicationDownloadError(fallback, 0);
  }

  private async problemFromBlob(blob: Blob, status: number, fallback = 'No fue posible descargar el archivo.'): Promise<ApplicationDownloadError> {
    try {
      const text = await blob.text();
      const parsed = JSON.parse(text) as ApplicationProblemDetails;
      return this.problemFromUnknown(parsed, status, fallback);
    } catch {
      return new ApplicationDownloadError(
        status === 422 ? 'La operación no cumple las condiciones funcionales requeridas.' : fallback,
        status
      );
    }
  }

  private problemFromUnknown(body: unknown, status: number, fallback: string): ApplicationDownloadError {
    const problem = body && typeof body === 'object' ? body as ApplicationProblemDetails : undefined;
    const fieldMessage = problem?.errors
      ? Object.values(problem.errors).flatMap(value => Array.isArray(value) ? value : [value]).find(Boolean)
      : undefined;
    const message = problem?.detail ?? problem?.mensaje ?? problem?.message ?? fieldMessage ?? problem?.title ?? fallback;
    const code = problem?.errorCode ?? problem?.code ?? problem?.codigo;
    return new ApplicationDownloadError(message, status, code, problem?.traceId, problem?.title, problem);
  }

  private isJsonContentType(contentType: string): boolean {
    return contentType.includes('application/json') || contentType.includes('application/problem+json');
  }
}

export function extractContentDispositionFileName(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null;
  }

  const encoded = /filename\*\s*=\s*(?:UTF-8'')?([^;]+)/i.exec(contentDisposition)?.[1];
  const plain = /filename\s*=\s*(?:"([^"]+)"|([^;]+))/i.exec(contentDisposition);
  const candidate = encoded ?? plain?.[1] ?? plain?.[2];
  if (!candidate) {
    return null;
  }

  try {
    const decoded = decodeURIComponent(candidate.trim().replace(/^"|"$/g, ''));
    return isSafeDownloadFileName(decoded) ? decoded : null;
  } catch {
    return null;
  }
}

function isSafeDownloadFileName(value: string): boolean {
  return Boolean(value)
    && value !== '.'
    && value !== '..'
    && !value.startsWith('.')
    && !value.includes('/')
    && !value.includes('\\')
    && !/[\u0000-\u001f\u007f]/.test(value);
}
