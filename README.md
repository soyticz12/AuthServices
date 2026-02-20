# AuthService

Authentication and authorization service for HRIS.

## Features

- **Multi-tenant login via `companyCode`**
  - Users belong to a company (`users.company_id`).
  - `companyCode` resolves the company before validating the user (prevents username collisions across companies).

- **JWT Authentication**
  - Access tokens + refresh tokens.
  - JWT contains `company_id`, `sub` (user id), role claim(s), etc.
  - Protected endpoints use `[Authorize]`.

- **Refresh Token Flow**
  - `POST /auth/refresh` issues a new access token using a refresh token.
  - Refresh tokens are stored in Postgres (`refresh_tokens` table).

- **User “Me” APIs**
  - `GET/PUT /me/profile`
  - `GET/PUT /me/preferences`
  - `PUT /me/photo`

- **Admin APIs (Role-based)**
  - `POST /admin/users` (Admin-only) create new users
  - (Optional) `POST /admin/security/unlock-login` unlock/reset login lockout for a user (Admin-only)

- **Login Abuse Protection (Redis)**
  - Per-user lockout stored in Redis (works across multiple API instances).
  - Progressive lockout supported (example):
    - 3 failed attempts → lock 15 minutes
    - next 3 failed attempts → lock 30 minutes
    - next 3 failed attempts → lock 45 minutes
  - Successful login clears attempts and resets escalation level.

- **HTTP Request Logging**
  - Logs: datetime, method, path, status, duration, IP, user-agent.

- **Consistent Auth Errors (401/403)**
  - JWT events return JSON `ProblemDetails` for:
    - missing/invalid token (401)
    - forbidden role/policy (403)

- **Swagger UI**
  - API explorer to test endpoints (Dev environment).

---

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core + PostgreSQL
- JWT Bearer Authentication
- Redis (login throttling / lockout)
- Docker Compose (local dev)

---

## Project Structure

- `Hris.AuthService.Api` — controllers, middleware, DI setup
- `Hris.AuthService.Application` — handlers/use-cases, abstractions (interfaces)
- `Hris.AuthService.Domain` — entities and domain logic
- `Hris.AuthService.Infrastructure` — EF Core persistence, repositories, security implementations, Redis implementations

---

## Quick Start (Docker)

1) Start everything:

```bash
docker compose up --build
