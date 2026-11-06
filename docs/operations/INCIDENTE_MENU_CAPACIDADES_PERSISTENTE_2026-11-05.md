# Corrección adicional: opción Capacidades de la plataforma visible después de Docker up

## Hallazgo

El seeder canónico anterior deja `/payment-rail-capability-registry` en `IsActive = false`,
pero depende de que se ejecute exitosamente el seed sobre la base de datos efectiva.
No existe evidencia del estado real de la instancia Docker del usuario, por lo que no se
atribuye el síntoma a una causa única.

La navegación tiene dos APIs distintas:
- `GET /api/navigation/menu`: menú lateral, solo muestra opciones activas.
- `GET /api/navigation/menu-items`: editor de organización, lista activas e inactivas.

## Alcance de esta corrección

1. Se conserva el seeder que desactiva los registros canónicos.
2. `GetMenuForCurrentUserHandler`: excluye la ruta retirada incluso si la BD histórica
   la mantiene activa (también variantes con mayúsculas o `/` final).
3. `NavigationService` Angular: aplica la misma exclusión al árbol visible, por seguridad
   durante despliegues mixtos.
4. Se incluyen pruebas regresivas unitarias en C# y Angular.
5. `scripts/sql/deactivate-payment-rail-capability-menu.sql`: corrección manual idempotente
   de filas exactas si el seed no se pudo ejecutar. No se aplica automáticamente.

El componente SPA y las APIs técnicas de capacidades siguen disponibles por URL autorizada.
El editor «Organización del menú» puede seguir mostrando el registro en estado **inactivo**:
esto es intencional por su función de administración.

## Despliegue sin borrar datos ni aplicar migraciones

Desde la carpeta raíz **de este ZIP descomprimido**:

```cmd
docker-ach.cmd down
docker-ach.cmd config
docker-ach.cmd infra
docker-ach.cmd up --seed --no-auto-migrations
docker-ach.cmd status
docker-ach.cmd logs
```

Verificar en logs `Database seed completed.`. Si no aparece, revisar errores anteriores.
Actualizar navegador con Ctrl+F5 o usar una pestaña privada para descartar caché.
Si aún aparece en el lateral, comprobar que los contenedores están construidos desde
la carpeta del proyecto correcto y que `/api/navigation/menu` pertenece a esa API.

La consulta `/api/navigation/menu-items` (administración) incluye inactivos por diseño.
No borrar registros ni volúmenes Docker para corregir esta opción.

## Limitaciones de verificación

No fue posible ejecutar pruebas dotnet ni Angular porque el entorno de preparación no
contiene el SDK .NET ni dependencias npm instaladas. Sí se inspeccionaron los cambios
estáticamente y se verificó la composición del ZIP.
