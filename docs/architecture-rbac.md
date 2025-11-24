# Arquitectura propuesta: SPA Angular + Backend .NET (Clean Architecture) con RBAC

Esta guía resume la estructura base para un proyecto listo para producción que combine un frontend Angular y un backend .NET 8 siguiendo Clean Architecture, con Control de Acceso Basado en Roles (RBAC) y claims de permisos.

## 1. Estructura de carpetas (backend)

```
backend/
├─ src/
│  ├─ Domain/
│  │  ├─ Entities/
│  │  │  ├─ Users/
│  │  │  │  ├─ User.cs
│  │  │  │  ├─ Role.cs
│  │  │  │  └─ Permission.cs
│  │  │  └─ Common/
│  │  │     └─ AuditableEntity.cs
│  │  ├─ ValueObjects/
│  │  └─ RepositoryContracts/
│  │     └─ IUnitOfWork.cs
│  ├─ Application/
│  │  ├─ Common/
│  │  │  ├─ Behaviors/ (validation, logging, performance)
│  │  │  └─ Interfaces/
│  │  │     ├─ ICurrentUserService.cs
│  │  │     └─ IPermissionService.cs
│  │  ├─ Security/
│  │  │  ├─ Commands/
│  │  │  │  ├─ Users/
│  │  │  │  ├─ Roles/
│  │  │  │  └─ RolePermissions/
│  │  │  └─ Queries/
│  │  │     ├─ Users/
│  │  │     ├─ Roles/
│  │  │     └─ MenuConfiguration/
│  │  ├─ DTOs/
│  │  └─ Mapping/
│  ├─ Infrastructure/
│  │  ├─ Identity/
│  │  │  ├─ ApplicationUser.cs
│  │  │  ├─ ApplicationRole.cs
│  │  │  └─ IdentityConfig.cs
│  │  ├─ Persistence/
│  │  │  ├─ ApplicationDbContext.cs
│  │  │  ├─ Configurations/ (EF Core)
│  │  │  └─ Migrations/
│  │  └─ Services/
│  └─ API/
│     ├─ Controllers/
│     │  └─ Security/
│     ├─ Filters/
│     ├─ Middleware/
│     └─ Program.cs
└─ tests/
   ├─ Unit/
   └─ Integration/
```

## 2. Entidades clave (Domain)

- `User`: hereda de `IdentityUser`, agrega campos de auditoría y navegación a roles.
- `Role`: hereda de `IdentityRole`, relación N:N con `Permission` mediante `RolePermission`.
- `Permission`: entidad simple con `Key` (e.g., `Users.Create`), `Description` y colección de roles.
- `RolePermission`: entidad de unión entre `Role` y `Permission`.

## 3. RBAC y seguridad

### 3.1 Configuración de Identity y JWT (API/Program.cs)

- Registrar Identity con roles (`AddIdentity<ApplicationUser, ApplicationRole>`).
- Añadir autenticación JWT (`AddAuthentication().AddJwtBearer(...)`).
- Incluir los permisos del usuario como claims en el token (claim type `"permission"`).

### 3.2 Políticas de autorización por permiso

