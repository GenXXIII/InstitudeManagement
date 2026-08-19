# My Full-Stack Development Architecture & Technology Stack

## 1. Overview

My current development approach uses a modern full-stack architecture based on:

* **Next.js** for the frontend
* **TypeScript** for frontend development
* **Tailwind CSS** for UI styling
* **ASP.NET Core** for the backend
* **Clean Architecture** for backend organization
* **Monolithic architecture** for deployment and application structure
* **MediatR** for application request handling
* **Entity Framework Core** for data access
* **SQL Server** as the primary database
* **Redis** for caching
* **SignalR** for real-time communication
* **Docker / Docker Compose** for containerization

The goal is to keep the system structured, maintainable, scalable, and easy to deploy without introducing unnecessary complexity.

---

# 2. Overall Architecture

The overall system is structured into three major areas:

```text
┌─────────────────────────────────────────────┐
│                  Frontend                   │
│                                             │
│          Next.js + TypeScript               │
│             Tailwind CSS                   │
└──────────────────────┬──────────────────────┘
                       │
                  HTTP / REST
                       │
┌──────────────────────▼──────────────────────┐
│                  Backend                    │
│                                             │
│       ASP.NET Core Clean Architecture       │
│                Monolithic                   │
│                                             │
│                  MediatR                    │
└───────────────┬───────────────┬──────────────┘
                │               │
                ▼               ▼
        ┌──────────────┐   ┌──────────────┐
        │  SQL Server  │   │    Redis     │
        │   Database   │   │    Cache     │
        └──────────────┘   └──────────────┘
                │
                │
        ┌───────▼────────┐
        │    SignalR     │
        │ Real-time Data │
        └────────────────┘

              Docker
        ┌───────────────┐
        │  Containers   │
        └───────────────┘
```

---

# 3. Main Repository Structure

The main repository is separated into frontend, backend, infrastructure, and documentation.

```text
Project/
│
├── frontend/
│   └── web/
│
├── backend/
│   └── Solution.sln
│
├── infrastructure/
│   ├── docker/
│   └── docker-compose.yml
│
├── docs/
│
├── .env
├── .gitignore
└── README.md
```

## Main responsibilities

| Directory         | Responsibility                           |
| ----------------- | ---------------------------------------- |
| `frontend/`       | Next.js application                      |
| `backend/`        | ASP.NET Core application                 |
| `infrastructure/` | Docker and infrastructure configuration  |
| `docs/`           | Architecture and technical documentation |
| `.env`            | Environment-specific configuration       |

---

# 4. Frontend Architecture

The frontend uses **Next.js + TypeScript**.

The main frontend structure is:

```text
frontend/
└── web/
    │
    ├── app/
    ├── components/
    ├── features/
    ├── lib/
    ├── hooks/
    ├── providers/
    ├── types/
    ├── public/
    │
    ├── package.json
    ├── next.config.ts
    ├── tsconfig.json
    └── Dockerfile
```

## 4.1 `app/`

The `app` directory contains the Next.js application routes and pages.

```text
app/
├── layout.tsx
├── page.tsx
├── dashboard/
├── login/
└── ...
```

Its main responsibility is:

> Routing, pages, layouts, loading states, errors, and other Next.js App Router functionality.

The `app` directory should not become a place where all business logic is stored.

---

# 5. Frontend Components

Reusable UI components are stored separately.

```text
components/
├── ui/
├── layout/
├── forms/
├── tables/
└── charts/
```

Examples:

```text
Button
Input
Select
Modal
Dialog
Table
Pagination
Sidebar
Header
```

The purpose is to avoid repeatedly implementing the same UI.

---

# 6. Feature-Based Frontend Structure

Business functionality is organized into features.

```text
features/
│
├── feature-a/
│   ├── components/
│   ├── hooks/
│   ├── api.ts
│   ├── types.ts
│   └── validation.ts
│
├── feature-b/
│   ├── components/
│   ├── hooks/
│   ├── api.ts
│   ├── types.ts
│   └── validation.ts
│
└── feature-c/
```

This keeps related functionality together.

A feature can contain:

* UI components
* API functions
* TypeScript types
* Hooks
* Validation
* Feature-specific logic

This prevents the frontend from becoming one large collection of unrelated files.

---

# 7. Frontend API Communication

Frontend API communication is centralized.

```text
Next.js
   │
   ▼
Feature API
   │
   ▼
API Client
   │
   ▼
ASP.NET Core API
```

For example:

```text
features/
└── products/
    └── api.ts

        ↓

lib/
└── api/
    └── client.ts
```

The API client is responsible for common HTTP behavior such as:

