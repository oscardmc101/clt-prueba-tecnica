# CLT - Prueba técnica Backend

API REST de usuarios, direcciones y conversión de divisas. Implementa los 13 endpoints del enunciado, carga masiva concurrente, seguridad por API Key y persistencia SQLite.

## Stack y requisitos

- SDK **.NET 10.0** estable, de cualquier banda 1xx/2xx/3xx/4xx o posterior de 10.0. `global.json` usa `10.0.100` como mínimo y `latestFeature`; los proyectos mantienen `net10.0`.
- ASP.NET Core Minimal API, EF Core 10, SQLite, FluentValidation, Swagger/OpenAPI y xUnit.
- Git y acceso a NuGet para la primera restauración. No requiere instalar un servidor de base de datos.

## Restaurar, crear la DB y ejecutar

Desde la raíz del repositorio:

```powershell
dotnet --version
dotnet restore
dotnet tool restore
dotnet ef database update --project src/Clt.Api
dotnet run --project src/Clt.Api --launch-profile http
```

API: `http://localhost:5143`. Swagger: **http://localhost:5143/swagger**.

La DB predeterminada es `src/Clt.Api/clt.db`. Las rutas relativas se resuelven contra la carpeta del proyecto API, también desde `dotnet ef`. Al iniciar se aplican las migraciones pendientes automáticamente; el comando manual anterior permite verificarlas antes del arranque. Se usa `MigrateAsync`, no `EnsureCreated`.

Para crear una migración después de modificar el modelo:

```powershell
dotnet ef migrations add NombreDelCambio --project src/Clt.Api --output-dir Infrastructure/Persistence/Migrations
dotnet ef database update --project src/Clt.Api
```

La migración inicial incluye índices únicos, FK con cascada y seed: **PYG = 1**, **USD = 7300**. Son tasas de ejemplo, sin actualización desde servicios externos.

## API Key y Swagger

Clave pública de prueba configurada en `appsettings.json`: **`clt-local-test-key`**.

```http
X-API-KEY: clt-local-test-key
```

Todos los endpoints funcionales requieren ese header; ausente o incorrecto devuelve `401`. En Swagger, pulsar **Authorize**, introducir la clave y ejecutar requests. La UI y el documento OpenAPI son públicos para facilitar la evaluación.

La configuración admite sobrescritura por variables de entorno: `ApiKey` y `ConnectionStrings__DefaultConnection`.

## Endpoints

| Método | Ruta | Éxito |
|---|---|---|
| POST | `/users` | 201, usuario y Location |
| GET | `/users` | 200, todos; filtro opcional `?isActive=true` o `false` |
| GET | `/users/{id}` | 200, usuario |
| PUT | `/users/{id}` | 200, usuario actualizado |
| DELETE | `/users/{id}` | 204 |
| POST | `/users/bulk` | 200, resumen y resultado por elemento |
| POST | `/users/{userId}/addresses` | 201, dirección |
| GET | `/users/{userId}/addresses` | 200, direcciones |
| PUT | `/addresses/{id}` | 200, dirección actualizada |
| DELETE | `/addresses/{id}` | 204 |
| GET | `/currencies` | 200, monedas |
| POST | `/currencies` | 201, moneda |
| POST | `/currency/convert` | 200, conversión |

Validación o JSON inválido: `400`; API Key inválida: `401`; recurso inexistente: `404`; email/código duplicado: `409`. Si se agota el timeout de bloqueo SQLite: `503`. Los errores generales usan Problem Details (`application/problem+json`), con errores por campo para FluentValidation. Las respuestas no exponen stack traces ni detalles internos.

## Ejemplos

PowerShell:

```powershell
$base = 'http://localhost:5143'
$headers = @{ 'X-API-KEY' = 'clt-local-test-key' }
Invoke-RestMethod "$base/users" -Method Post -Headers $headers -ContentType 'application/json' -Body '{"name":"Juan","email":"juan@test.com"}'
Invoke-RestMethod "$base/users/1" -Method Put -Headers $headers -ContentType 'application/json' -Body '{"name":"Juan Actualizado","email":"juan@test.com","isActive":false}'
Invoke-RestMethod "$base/users/1/addresses" -Method Post -Headers $headers -ContentType 'application/json' -Body '{"street":"Calle Falsa 123","city":"Asunción","country":"Paraguay","zipCode":"9999"}'
Invoke-RestMethod "$base/currencies" -Method Post -Headers $headers -ContentType 'application/json' -Body '{"code":"EUR","name":"Euro","rateToBase":8000}'
Invoke-RestMethod "$base/currency/convert" -Method Post -Headers $headers -ContentType 'application/json' -Body '{"fromCurrencyCode":"USD","toCurrencyCode":"PYG","amount":100}'
```

