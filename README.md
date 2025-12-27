# Auth-Api
🔐 Auth API

A production-style Authentication & Authorization API built with ASP.NET Core.
The project focuses on JWT-based security, a clean internal structure (Controllers → Services → Repositories), custom middleware, and observability-first development.

---

🚀 Features
User registration & authentication
JWT token generation and validation
Unique email enforcement at database level
Repository & service-based design
Custom middleware pipeline:
Correlation ID
Request & response logging
Global exception handling
Metrics enrichment
Structured JSON logging with Serilog
EF Core migrations
Docker-based observability stack (Prometheus, Grafana, Loki/Promtail)
Swagger / OpenAPI documentation

---

🧱 Project Structure
AuthApi/
 ├── Controllers/                 // HTTP endpoints
 ├── Data/                        // DbContext & database configuration
 ├── DTOs/                        // Request / response contracts
 ├── Mappings/                    // AutoMapper profiles
 ├── Middleware/                  // Custom middleware components
 │   ├── CorrelationIdMiddleware.cs
 │   ├── ExceptionMiddleware.cs
 │   ├── MetricsEnricher.cs
 │   ├── MetricsMiddleware.cs
 │   ├── RequestLoggingMiddleware.cs
 │   └── ResponseWrapperMiddleware.cs
 │
 ├── Migrations/                  // EF Core migrations
 ├── Models/                      // Domain models
 │   ├── User.cs
 │   └── ApiResponse.cs
 │
 ├── Repositories/                // Data access layer
 │   ├── IUserRepository.cs
 │   └── UserRepository.cs
 │
 ├── Services/                    // Business logic
 ├── Program.cs
 └── appsettings.json

---

🧠 Architectural Approach
Controllers
Handle HTTP concerns only (routing, status codes, DTOs).
Services
Implement business logic such as authentication, validation, and token issuance.
Repositories
Abstract database access using Entity Framework Core.
Middleware
Handle cross-cutting concerns like logging, correlation IDs, metrics, and error handling.
Database
EF Core with migrations, including constraints such as unique email enforcement.

---

🛠 Tech Stack
C# / ASP.NET Core
Entity Framework Core
SQL Server
JWT Authentication
AutoMapper
Serilog (JSON console & rolling file logs)
Docker / Docker Compose
Prometheus
Grafana
Loki / Promtail
Swagger / OpenAPI

---

▶️ Run Locally
Prerequisites:
.NET SDK 8+
SQL Server (local or Docker)
Docker (recommended for observability)

Run the API
dotnet restore
dotnet ef database update
dotnet run --project AuthApi

Swagger UI:
https://localhost:<configured-port>/swagger

---

📈 Observability Stack
Start the observability stack:
docker compose -f docker-compose.observability.yml up -d

Includes:
Prometheus for metrics
Grafana for dashboards
Loki + Promtail for centralized logs

---

🔐 Configuration & Secrets
Sensitive configuration (JWT keys, connection strings) is not committed.
For local development:
Use appsettings.Development.json (gitignored)
Or environment variables

---

📎 Why This Project Exists
This project was built as part of my backend engineering portfolio to demonstrate:
Secure authentication design
Middleware-driven architecture
Observability-aware development
Clean separation of concerns
Production-oriented thinking in ASP.NET Core

---

⭐ Recruiter Note
This repository is one of my primary portfolio projects and is pinned on my GitHub profile alongside other production-style backend services.




