import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { MenuItem } from '../models/menu.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly api = inject(ApiService);

  getMenu(): Observable<MenuItem[]> {
    return this.api.get<MenuItem[]>('navigation/menu').pipe(
      map((items) => this.mergeDefaultMenu(this.sortMenu(items ?? [])))
    );
  }

  private mergeDefaultMenu(items: MenuItem[]): MenuItem[] {

    const transactionsChildren: MenuItem[] = [
      { id: -201, label: 'Listado', route: '/transactions/list', icon: 'list' },
      { id: -202, label: 'Crear transacción', route: '/transactions/create', icon: 'add_circle' },
      { id: -203, label: 'Carga masiva', route: '/transactions/bulk-create', icon: 'upload_file' },
      { id: -2031, label: 'Carga masiva por archivo', route: '/transactions/bulk-ingestion/upload', icon: 'file_upload' },
      { id: -2032, label: 'Seguimiento lotes', route: '/transactions/bulk-ingestion/tracking', icon: 'monitoring' },
      { id: -2033, label: 'Config. ciclos', route: '/transactions/cycle-configs', icon: 'schedule' },
      { id: -204, label: 'Cargar NACHA-M', route: '/transactions/nacha-upload', icon: 'upload' },
      { id: -205, label: 'Devoluciones ACH', route: '/transactions/returns', icon: 'assignment_return' }
    ];

    const transactionsGroup: MenuItem = {
      id: -200,
      label: 'Transacciones',
      route: '/transactions',
      icon: 'payments',
      children: transactionsChildren
    };

    const catalogChildren: MenuItem[] = [
      { id: -2101, label: 'Conceptos de lote', route: '/catalogs/company-entry-descriptions', icon: 'list' },
      { id: -211, label: 'Tipos de documento', route: '/catalogs/document-types', icon: 'badge' },
      { id: -212, label: 'Tipos de género', route: '/catalogs/gender-types', icon: 'diversity_3' },
      { id: -213, label: 'Tipos de persona', route: '/catalogs/person-types', icon: 'apartment' },
      { id: -214, label: 'Tipos de teléfono', route: '/catalogs/phone-types', icon: 'call' },
      { id: -215, label: 'Tipos de correo', route: '/catalogs/email-types', icon: 'mail' },
      { id: -216, label: 'Tipos de dirección', route: '/catalogs/address-types', icon: 'location_on' },
      { id: -217, label: 'Códigos de transacción ACH', route: '/catalogs/transaction-codes', icon: 'numbers' }
    ];

    const catalogGroup: MenuItem = {
      id: -210,
      label: 'Catálogos',
      route: '/catalogs',
      icon: 'list_alt',
      children: catalogChildren
    };
    const customerItem: MenuItem = {
      id: -220,
      label: 'Clientes',
      route: '/customers',
      icon: 'group'
    };

    const reportsItem: MenuItem = {
      id: -240,
      label: 'Reportes',
      route: '/reports',
      icon: 'analytics'
    };


    const logsChildren: MenuItem[] = [
      { id: -231, label: 'Log de auditoría', route: '/audit-logs', icon: 'fact_check' },
      { id: -232, label: 'Log de autenticaciones', route: '/auth-logs', icon: 'shield' },
      { id: -233, label: 'Log de navegación', route: '/navigation-logs', icon: 'route' }
    ];

    const logsGroup: MenuItem = {
      id: -230,
      label: 'Logs',
      route: '/audit-logs',
      icon: 'receipt_long',
      children: logsChildren
    };

    if (!items.length) {
      return [transactionsGroup, customerItem, reportsItem, logsGroup, catalogGroup];
    }

    const hasRoute = (menu: MenuItem[], route: string): boolean =>
      menu.some((item) => item.route === route || (item.children?.length && hasRoute(item.children, route)));

    const existingCatalogGroup = items.find((item) => item.route === '/catalogs' || item.label === 'Catálogos');
    if (existingCatalogGroup) {
      const existingChildren = existingCatalogGroup.children ?? [];
      const missingChildren = catalogChildren.filter((child) => !hasRoute(existingChildren, child.route));
      if (missingChildren.length) {
        existingCatalogGroup.children = [...existingChildren, ...missingChildren];
      }
      const existingTransactionsGroup = items.find((item) => item.route === '/transactions' || item.label === 'Transacciones');
      if (existingTransactionsGroup) {
        const existingTransactionChildren = existingTransactionsGroup.children ?? [];
        const missingTransactionChildren = transactionsChildren.filter((child) => !hasRoute(existingTransactionChildren, child.route));
        if (missingTransactionChildren.length) {
          existingTransactionsGroup.children = [...existingTransactionChildren, ...missingTransactionChildren];
        }
      }

      let next = hasRoute(items, transactionsGroup.route) ? items : [...items, transactionsGroup];
      if (!hasRoute(next, customerItem.route)) {
        next = [...next, customerItem];
      }
      if (!hasRoute(next, reportsItem.route)) {
        next = [...next, reportsItem];
      }

      const existingLogsGroup = next.find((item) => item.route === '/audit-logs' || item.label === 'Logs');
      if (existingLogsGroup) {
        const existingLogChildren = existingLogsGroup.children ?? [];
        const missingLogChildren = logsChildren.filter((child) => !hasRoute(existingLogChildren, child.route));
        if (missingLogChildren.length) {
          existingLogsGroup.children = [...existingLogChildren, ...missingLogChildren];
        }
        return next;
      }

      return [...next, logsGroup];
    }

    const withTransactions = hasRoute(items, transactionsGroup.route) ? items : [...items, transactionsGroup];
    const withCustomer = hasRoute(withTransactions, customerItem.route) ? withTransactions : [...withTransactions, customerItem];
    const withReports = hasRoute(withCustomer, reportsItem.route) ? withCustomer : [...withCustomer, reportsItem];
    const withLogs = hasRoute(withReports, '/navigation-logs') || hasRoute(withReports, '/auth-logs') || hasRoute(withReports, '/audit-logs')
      ? withReports
      : [...withReports, logsGroup];

    return [...withLogs, catalogGroup];
  }

  private sortMenu(items: MenuItem[]): MenuItem[] {
    return [...items]
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .map((item) => ({
        ...item,
        children: item.children?.length ? this.sortMenu(item.children) : []
      }));
  }
}
