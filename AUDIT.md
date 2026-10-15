# Auditoría de cumplimiento

Fecha: 15/10/2026. Fuente: las cinco páginas de **Prueba Tecnica Desarrollador_26.pdf**, el checklist aprobado y los ajustes posteriores del usuario.

Los requisitos funcionales están completos. La publicación remota solicitada por el PDF queda aplazada por instrucción explícita del usuario. No se creó remoto ni se ejecutó push.

En las tablas, las rutas de implementación son relativas a `src/Clt.Api/`, salvo indicación contraria. Las clases de tests están en `tests/Clt.Api.Tests/`; sus nombres de método permiten localizar la evidencia.

## Modelo de datos

| REQUISITO | ESTADO | ARCHIVO / CLASE | TEST | OBSERVACIÓN |
|---|---|---|---|---|
| PDF p.1: User.Id int, PK, autoincrement | Completo | `Domain/Entities/User.cs`; migración `InitialCreate` | `UserTests.User_crud_filter_and_default_active_work_without_password` | Id generado al insertar. |
| PDF p.1: Name requerido | Completo | `CreateUserValidator`, `UpdateUserValidator`; `AppDbContext` | `ValidatorTests.Create_and_update_user_validate_required_fields` | También rechaza espacios en blanco. |
| PDF p.1: Email requerido, válido y único | Completo | Validators de User; `AppDbContext`; `DatabaseErrors` | `UserTests.Invalid_user_returns_validation_problem`, `Duplicate_email_is_conflict_even_with_different_case` | Índice único real, NOCASE y normalización; conflicto 409. |
| PDF p.1: IsActive bool, default true | Completo | `User`; `AppDbContext`; migración | `UserTests.User_crud_filter_and_default_active_work_without_password` | Inserción true; actualización false comprobada. |
| PDF p.1: Password hasheado; ajuste: opcional | Completo | `CreateUserHandler`; `PasswordHasher<User>` | `UserTests.Password_is_hashed_and_never_returned`; `ValidatorTests.User_without_password_is_valid_but_an_explicit_empty_password_is_not` | Solo columna nullable PasswordHash; no password plano. |
| PDF p.1: Address.Id PK, UserId obligatorio FK | Completo | `Address`; `AppDbContext`; `InitialCreate` | `PersistenceTests.Migration_seed_unique_indexes_and_cascade_work_on_real_sqlite` | FK no nullable; índice UserId. |
| PDF p.1 y p.3: User 1:N Addresses | Completo | `User.Addresses`, `Address.User`; `AppDbContext` | `AddressTests.Address_crud_and_user_cascade_work`; `PersistenceTests` | Cascade; Currency independiente. |
| PDF p.2: Street, City, Country requeridos | Completo | `CreateAddressValidator`, `UpdateAddressValidator`; `AppDbContext` | `ValidatorTests.Create_and_update_address_validate_required_fields` | Crear y editar. |
| PDF p.2: ZipCode opcional | Completo | `Address`; Commands de Address | `ValidatorTests.Zip_code_is_optional_on_create_and_update` | Nullable. |
| PDF p.2: Currency.Id int PK | Completo | `Currency`; `InitialCreate` | `CurrencyTests.Currencies_are_seeded_and_new_codes_are_normalized_and_unique` | Generado al insertar. |
| PDF p.2-3: Code requerido y único | Completo | `CreateCurrencyValidator`; `AppDbContext`; `DatabaseErrors` | `CurrencyTests.Invalid_currency_returns_400`, `Currencies_are_seeded_and_new_codes_are_normalized_and_unique` | Código normalizado; índice único NOCASE. |
| PDF p.2-3: Name requerido, RateToBase decimal > 0 | Completo | `Currency`; `CreateCurrencyValidator` | `ValidatorTests.Currency_requires_code_name_and_positive_rate`; `CurrencyTests.Invalid_currency_returns_400` | Decimal almacenado por el proveedor como TEXT; cálculo decimal en aplicación. |
| Ajuste: seed PYG=1 y USD=7300 | Completo | `AppDbContext.HasData`; `InitialCreate` | `PersistenceTests`; `CurrencyTests.Conversion_uses_rate_to_base` | Seed determinista versionado. |
| Campos adicionales opcionales del PDF | No necesarios | Entidades de dominio | Revisión del modelo | No se agregaron campos ajenos al alcance. |

## Endpoints y reglas

