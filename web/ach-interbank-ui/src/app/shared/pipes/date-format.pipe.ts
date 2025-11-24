import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateFormat'
})
export class DateFormatPipe implements PipeTransform {
  transform(value: string | Date | null | undefined, options?: Intl.DateTimeFormatOptions): string {
    if (!value) {
      return '-';
    }

    const date = value instanceof Date ? value : new Date(value);
    const formatOptions: Intl.DateTimeFormatOptions = {
      timeZone: 'America/Bogota',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
      ...(options ?? {})
    };

    return new Intl.DateTimeFormat('es-CO', formatOptions).format(date);
  }
}
