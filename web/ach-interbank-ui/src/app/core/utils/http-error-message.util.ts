import { HttpErrorResponse } from '@angular/common/http';

const statusMessages: Record<number, string> = {
  0: 'No fue posible conectar con el servidor. Verifica la conexión e intenta nuevamente.',
  400: 'La solicitud contiene datos inválidos.',
  401: 'La sesión no es válida o expiró.',
  403: 'No tienes permisos para realizar esta operación.',
  409: 'La operación entra en conflicto con un registro existente.',
  413: 'El archivo supera el tamaño máximo permitido.',
  500: 'El servidor no pudo procesar la solicitud.'
};

export function getHttpErrorMessage(error: unknown, fallback = 'No fue posible completar la operación.'): string {
  const payload = error instanceof HttpErrorResponse ? error.error : error;
  const messages = collectMessages(payload);

  if (messages.length > 0) {
    return messages.join(' ');
  }

  if (error instanceof HttpErrorResponse) {
    const statusMessage = statusMessages[error.status];
    if (statusMessage) {
      return statusMessage;
    }

    if (error.message?.trim()) {
      return error.message.trim();
    }
  }

  if (error instanceof Error && error.message?.trim()) {
    return error.message.trim();
  }

  return fallback;
}

function collectMessages(value: unknown, depth = 0): string[] {
  if (depth > 5 || value === null || value === undefined) {
    return [];
  }

  if (typeof value === 'string') {
    const message = value.trim();
    return message && message !== '[object Object]' ? [message] : [];
  }

  if (Array.isArray(value)) {
    return distinct(value.flatMap((item) => collectMessages(item, depth + 1)));
  }

  if (value instanceof Error) {
    return collectMessages(value.message, depth + 1);
  }

  if (typeof value !== 'object') {
    return [];
  }

  const record = value as Record<string, unknown>;
  const preferred = ['detail', 'message', 'errors', 'title'];
  const preferredKeys = preferred.filter((key) => key in record);
  const values = preferredKeys.length > 0 ? preferredKeys.map((key) => record[key]) : Object.values(record);
  return distinct(values.flatMap((item) => collectMessages(item, depth + 1)));
}

function distinct(messages: string[]): string[] {
  return [...new Set(messages.map((message) => message.trim()).filter(Boolean))];
}