| REQUISITO | ESTADO | ARCHIVO / CLASE | TEST | OBSERVACIÓN |
|---|---|---|---|---|
| POST /users | Completo | `Api/Endpoints/UserEndpoints.cs`; `CreateUserHandler` | `UserTests.User_crud_filter_and_default_active_work_without_password` | 201 y Location; acepta el ejemplo oficial sin password. |
| GET /users | Completo | `ListUsersHandler` | `UserTests.User_crud_filter_and_default_active_work_without_password` | Devuelve todos. |
| GET /users filtro isActive opcional | Completo | `ListUsersQuery`; `ListUsersHandler` | Mismo test de CRUD | true y false comprobados. |
| GET /users/{id} | Completo | `GetUserHandler` | `UserTests.Missing_user_returns_404_on_read_update_delete` y CRUD | 200 o 404. |
| PUT /users/{id} | Completo | `UpdateUserHandler`, `UpdateUserValidator` | `UserTests` CRUD; `ErrorHandlingTests.Updating_to_an_existing_email_returns_409_and_does_not_change_user` | Name, email e isActive; conserva hash. |
| DELETE /users/{id} | Completo | `DeleteUserHandler` | `UserTests` CRUD e inexistentes; `AddressTests` cascada | 204 o 404. |
| POST /users/bulk | Completo | `BulkCreateUsersHandler`, `CreateUserHandler` | `BulkTests.Mixed_bulk_preserves_successes_and_reports_each_error_in_input_order` | Total, creados, fallidos y resultado/error por elemento. |
| Bulk: no abortar éxitos ante fallos individuales | Completo | `BulkCreateUsersHandler.ProcessAsync` | `BulkTests` lote mixto y repetición | Validación 400, duplicados 409, otros fallos sanitizados. |
| Bulk: body/JSON del ejemplo | Completo | Raíz: `sample-data/bulk_users_test.json` | `BulkTests.Sample_json_can_be_posted_directly_and_reports_duplicates_on_repeat` | Objeto con users; cuatro registros y acentos originales. |
| Bulk: contenedor validado | Completo | `BulkCreateUsersValidator` | `BulkTests.Invalid_container_returns_400`; `ValidatorTests` bulk | Contenedor nulo/ausente/vacío: 400; elementos inválidos no rechazan todo el lote. |
| POST /users/{userId}/addresses | Completo | `CreateAddressHandler`, `CreateAddressValidator` | `AddressTests.Address_crud_and_user_cascade_work` | 201; ZipCode opcional. |
| Crear dirección: UserId inexistente -> 404 | Completo | `CreateAddressHandler` | `AddressTests.Missing_parent_and_missing_address_return_404` | Verificación antes de persistir; FK real. |
| GET /users/{userId}/addresses | Completo | `ListAddressesHandler` | `AddressTests` CRUD e inexistentes | Usuario sin direcciones: []; usuario inexistente: 404. |
| PUT /addresses/{id} | Completo | `UpdateAddressHandler`, `UpdateAddressValidator` | `AddressTests` CRUD y validación | 200, 400 o 404. |
| DELETE /addresses/{id} | Completo | `DeleteAddressHandler` | `AddressTests` CRUD e inexistentes | 204 o 404. |
| GET /currencies | Completo | `ListCurrenciesHandler` | `CurrencyTests.Currencies_are_seeded_and_new_codes_are_normalized_and_unique` | Incluye seed. |
| POST /currencies | Completo | `CreateCurrencyHandler`, `CreateCurrencyValidator` | `CurrencyTests` válidas, inválidas y duplicadas | 201, 400, 409. |
| POST /currency/convert | Completo | `ConvertCurrencyQuery`, `ConvertCurrencyHandler` | `CurrencyTests.Conversion_uses_rate_to_base` | Ruta y nombres de respuesta exactos del PDF. |
| Conversión: amount > 0 | Completo | `ConvertCurrencyValidator` | `CurrencyTests.Non_positive_amount_is_invalid` | Cero y negativo -> 400. |
| Conversión: ambas monedas deben existir | Completo | `ConvertCurrencyHandler.FindCurrencyAsync` | `CurrencyTests.Missing_currency_returns_404` | Incluye el caso origen=destino inexistente. |
| Fórmula y ejemplo 100 USD -> 730000 PYG | Completo | `ConvertCurrencyHandler` | `CurrencyTests.Conversion_uses_rate_to_base` | También conversión inversa; sin redondeo adicional. |
| Respuesta fromCurrency/toCurrency/originalAmount/convertedAmount | Completo | `ConversionResponse` | `CurrencyTests.Conversion_uses_rate_to_base` | Verificado también desde Swagger UI. |
| Ajuste: overflow controlado | Completo | `ConvertCurrencyHandler` | `ErrorHandlingTests.Conversion_overflow_returns_400` | Mensaje público sin excepción interna. |

## Seguridad, arquitectura y concurrencia

