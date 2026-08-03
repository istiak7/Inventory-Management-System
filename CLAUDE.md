# Inventory Management System - Architecture Guide

## Stack
- .NET 10 Minimal API
- EF Core + PostgreSQL (Npgsql)
- MediatR (CQRS pattern)
- FluentValidation + ValidationBehavior pipeline
- JWT Authentication

## Project Structure

```
Features/
  <FeatureName>/
    Command/
      <ActionName>/
        <Action>Command.cs          # IRequest<Result>
        <Action>CommandHandler.cs   # IRequestHandler
        <Action>Validator.cs        # AbstractValidator
        <Action>Endpoint.cs         # IEndpoint (Minimal API)
    Query/
      <ActionName>/
        <Action>Query.cs            # IRequest<Result>
        <Action>QueryHandler.cs     # IRequestHandler
        <Action>Response.cs         # DTO (sealed record)
        <Action>Endpoint.cs         # IEndpoint (Minimal API)

Entities/
  BaseEntity.cs                     # Id, CreatedAt, UpdatedAt, IsActive
  <EntityName>.cs

Database/
  AppDbContext.cs
  Configurations/<Entity>Configuration.cs

Shared/
  IEndpoint.cs                      # Minimal API endpoint contract
  Result.cs                         # Unified response wrapper
  ValidationBehaivor.cs             # MediatR pipeline behavior
  JwtSettings.cs
  Repository/
    IBaseRepository.cs
    BaseRepository.cs
  Extensions/
    DependencyExtensions/           # Service registration extensions
    PaginationExtensions/
      EFCorePaginationExtensions.cs # .ToPagedResultAsync(pageNumber, pageSize)
      PageResult.cs                 # PagedResult<T> with metadata
      DapperPaginationExtension.cs

Middleware/
  ExceptionHandlingMiddleware.cs
```

## Conventions

### Result Wrapper
All handlers return `Result` from `Inventory_Management_System.Shared`:
```csharp
return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "...", Data = ... };
```

### Endpoint Registration
Endpoints are auto-discovered via reflection in `Program.cs`. Any class implementing `IEndpoint` is picked up automatically — no manual registration needed.

### Pagination (EF Core)
Use `AppDbContext` directly in query handlers (not `IBaseRepository`) to get `IQueryable<T>` for pagination:
```csharp
var pagedResult = await _dbContext.Suppliers
    .AsNoTracking()
    .OrderBy(s => s.Id)
    .Select(s => new SupplierResponse(...))
    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
```
Default: `pageNumber = 1`, `pageSize = 10`.

### Response DTOs
Use `sealed record` for query responses.

### Commands vs Queries
- Commands (write): use `IBaseRepository<T>` injected via constructor
- Queries (read/paginated): inject `AppDbContext` directly for `IQueryable` access

### Endpoint Tags
Group related endpoints with `.WithTags("FeatureName")` for Swagger grouping.

### Use Result Pattern
Use the `Result` pattern for consistent error handling across all API endpoints.