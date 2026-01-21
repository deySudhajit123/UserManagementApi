# User Management API

## Features (Rubric Mapping)

- GitHub repository created for this project.
- CRUD endpoints:
  - GET /api/users
  - GET /api/users/{id}
  - POST /api/users
  - PUT /api/users/{id}
  - DELETE /api/users/{id}
- Validation:
  - DataAnnotations on DTOs
  - Unique email constraint (returns 409 Conflict)
- Middleware:
  - Request logging middleware
  - API Key authentication middleware (header: X-API-KEY)
- Copilot debugging:
  - Used GitHub Copilot to diagnose and fix endpoint issues and validation behavior.
  - See commit history for “Copilot-assisted debugging” commits.

## How to Run

```bash
dotnet run
```
