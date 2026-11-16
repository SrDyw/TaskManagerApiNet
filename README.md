# TaskManager API

MVP REST API for managing tasks, built with .NET 9, ASP.NET Core and EF Core on SQL Server.

## Stack

| Concern     | Library                                   |
|-------------|-------------------------------------------|
| Web API     | ASP.NET Core 9 (controllers)              |
| API docs    | Microsoft.AspNetCore.OpenApi + Swagger UI |
| Persistence | EF Core 9 + SQL Server                    |
| Mapping     | Mapster (DTO ⇄ model)                     |
| Validation  | FluentValidation (global action filter)   |
| Tests       | xUnit + WebApplicationFactory + EF InMemory |

## Task model

| Field         | Type                                 | Rules                                   |
|---------------|--------------------------------------|-----------------------------------------|
| `title`       | string                               | required, max 150                       |
| `description` | string?                              | optional, max 1000                      |
| `state`       | `Pending` \| `InProgress` \| `Done`  | defaults to `Pending` on create         |
| `assignee`    | string                               | required, max 100                       |
| `dueDate`     | DateTime (UTC)                       | required; must be in the future on create |

`state` is the `TaskState` enum. In the database it is an `int` column (`Pending = 0`, `InProgress = 1`, `Done = 2`) with a `CK_Tasks_State` check constraint. The JSON API accepts and returns the names (integers are also accepted on input).

`id`, `createdAt` and `updatedAt` are set by the server. Text fields are trimmed and all dates are stored and returned in UTC.

## Endpoints

| Method | Route             | Success | Errors   |
|--------|-------------------|---------|----------|
| GET    | `/api/tasks?state=&assignee=` | 200 | — |
| GET    | `/api/tasks/{id}` | 200     | 404      |
| POST   | `/api/tasks`      | 201     | 400      |
| PUT    | `/api/tasks/{id}` | 200     | 400, 404 |
| DELETE | `/api/tasks/{id}` | 204     | 404      |

Validation failures return a `ValidationProblemDetails` body with errors per field. See `src/TaskManager.Api/TaskManager.Api.http` for ready-to-run requests.

## Getting started

Requirements: .NET 9 SDK and SQL Server (LocalDB by default). Set the `DefaultConnection` connection string in `appsettings.json` to use another server.

```bash
dotnet tool restore                                  # installs dotnet-ef 9 locally
dotnet tool run dotnet-ef database update -p src/TaskManager.Api
dotnet run --project src/TaskManager.Api             # http://localhost:5152
```

In the `Development` environment, pending migrations are applied on startup, the OpenAPI document is served at `/openapi/v1.json` and Swagger UI at [`/swagger`](http://localhost:5152/swagger). Both launch profiles open the browser on `/swagger` when started from Visual Studio, Rider or `dotnet watch`.

## Tests

```bash
dotnet test
```

The integration tests boot the full API pipeline against an isolated in-memory database, so SQL Server is not needed to run them. They cover:

- **Create**: 201 with mapped/trimmed data; 400 for missing fields, past due date, overlong title or unknown state.
- **Update**: 200 with changes saved; 400 leaves the task unchanged; 404 for an unknown id.
- **Delete**: 204 then 404 on fetch; 404 for an unknown id or a second delete.
- **Mapping**: Mapster config compiles with every member mapped; DTO → model trims text and ignores server-set fields.