* Base URL
* Headers
* Authentication
* Error handling
* Request configuration

Individual features should not duplicate this infrastructure.

---

# 8. Backend Architecture

The backend uses:

**ASP.NET Core + Clean Architecture + Monolithic Architecture**

The backend structure is:

```text
backend/
│
├── src/
│   │
│   ├── Project.API/
│   ├── Project.Application/
│   ├── Project.Domain/
│   └── Project.Infrastructure/
│
└── tests/
```

---

# 9. Domain Layer

The Domain layer contains the core business model.

```text
Project.Domain/
│
├── Entities/
├── Enums/
├── ValueObjects/
├── Events/
└── Common/
```

The Domain should contain business concepts and rules.

It should not depend on:

* ASP.NET Core
* SQL Server
* Redis
* Entity Framework implementation
* Controllers
* Next.js

The Domain is the most independent layer.

---

# 10. Application Layer

The Application layer contains application use cases.

```text
Project.Application/
│
├── Common/
├── Features/
│   ├── FeatureA/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── DTOs/
│   │   └── Validators/
│   │
│   └── FeatureB/
│
└── DependencyInjection.cs
```

This is where **MediatR** is primarily used.

---

# 11. MediatR

MediatR is used as the application's request-dispatching mechanism.

It is not treated as a traditional service layer.

The flow is:

```text
Controller
    │
    ▼
MediatR
    │
    ├── Command
    │      │
    │      ▼
    │    Handler
    │
    └── Query
           │
           ▼
         Handler
```

For example:

```text
CreateSomethingCommand
        ↓
CreateSomethingCommandHandler
        ↓
Domain / Repository
        ↓
Database
```

This separates the API entry point from the application's use cases.

---

# 12. Commands

Commands represent operations that change state.

Examples:

```text
Create
Update
Delete
Assign
Approve
Cancel
```

Structure:

```text
Commands/
└── CreateSomething/
    ├── CreateSomethingCommand.cs
    ├── CreateSomethingCommandHandler.cs
    └── CreateSomethingValidator.cs
```

---

# 13. Queries

Queries are used to retrieve data.

Examples:

```text
GetById
GetAll
Search
Filter
GetStatistics
```

Structure:

```text
Queries/
└── GetSomething/
    ├── GetSomethingQuery.cs
    └── GetSomethingQueryHandler.cs
```

Commands and queries are therefore clearly separated.

---

# 14. Infrastructure Layer

The Infrastructure layer contains implementations that communicate with external systems.

```text
Project.Infrastructure/
│
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   └── Migrations/
│
├── Repositories/
├── Caching/
├── Services/
└── DependencyInjection.cs
```

Infrastructure is responsible for things such as:

* Entity Framework Core
* SQL Server
* Redis
* External services
* Repository implementations
* Persistence

---

# 15. API Layer

The API layer is the entry point into the backend.

```text
Project.API/
│
├── Controllers/
├── Hubs/
├── Middleware/
├── Filters/
└── Program.cs
```

Its responsibilities include:

* HTTP endpoints
* Request/response handling
* Authentication/authorization integration
* Middleware
* SignalR hubs
* Dependency injection configuration

The controller should remain thin.

Preferred flow:

```text
HTTP Request
     ↓
Controller
     ↓
MediatR
     ↓
Command / Query
     ↓
Handler
     ↓
Domain / Infrastructure
     ↓
Database
```

---

# 16. Entity Framework Core

Entity Framework Core is used as the ORM between the application and SQL Server.

```text
Application
     ↓
Infrastructure
     ↓
EF Core
     ↓
SQL Server
```

EF Core handles:

* Entity mapping
* Database queries
* Relationships
* Transactions
* Migrations
* Persistence

Database configuration belongs in Infrastructure rather than Domain.

---

# 17. SQL Server

SQL Server is the primary persistent database.

```text
ASP.NET Core
      ↓
Entity Framework Core
      ↓
SQL Server
```

SQL Server stores the application's permanent data.

Redis is not used as a replacement for SQL Server.

---

# 18. Redis

Redis is used for caching.

The general flow is:

```text
Request
   ↓
Application
   ↓
Check Redis
   │
   ├── Cache Hit
   │      ↓
   │    Return
   │
   └── Cache Miss
          ↓
      SQL Server
          ↓
        Redis
          ↓
        Return
```

Redis is appropriate for frequently accessed data where caching provides a performance benefit.

Permanent business data remains in SQL Server.

---

# 19. SignalR

SignalR provides real-time communication.

Normal API communication:

```text
Next.js
   ↓
HTTP Request
   ↓
ASP.NET Core
   ↓
Response
```

