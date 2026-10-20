import { HttpErrorResponse } from '@angular/common/http';
import { getHttpErrorMessage } from './http-error-message.util';

describe('getHttpErrorMessage', () => {
  it('extracts a plain text response', () => {
    const error = new HttpErrorResponse({ status: 400, error: 'Certificado inválido.' });
    expect(getHttpErrorMessage(error)).toBe('Certificado inválido.');
  });

  it('extracts ProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'Solicitud inválida', detail: 'El archivo no es X.509.' }
    });
    expect(getHttpErrorMessage(error)).toBe('El archivo no es X.509. Solicitud inválida');
  });

  it('extracts ValidationProblemDetails without object coercion', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'Error de validación', errors: { file: ['Archivo requerido.'], password: ['Contraseña incorrecta.'] } }
    });
    const message = getHttpErrorMessage(error);
    expect(message).toContain('Archivo requerido.');
    expect(message).toContain('Contraseña incorrecta.');
    expect(message).not.toContain('[object Object]');
  });

  it('returns a useful message for 403', () => {
    const error = new HttpErrorResponse({ status: 403 });
    expect(getHttpErrorMessage(error)).toContain('No tienes permisos');
  });

  it('returns a useful message for a network error', () => {
    const error = new HttpErrorResponse({ status: 0 });
    expect(getHttpErrorMessage(error)).toContain('conectar con el servidor');
  });
});
