import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { MenuItem } from '../models/menu.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly api = inject(ApiService);

  getMenu(): Observable<MenuItem[]> {
    return this.api.get<MenuItem[]>('navigation/menu').pipe(
      map((items) => this.mergeDefaultMenu(this.sortMenu(this.removeLegacyNachaMenuItems(items ?? []))))
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
      { id: -2034, label: 'Reglas por camara', route: '/transactions/clearing-house-rules', icon: 'rule' },
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

    const cenitChildren: MenuItem[] = [
      { id: -2501, label: 'Regulatorio: Devoluciones', route: '/cenit/regulatorio/causales-devolucion', icon: 'rule' },
      { id: -2502, label: 'Regulatorio: Rechazos', route: '/cenit/regulatorio/causales-rechazo', icon: 'gavel' },
      { id: -2503, label: 'Regulatorio: Políticas', route: '/cenit/regulatorio/politicas-transaccion', icon: 'policy' },
      { id: -2504, label: 'Operación: Ciclos', route: '/cenit/operacion/ciclos', icon: 'schedule' },
      { id: -2505, label: 'Operación: Cola', route: '/cenit/operacion/cola', icon: 'queue' },
      { id: -2506, label: 'Operación: Neteo', route: '/cenit/operacion/neteo', icon: 'account_balance' },
      { id: -2507, label: 'Operación: Optimización', route: '/cenit/operacion/optimizacion', icon: 'tune' },
      { id: -2508, label: 'Operación: Devoluciones', route: '/cenit/operacion/devoluciones', icon: 'assignment_return' },
      { id: -2509, label: 'Operación: Trazabilidad', route: '/cenit/operacion/trazabilidad', icon: 'travel_explore' }
    ];

    const cenitGroup: MenuItem = {
      id: -250,
      label: 'CENIT',
      route: '/cenit',
      icon: 'monitoring',
      children: cenitChildren
    };



    const nachaSecurityChildren: MenuItem[] = [
      { id: -2601, label: 'Dashboard seguridad', route: '/nacha-security/dashboard', icon: 'shield' },
      { id: -2602, label: 'Certificados', route: '/nacha-security/certificates', icon: 'badge' },
      { id: -2603, label: 'Generar NACHA-M', route: '/nacha-security/nacha/generate', icon: 'description' },
      { id: -2604, label: 'Generar NACHA-M cifrado', route: '/nacha-security/nacha/generate-encrypted', icon: 'encrypted' },
      { id: -2605, label: 'Cifrado manual', route: '/nacha-security/digital-envelope/manual-encrypt', icon: 'lock' },
      { id: -2606, label: 'Descifrado manual', route: '/nacha-security/digital-envelope/manual-decrypt', icon: 'lock_open' },
      { id: -2607, label: 'Auditoría operaciones', route: '/nacha-security/digital-envelope/audit', icon: 'fact_check' },
      { id: -2608, label: 'Interoperabilidad', route: '/nacha-security/digital-envelope/interoperability', icon: 'hub' }
    ];

    const nachaSecurityGroup: MenuItem = {
      id: -260,
      label: 'Seguridad NACHA',
      route: '/nacha-security/dashboard',
      icon: 'security',
      children: nachaSecurityChildren
    };

    const nachaConfigGroup: MenuItem = {
      id: -280,
      label: 'Config Profiles',
      route: '/nacha-config-admin/perfiles',
      icon: 'tune',
      children: [
        { id: -2801, label: 'Config Profiles', route: '/nacha-config-admin/perfiles', icon: 'fact_check' }
      ]
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

    const achResponsesChildren: MenuItem[] = [
      { id: -2701, label: 'Bandeja', route: '/ach-responses', icon: 'assignment' },
      { id: -2702, label: 'Revisión manual', route: '/ach-responses/manual-review', icon: 'rule' },
      { id: -2703, label: 'Homologaciones', route: '/ach-responses/status-mappings', icon: 'sync_alt' },
      { id: -2704, label: 'Dashboard operativo', route: '/ach-responses/dashboard', icon: 'dashboard' }
    ];

    const achResponsesGroup: MenuItem = {
      id: -270,
      label: 'Respuestas ACH',
      route: '/ach-responses',
      icon: 'assignment',
      children: achResponsesChildren
    };

    if (!items.length) {
      return [transactionsGroup, achResponsesGroup, customerItem, reportsItem, cenitGroup, nachaConfigGroup, nachaSecurityGroup, logsGroup, catalogGroup];
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
      if (!hasRoute(next, achResponsesGroup.route)) {
        next = [...next, achResponsesGroup];
      } else {
        const existingAchResponsesGroup = next.find((item) => item.route === '/ach-responses' || item.label === 'Respuestas ACH');
        if (existingAchResponsesGroup) {
          const existingAchResponsesChildren = existingAchResponsesGroup.children ?? [];
          const missingAchResponsesChildren = achResponsesChildren.filter((child) => !hasRoute(existingAchResponsesChildren, child.route));
          if (missingAchResponsesChildren.length) {
            existingAchResponsesGroup.children = [...existingAchResponsesChildren, ...missingAchResponsesChildren];
          }
        }
      }
      if (!hasRoute(next, cenitGroup.route)) {
        next = [...next, cenitGroup];
      } else {
        const existingCenitGroup = next.find((item) => item.route === '/cenit' || item.label === 'CENIT');
        if (existingCenitGroup) {
          const existingCenitChildren = existingCenitGroup.children ?? [];
          const missingCenitChildren = cenitChildren.filter((child) => !hasRoute(existingCenitChildren, child.route));
          if (missingCenitChildren.length) {
            existingCenitGroup.children = [...existingCenitChildren, ...missingCenitChildren];
          }
        }
      }

      if (!hasRoute(next, nachaSecurityGroup.route)) {
        next = [...next, nachaSecurityGroup];
      }

      if (!hasRoute(next, nachaConfigGroup.route)) {
        next = [...next, nachaConfigGroup];
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
    const withAchResponses = hasRoute(withTransactions, achResponsesGroup.route)
      ? withTransactions
      : [...withTransactions, achResponsesGroup];
    const withCustomer = hasRoute(withAchResponses, customerItem.route) ? withAchResponses : [...withAchResponses, customerItem];
    const withReports = hasRoute(withCustomer, reportsItem.route) ? withCustomer : [...withCustomer, reportsItem];
    const withCenit = hasRoute(withReports, cenitGroup.route) ? withReports : [...withReports, cenitGroup];
    const withNachaConfig = hasRoute(withCenit, nachaConfigGroup.route) ? withCenit : [...withCenit, nachaConfigGroup];
    const withNachaSecurity = hasRoute(withNachaConfig, nachaSecurityGroup.route) ? withNachaConfig : [...withNachaConfig, nachaSecurityGroup];
    const withLogs = hasRoute(withNachaSecurity, '/navigation-logs') || hasRoute(withNachaSecurity, '/auth-logs') || hasRoute(withNachaSecurity, '/audit-logs')
      ? withNachaSecurity
      : [...withNachaSecurity, logsGroup];

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

  private removeLegacyNachaMenuItems(items: MenuItem[]): MenuItem[] {
    const legacyRoutes = new Set(['/ach-cycles/nacha/layouts', '/ach-cycles/nacha/definitions']);

    return items
      .filter((item) => !legacyRoutes.has(item.route))
      .map((item) => ({
        ...item,
        children: item.children?.length ? this.removeLegacyNachaMenuItems(item.children) : []
      }));
  }
}