Usar en las rutas el `id` devuelto por la creación. La conversión de ejemplo devuelve:

```json
{"fromCurrency":"USD","toCurrency":"PYG","originalAmount":100,"convertedAmount":730000}
```

Enviar el archivo de prueba directamente, desde la raíz:

```powershell
curl.exe -X POST http://localhost:5143/users/bulk -H "X-API-KEY: clt-local-test-key" -H "Content-Type: application/json" --data-binary "@sample-data/bulk_users_test.json"
```

El body bulk es **`{"users":[...]}`**, igual al ejemplo oficial. Su respuesta contiene `total`, `createdCount`, `failedCount` y `results`. Cada resultado conserva `index` (base cero), `email`, `success`, `user` o `error` con `status`, `message` y errores de validación cuando corresponda. Los fallos de un elemento no revierten los éxitos. Repetir el archivo devuelve conflictos individuales por emails existentes. Un contenedor ausente, nulo o vacío devuelve `400`; elementos inválidos o nulos se reportan individualmente dentro del `200` del lote.

## Arquitectura y concurrencia

```text
src/Clt.Api/
  Api/                   Endpoints, middleware y Problem Details
  Domain/Entities/       User, Address, Currency
  Application/
    Users/{Commands,Queries}/
    Addresses/{Commands,Queries}/
    Currencies/{Commands,Queries}/
    CurrencyConversion/
    Common/
  Infrastructure/Persistence/   AppDbContext, factory de diseño y migraciones
tests/Clt.Api.Tests/      Integración SQLite, validators y concurrencia
sample-data/             JSON oficial de bulk
```

**CQRS simple:** Commands para escrituras, Queries para lecturas, cada uno con su Handler. Los endpoints delegan; los handlers validan y ejecutan el caso de uso. La conversión es Query aunque use HTTP POST. Una sola DB y llamadas directas por DI, sin MediatR ni repositorios genéricos.

**IDbContextFactory:** todos los accesos de aplicación crean y liberan un contexto por operación mediante `IDbContextFactory<AppDbContext>`. No se inyecta `AppDbContext` en handlers/endpoints ni se comparte una conexión entre tareas. `AppDbContextDesignFactory` se utiliza únicamente para las herramientas de migraciones.

**Task.WhenAll:** en bulk reúne las operaciones individuales de `CreateUserHandler`, cada una con su contexto; un `SemaphoreSlim(4)` por lote limita el trabajo activo. En conversión reúne las dos búsquedas de moneda con contextos distintos. Si ambos códigos coinciden se consulta una sola vez.

`Microsoft.Data.Sqlite` ejecuta I/O síncrona incluso en métodos async. Por eso ambos flujos programan explícitamente trabajo con `Task.Run`; no dependen de que un `await` ceda ejecución. Es una decisión acotada a este requisito con SQLite. WAL, timeout de 10 segundos y transacciones cortas reducen contención. SQLite permite un único escritor; la concurrencia de aplicación no implica escrituras físicas simultáneas ni garantiza acelerar cada lote.

## Tests y verificación

```powershell
dotnet restore
dotnet build
dotnet test
```

**54 tests**, con SQLite real temporal e independiente por instancia de API. Cubren seguridad de los 13 endpoints, CRUD, unicidad, cascada, seed/migración, hashing, validators, conversión y bulk parcial. Incluyen la conservación de `isActive` cuando se omite y su actualización explícita. Dos pruebas con una barrera en interceptores de EF comprueban solapamiento y diferentes `ContextId`; no se basan en medir velocidad.

Los warnings de compilación se tratan como errores.

## Decisiones y alcance

- Password opcional al crear, incluido bulk; si está presente no puede estar vacío. Solo se persiste `PasswordHash` generado con `PasswordHasher<User>` (PBKDF2 con salt); ningún DTO lo devuelve. PUT requiere nombre/email y conserva el hash. Si se omite `isActive`, conserva su valor actual; si se envía `true` o `false`, lo actualiza explícitamente.
- Email recortado y normalizado a minúsculas; códigos recortados y en mayúsculas. Los índices únicos SQLite usan `NOCASE`; son la garantía ante carreras de duplicados.
- Borrado físico de usuarios con cascada de direcciones. Usuario inexistente al crear/listar direcciones devuelve `404`; usuario sin direcciones devuelve `[]`.
- Importes y tasas usan `decimal`, sin conversión a `double`. Fórmula del PDF en aplicación, sin redondeo de negocio adicional; overflow devuelve `400`.
- Currency implementa listar, crear y convertir: las operaciones enumeradas en el PDF.

Posibles mejoras: paginación, límites de lote configurables, políticas de contraseña según producto, gestión/rotación de API Keys, observabilidad y una DB con múltiples escritores para cargas mayores.