| REQUISITO | ESTADO | ARCHIVO / CLASE | TEST | OBSERVACIÓN |
|---|---|---|---|---|
| .NET 10 y Minimal API | Completo | `Clt.Api.csproj`, `Program.cs`, `Api/Endpoints/`; raíz `global.json` | Toda la suite con WebApplicationFactory y arranque Kestrel | SDK comprobado: 10.0.402; net10.0. |
| Header X-API-KEY en toda la API funcional | Completo | `ApiKeyMiddleware` | `SecurityTests.Every_functional_endpoint_requires_api_key_before_processing_body` | Revisa los 13 endpoints con clave ausente e incorrecta. |
| API Key en appsettings.json | Completo | `appsettings.json`; `ApiKeyMiddleware` | `SecurityTests`, clientes autorizados de integración | Clave pública de evaluación; override por entorno. |
| API Key ausente/incorrecta -> 401 | Completo | `ApiKeyMiddleware` | `SecurityTests.Missing_or_incorrect_api_key_returns_401` | Problem Details y comparación de hashes en tiempo constante. |
| Swagger/OpenAPI y Authorize apiKey | Completo | `Program.cs` | `SecurityTests.Swagger_is_accessible_and_documents_api_key`; comprobación de navegador | Documento/UI públicos según ajuste del usuario. |
| EF Core + SQLite | Completo | `Clt.Api.csproj`; `PersistenceRegistration`; `AppDbContext` | `PersistenceTests`; toda integración | Base real; sin proveedor InMemory. |
| DbSet User/Address/Currency | Completo | `AppDbContext` | `PersistenceTests`; CRUD y CurrencyTests | Tres DbSet presentes. |
| Registrar IDbContextFactory<AppDbContext> | Completo | `PersistenceRegistration.AddPersistence` | `PersistenceTests`; `ConcurrentOperationsTests` | AddDbContextFactory. |
| No inyectar contexto scoped directamente | Completo | Todos los handlers y endpoints | Revisión estática + pruebas concurrentes | Búsqueda de inyección directa: cero coincidencias. |
| CQRS Commands/Queries/Handlers | Completo | `Application/` | Tests de integración por funcionalidad | Handlers explícitos registrados por DI. Conversión es Query. |
| Carpetas mínimas del PDF y Currencies/Queries adicional | Completo | `Application/Users/{Commands,Queries}`, `Addresses/{Commands,Queries}`, `Currencies/{Commands,Queries}`, `CurrencyConversion` | Revisión de estructura | Estructura aprobada conservada. |
| FluentValidation crear/editar User | Completo | `CreateUserValidator`, `UpdateUserValidator` | `ValidatorTests`; `UserTests` | Validación ejecutada por los handlers. |
| FluentValidation crear/editar Address | Completo | `CreateAddressValidator`, `UpdateAddressValidator` | `ValidatorTests`; `AddressTests` | Tres campos obligatorios y ZipCode opcional. |
| FluentValidation crear Currency | Completo | `CreateCurrencyValidator` | `ValidatorTests`; `CurrencyTests` | Unicidad garantizada por índice y traducida a 409. |
| FluentValidation conversión y bulk | Completo | `ConvertCurrencyValidator`, `BulkCreateUsersValidator` | `ValidatorTests`; `BulkTests`; `CurrencyTests` | Elementos bulk reutilizan CreateUserValidator. |
| Paralelismo obligatorio real con Task.WhenAll | Completo | `BulkCreateUsersHandler` | `ConcurrentOperationsTests.Bulk_operations_overlap_and_every_insert_uses_a_different_context` | Barrera confirma solapamiento; ocho inserts con ocho contextos. |
| Bulk: contexto independiente por operación | Completo | Cada invocación de `CreateUserHandler` usa factory y await using | Mismo test de bulk concurrente | Sin DbContext ni conexión compartidos. |
| Conversión: búsquedas concurrentes independientes | Completo | `ConvertCurrencyHandler` | `ConcurrentOperationsTests.Conversion_reads_overlap_and_use_two_different_contexts` | Dos tareas y dos ContextId; una consulta si códigos coinciden. |
| SQLite estable con concurrencia sencilla | Completo | SemaphoreSlim(4), `PersistenceRegistration`, `BulkCreateUsersHandler` | Integración bulk de ocho inserts y lote mixto | WAL, timeout finito 10s, transacciones por usuario; un escritor físico de SQLite. |
| Manejo consistente de errores/códigos HTTP | Completo | `ApiExceptionHandler`, `ApiException`, `DatabaseErrors`, endpoints | `ErrorHandlingTests`, `SecurityTests`, integración | 200/201/204/400/401/404/409; 503 para bloqueo agotado. |
| No devolver hash ni password | Completo | `UserResponse` y proyecciones; `BulkUserResult` | `UserTests.Password_is_hashed_and_never_returned`, `BulkTests`, `SecurityTests` Swagger | Ningún DTO contiene PasswordHash; ningún endpoint serializa User. |
| Simplicidad, sin infraestructura adicional | Completo | Solución API + tests | Revisión de csproj y estructura | Sin MediatR, AutoMapper, repositorios genéricos, microservicios, Kafka, Redis ni Docker. |