Ejemplo de política que valida un claim de permiso específico:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("Users.Create", policy =>
        policy.RequireClaim("permission", "Users.Create"));
});
```

Uso en un controlador:

```csharp
[Authorize(Policy = "Users.Create")]
[HttpPost("api/security/users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    => Ok(await _mediator.Send(command));
```

### 3.3 CQRS con MediatR

- **Commands**: `CreateUserCommand`, `UpdateUserCommand`, `DeleteUserCommand`, `AssignPermissionsToRoleCommand`.
- **Queries**: `GetUsersQuery`, `GetRolesQuery`, `GetRolePermissionsQuery`, `GetMenuConfigurationQuery`.
- Handlers en `Application/Security/Commands/.../Handler.cs` y `Queries/.../Handler.cs`.

### 3.4 Persistencia (EF Core Code First)

- `ApplicationDbContext` expone `DbSet<User>`, `DbSet<Role>`, `DbSet<Permission>`, `DbSet<RolePermission>`.
- Configuraciones Fluent para N:N `RolePermission` y seeds iniciales de permisos/roles.

## 4. Endpoints mínimos (API)

- `POST /api/auth/login` → devuelve JWT con claims de permiso.
- `GET /api/security/users` / `POST` / `PUT` / `DELETE`.
- `GET /api/security/roles` / `POST` / `PUT` / `DELETE`.
- `POST /api/security/rolepermissions` → asigna lista de permisos a un rol.
- `GET /api/security/menuconfiguration` → devuelve menú parametrizable según permisos.

## 5. Frontend Angular

### 5.1 Estructura de módulos

```
web/ach-interbank-ui/src/app/
├─ core/
│  ├─ auth/
│  │  ├─ auth.module.ts
│  │  ├─ auth.service.ts
│  │  ├─ auth.interceptor.ts
│  │  └─ auth.guard.ts
│  ├─ permissions/
│  │  └─ permission.service.ts
│  ├─ layout/
│  │  ├─ dynamic-menu/
│  │  │  ├─ dynamic-menu.component.ts
│  │  │  └─ dynamic-menu.component.html
│  └─ models/ (DTOs compartidos)
├─ features/
│  ├─ users/
│  │  ├─ users.module.ts
│  │  ├─ user-list.component.ts
│  │  └─ user-form.component.ts
│  └─ roles/
│     ├─ roles.module.ts
│     └─ role-permissions.component.ts
└─ shared/
   ├─ components/
   └─ directives/
```

### 5.2 AuthService (esqueleto)

```ts
@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenKey = 'auth.token';
  private permissionsKey = 'auth.permissions';

  constructor(private http: HttpClient) {}

  login(credentials: { email: string; password: string }) {
    return this.http.post<LoginResponse>('/api/auth/login', credentials).pipe(
      tap(({ token, permissions }) => {
        localStorage.setItem(this.tokenKey, token);
        localStorage.setItem(this.permissionsKey, JSON.stringify(permissions));
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.permissionsKey);
  }
}
```

### 5.3 PermissionService

```ts
@Injectable({ providedIn: 'root' })
export class PermissionService {
  private permissions = new Set<string>();

  loadFromStorage() {
    const raw = localStorage.getItem('auth.permissions');
    this.permissions = new Set(raw ? JSON.parse(raw) : []);
  }

  hasPermission(key: string): boolean {
    return this.permissions.has(key);
  }
}
```

Uso en un botón:

```html
<button *ngIf="permissionService.hasPermission('Users.Create')">Crear usuario</button>
```

### 5.4 Interceptor JWT

```ts
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const token = this.auth.getToken();
    if (!token) return next.handle(req);
    return next.handle(
      req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    );
  }
}
```

### 5.5 Guard de rutas basado en permisos

```ts
@Injectable({ providedIn: 'root' })
export class PermissionGuard implements CanActivate {
  constructor(private permissions: PermissionService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const required = route.data['permission'] as string;
    const allowed = this.permissions.hasPermission(required);
    if (!allowed) {
      this.router.navigate(['/forbidden']);
    }
    return allowed;
  }
}
```

Uso en routing:

```ts
{
  path: 'admin/users',
  component: UserListComponent,
  canActivate: [PermissionGuard],
  data: { permission: 'Users.View' }
}
```

### 5.6 Menú dinámico

`dynamic-menu.component.ts`
```ts
@Component({
  selector: 'app-dynamic-menu',
  templateUrl: './dynamic-menu.component.html'
})
export class DynamicMenuComponent implements OnInit {
  menu: MenuItem[] = [];

  constructor(private http: HttpClient, private permissions: PermissionService) {}

  ngOnInit(): void {
    this.http.get<MenuItem[]>('/api/security/menuconfiguration').subscribe(items => {
      this.menu = items.filter(item =>
        !item.permission || this.permissions.hasPermission(item.permission)
      );
    });
  }
}
```

`dynamic-menu.component.html`
```html
<nav>
  <ul>
    <li *ngFor="let item of menu">
      <a [routerLink]="item.route">{{ item.label }}</a>
    </li>
  </ul>
</nav>
```

## 6. Estilo/UI

- Utilizar Tailwind o Angular Material: generar layout base (sidebar/topbar) con componentes reutilizables.
- Respetar principios responsivos (grid/flex) y accesibilidad (ARIA, contraste, navegación por teclado).

## 7. Pipeline de CI/CD (sugerido)

- Backend: `dotnet format`, `dotnet test`, `dotnet publish -c Release`.
- Frontend: `npm run lint`, `npm run test`, `npm run build --configuration production`.
- Seguridad: análisis SAST/secret scanning y revisión de dependencias (Dependabot/GitHub Advanced Security).

Esta estructura proporciona un esqueleto listo para extender permisos, menús y módulos sin romper el núcleo RBAC.