Real-time communication:

```text
ASP.NET Core
      ↓
   SignalR Hub
      ↓
    Next.js
      ↓
    UI Update
```

SignalR can be used for:

* Live dashboard updates
* Notifications
* Status changes
* Real-time monitoring
* Other data that needs immediate UI updates

SignalR should be used where real-time communication provides value rather than replacing normal REST APIs.

---

# 20. Docker

Docker is used to containerize the application and supporting infrastructure.

A typical environment can contain:

```text
Docker Compose
│
├── Next.js
├── ASP.NET Core API
├── SQL Server
└── Redis
```

Depending on the environment, additional infrastructure can also be included.

The objective is to make the development and deployment environment consistent.

---

# 21. Docker Compose

Docker Compose manages multiple containers as one application environment.

Example architecture:

```text
docker-compose
│
├── frontend
│      └── Next.js
│
├── backend
│      └── ASP.NET Core
│
├── sqlserver
│      └── SQL Server
│
└── redis
       └── Redis
```

This allows the entire application environment to be started together.

---

# 22. Dependency Direction

One of the most important principles of the Clean Architecture implementation is dependency direction.

```text
        ┌──────────────┐
        │     API      │
        └──────┬───────┘
               │
               ▼
        ┌──────────────┐
        │ Application  │
        └──────┬───────┘
               │
               ▼
        ┌──────────────┐
        │    Domain    │
        └──────────────┘
               ▲
               │
        ┌──────┴───────┐
        │Infrastructure│
        └──────────────┘
```

The key principle is:

> **The inner layers should not depend on outer infrastructure details.**

The Domain remains independent.

---

# 23. Complete Request Flow

A typical request follows this architecture:

```text
                    USER
                      │
                      ▼
                  Next.js
                      │
                  HTTP/REST
                      │
                      ▼
             ASP.NET Core API
                      │
                      ▼
                 Controller
                      │
                      ▼
                  MediatR
                      │
               Command / Query
                      │
                      ▼
                   Handler
                      │
              ┌───────┴───────┐
              │               │
              ▼               ▼
           Domain        Infrastructure
                              │
                       ┌──────┴──────┐
                       │             │
                       ▼             ▼
                  SQL Server       Redis
```

For real-time updates:

```text
Backend
   │
   ▼
SignalR Hub
   │
   ▼
Next.js
   │
   ▼
UI updates
```

---

# 24. Main Technology Stack

| Category                                      | Technology            |
| --------------------------------------------- | --------------------- |
| Frontend Framework                            | Next.js               |
| Frontend Language                             | TypeScript            |
| Styling                                       | Tailwind CSS          |
| Backend Framework                             | ASP.NET Core          |
| Backend Language                              | C#                    |
| Architecture                                  | Clean Architecture    |
| Application Architecture                      | Monolith              |
| Request Handling                              | MediatR               |
| ORM                                           | Entity Framework Core |
| Database                                      | SQL Server            |
| Cache                                         | Redis                 |
| Real-Time Communication                       | SignalR               |
| Containerization                              | Docker                |
| Container Orchestration for Local Environment | Docker Compose        |
| Source Control                                | Git                   |
| Repository Hosting                            | GitHub                |
| API Style                                     | REST                  |
| API Documentation                             | OpenAPI / Swagger     |

---

# 25. Architectural Principles

My development approach follows these main principles:

### Separation of Concerns

Each layer has a specific responsibility.

### Clean Architecture

Business logic should not be tightly coupled to infrastructure.

### Feature-Based Organization

Related functionality should stay together.

### Thin Controllers

Controllers should handle HTTP concerns and delegate application work.

### MediatR-Based Use Cases

Commands and queries represent application operations.

### Database as Source of Truth

SQL Server stores permanent application data.

### Cache as an Optimization

Redis improves performance but does not replace the database.

### Real-Time Where Needed

SignalR is used for functionality that benefits from live updates.

### Containerized Environment

Docker provides a consistent development and deployment environment.

### Maintainability Over Complexity

Architecture should solve real problems without introducing unnecessary technologies.

---

# 26. Development Philosophy

The overall structure can be summarized as:

```text
Simple Main Architecture
        ↓
Clear Responsibilities
        ↓
Clean Backend Boundaries
        ↓
Feature-Based Frontend
        ↓
MediatR Use Cases
        ↓
SQL Server Persistence
        ↓
Redis Optimization
        ↓
SignalR Real-Time Features
        ↓
Docker Deployment
```

The objective is not to use as many technologies as possible.

The objective is to give each technology a **clear responsibility** within the system.