## Entrega y verificaciones finales

| REQUISITO | ESTADO | ARCHIVO / CLASE | TEST | OBSERVACIÓN |
|---|---|---|---|---|
| Migración inicial versionada | Completo | `Infrastructure/Persistence/Migrations/20261015151455_InitialCreate*`; snapshot | `PersistenceTests`; `dotnet ef migrations has-pending-model-changes` | Aplicada por CLI y al arrancar; sin cambios de modelo pendientes. |
| JSON utilizable contra bulk | Completo | Raíz `sample-data/bulk_users_test.json` | `BulkTests` sample + comprobación HTTP real | Cuatro éxitos sobre DB nueva; repetición informa cuatro conflictos. |
| Proyecto xUnit y tests útiles | Completo | `tests/Clt.Api.Tests/` | 50 casos aprobados | Validators, seguridad, persistencia, flujos y concurrencia. |
| README versión/requisitos/restore/DB/ejecución/Swagger | Completo | Raíz `README.md`; `dotnet-tools.json` | Revisión documental y ejecución de comandos equivalentes | URL local 5143; herramienta EF local versionada. |
| README clave/header/endpoints/requests/tests | Completo | Raíz `README.md` | Revisión documental; HTTP/Swagger | Clave y ejemplo bulk directamente utilizables. |
| README CQRS/factory/paralelismo/decisiones/alcance/mejoras | Completo | Raíz `README.md` | Revisión documental | Incluye limitación de un escritor y motivos de Task.Run. |
| .gitignore y limpieza de archivos publicables | Completo | Raíz `.gitignore`, `.gitattributes` | Revisión del conjunto versionado y no ignorado | DB, bin, obj, .local, logs y resultados de tests excluidos. |
| Proyecto compila y levanta | Completo | Solución `CltTechnicalTest.sln`; `Program.cs` | Build final y arranque HTTP real | Cero warnings y errores de build. |
| Restore/build/test después de fases importantes | Completo | Historial de ejecución y commits locales | Validación ejecutada en fases 1-8 | Fallos intermedios corregidos antes de avanzar. |
| Código en Git y commits por fases | Completo localmente | Historial Git | `git log --oneline`, revisión de estado | Commits coherentes, sin publicar. |
| PDF: subir repositorio remoto | Aplazado por el usuario | Sin remoto configurado | `git remote -v` vacío | No crear remoto ni hacer push hasta revisión local. |
| Secretos/credenciales accidentales | Revisado | Archivos publicables e historial Git | Escaneo por patrones y revisión manual de configuración/passwords | Sin tokens privados ni claves privadas detectados. Clave de prueba y passwords ficticios intencionales. |
| Warnings, nullable, código muerto y nombres | Revisado | `Directory.Build.props`; fuentes y tests | Build con TreatWarningsAsErrors; búsquedas estáticas | Cero warnings/nullable; sin TODO/FIXME/NotImplementedException/plantilla sobrante. |

## Evidencia

- `dotnet --version`: **10.0.402**. `git --version`: **2.39.1.windows.1**.
- Validación final: `dotnet restore`, `dotnet build`, `dotnet test`. **50 ejecutados, 50 aprobados, 0 fallidos, 0 omitidos; 0 warnings y 0 errores de build**.
- NuGet: `dotnet list CltTechnicalTest.sln package --vulnerable --include-transitive --no-restore`: ambos proyectos sin paquetes vulnerables reportados por los orígenes consultados.
- EF: `dotnet ef migrations has-pending-model-changes --project src/Clt.Api --no-build`: **No changes have been made to the model since the last migration.**
- Kestrel: **Now listening on: http://localhost:5143** y **Application started**; DB de verificación separada del archivo predeterminado.
- HTTP real: **20 comprobaciones** correctas que recorren los 13 endpoints y errores 400/401/404/409; Swagger contiene 13 operaciones; sample bulk crea cuatro usuarios.
- Swagger UI: abrió, aceptó Authorize, envió X-API-KEY y devolvió **200** con **convertedAmount = 730000**; consola de la página sin errores.
- Inspección directa del SQLite de verificación: índices únicos User.Email y Currency.Code, FK con ON DELETE CASCADE, WAL, sin violaciones de foreign keys; tabla Users tiene PasswordHash y no tiene columna Password.
- Los artefactos de verificación local se excluyen de Git. El servidor usado para verificar se detuvo al terminar; ejecutar con las instrucciones del README.

Pendientes funcionales: ninguno dentro del alcance solicitado. Publicación remota: deliberadamente pendiente de aprobación posterior del usuario.
