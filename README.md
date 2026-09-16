# Inventory Management System API

A lightweight, clean, and high-performance backend API built with **.NET 10 Minimal APIs** using **Vertical Slice Architecture** and the **CQRS** pattern.

---

## Tech Stack

* **Framework:** .NET 10 (Minimal API)
* **Database:** PostgreSQL with Entity Framework Core (EF Core)
* **Design Pattern:** CQRS with MediatR & Vertical Slice Architecture
* **Validation:** FluentValidation (with MediatR Pipeline)
* **Authentication:** JWT (JSON Web Token)

---

## Project Architecture

This project follows **Vertical Slice Architecture**. Instead of grouping code by layers (Controllers, Services, Repositories), code is organized by **Features**. Each feature contains its own Commands, Queries, Validators, and Endpoints.

```text
Features/
  ├── <FeatureName>/
        ├── Command/
        │     └── <ActionName>/
        │           ├── <Action>Command.cs          # MediatR Request
        │           ├── <Action>CommandHandler.cs   # Business Logic
        │           ├── <Action>Validator.cs        # FluentValidation
        │           └── <Action>Endpoint.cs         # Minimal API Route
        └── Query/
              └── <ActionName>/
                    ├── <Action>Query.cs            # MediatR Request
                    ├── <Action>QueryHandler.cs     # Business Logic
                    ├── <Action>Response.cs         # Response DTO (sealed record)
                    └── <Action>Endpoint.cs         # Minimal API Route
