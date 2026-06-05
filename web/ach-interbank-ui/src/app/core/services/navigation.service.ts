import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
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
      { id: -2034, label: 'Reglas por cámara', route: '/transactions/clearing-house-rules', icon: 'rule' },
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

    const nachaConfigChildren: MenuItem[] = [
      { id: -2801, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles', icon: 'fact_check' },
      { id: -2802, label: 'Registros oficiales', route: '/nacha-config-admin/records', icon: 'view_list' },
      { id: -2803, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields', icon: 'schema' }
    ];

    const nachaConfigGroup: MenuItem = {
      id: -280,
      label: 'Configuración NACHA-M',
      route: '/nacha-config-admin/perfiles',
      icon: 'tune',
      children: nachaConfigChildren
    };

    const soapUatConsoleGroup: MenuItem = {
      id: -290,
      label: 'SOAP UAT Console',
      route: '/ach/nacha/soap-uat-console',
      icon: 'fact_check',
      children: [{ id: -2901, label: 'SOAP UAT Console', route: '/ach/nacha/soap-uat-console', icon: 'fact_check' }]
    };

    const reconciliationGroup: MenuItem = {
      id: -291,
      label: 'Conciliación ACH',
      route: '/ach/reconciliation',
      icon: 'fact_check',
      children: [{ id: -2911, label: 'Conciliación ACH', route: '/ach/reconciliation', icon: 'fact_check' }]
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
      { id: -2704, label: 'Panel operativo', route: '/ach-responses/dashboard', icon: 'dashboard' }
    ];

    const achResponsesGroup: MenuItem = {
      id: -270,
      label: 'Respuestas ACH',
      route: '/ach-responses',
      icon: 'assignment',
      children: achResponsesChildren
    };

    if (!items.length) {
      return [
        transactionsGroup,
        achResponsesGroup,
        customerItem,
        reportsItem,
        cenitGroup,
        nachaConfigGroup,
        soapUatConsoleGroup,
        reconciliationGroup,
        nachaSecurityGroup,
        logsGroup,
        catalogGroup
      ];
    }

    const hasRoute = (menu: MenuItem[], route: string): boolean =>
      menu.some((item) => item.route === route || (item.children?.length && hasRoute(item.children, route)));

    const mergeChildren = (menu: MenuItem[] | undefined, children: MenuItem[]): MenuItem[] => {
      const existing = menu ?? [];
      const missing = children.filter((child) => !hasRoute(existing, child.route));
      return missing.length ? [...existing, ...missing] : existing;
    };

    const existingCatalogGroup = items.find((item) => item.route === '/catalogs' || item.label === 'Catálogos');
    if (existingCatalogGroup) {
      existingCatalogGroup.children = mergeChildren(existingCatalogGroup.children, catalogChildren);
    }

    const existingTransactionsGroup = items.find((item) => item.route === '/transactions' || item.label === 'Transacciones');
    if (existingTransactionsGroup) {
      existingTransactionsGroup.children = mergeChildren(existingTransactionsGroup.children, transactionsChildren);
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
        existingAchResponsesGroup.children = mergeChildren(existingAchResponsesGroup.children, achResponsesChildren);
      }
    }
    if (!hasRoute(next, cenitGroup.route)) {
      next = [...next, cenitGroup];
    } else {
      const existingCenitGroup = next.find((item) => item.route === '/cenit' || item.label === 'CENIT');
      if (existingCenitGroup) {
        existingCenitGroup.children = mergeChildren(existingCenitGroup.children, cenitChildren);
      }
    }

    if (!hasRoute(next, nachaSecurityGroup.route)) {
      next = [...next, nachaSecurityGroup];
    } else {
      const existingNachaSecurityGroup = next.find((item) => item.route === '/nacha-security/dashboard' || item.label === 'Seguridad NACHA');
      if (existingNachaSecurityGroup) {
        existingNachaSecurityGroup.children = mergeChildren(existingNachaSecurityGroup.children, nachaSecurityChildren);
      }
    }

    if (!hasRoute(next, nachaConfigGroup.route)) {
      next = [...next, nachaConfigGroup];
    } else {
      const existingNachaConfigGroup = next.find(
        (item) =>
          item.route === '/nacha-config-admin/perfiles' ||
          item.label === 'Configuración NACHA-M' ||
          item.label === 'NACHA-M Configuración'
      );
      if (existingNachaConfigGroup) {
        existingNachaConfigGroup.label = 'Configuración NACHA-M';
        existingNachaConfigGroup.children = mergeChildren(existingNachaConfigGroup.children, nachaConfigChildren);
      }
    }

    if (!hasRoute(next, soapUatConsoleGroup.route)) {
      next = [...next, soapUatConsoleGroup];
    }

    if (!hasRoute(next, reconciliationGroup.route)) {
      next = [...next, reconciliationGroup];
    }

    const existingLogsGroup = next.find((item) => item.route === '/audit-logs' || item.label === 'Logs');
    if (existingLogsGroup) {
      existingLogsGroup.children = mergeChildren(existingLogsGroup.children, logsChildren);
    } else {
      next = [...next, logsGroup];
    }

    return [...next, catalogGroup];
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
    const legacyRoutes = new Set([
      '/ach-cycles/nacha/layouts',
      '/ach-cycles/nacha/definitions',
      '/nacha-layouts',
      '/nacha-record-definitions'
    ]);

    return items
      .filter((item) => !legacyRoutes.has(item.route))
      .map((item) => ({
        ...item,
        children: item.children?.length ? this.removeLegacyNachaMenuItems(item.children) : []
      }));
  }
}
