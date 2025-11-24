import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'currencyCol'
})
export class CurrencyColPipe implements PipeTransform {
  transform(value: number | string | null | undefined): string {
    if (value === null || value === undefined || value === '') {
      return '-';
    }
    const amount = typeof value === 'string' ? Number(value) : value;
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'COP',
      minimumFractionDigits: 0
    }).format(amount ?? 0);
  }
}
