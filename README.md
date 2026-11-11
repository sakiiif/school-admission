# School Admission System

Online, first-come-first-served school admission. Guardians apply for a seat in a class (each shift is its own class). The seat is held for a limited time: paying the admission fee confirms the admission, otherwise the seat is released to others.

**Stack:** ASP.NET Core 10 (Clean Architecture) · EF Core · PostgreSQL 16 · React + TypeScript (Vite) · Docker Compose

---

## 1. Run the app with Docker

**Requirement:** Docker with Docker Compose. Nothing else needs to be installed.

From the repository root:

```bash
docker compose up --build
```

The first build takes a few minutes. This starts PostgreSQL, the backend and the frontend together. Wait until the backend log shows it is listening, then continue to step 2.

To stop: `docker compose down` (data is kept in a Docker volume).
To stop and delete all data: `docker compose down -v`.

## 2. Open the frontend

| What | URL |
|---|---|
| **Frontend (web app)** | **http://localhost:3000** |
| **Backend API** | **http://localhost:8080** |
| **Health check** | **http://localhost:8080/health** |

## 3. Run the tests

**Backend** (needs the .NET 10 SDK, and Docker running because the tests start their own temporary PostgreSQL container; the app does not need to be running):

```bash
cd backend
dotnet test
```

This including the concurrency test too: 100 simultaneous applications for a class with 10 seats must produce exactly 10 holds, and the held plus confirmed count is checked while the requests are running.

**Frontend** (needs Node.js 20 or newer):

```bash
cd frontend
npm install
npm test
```

Optional checks: `npm run lint` and `npm run build` (the build also type-checks the tests).

---

## Using the app

1. Enter a 17-digit **student registration number** in the box at the top. It is remembered in the browser.
2. **School admin:** at the bottom of the page, create a class with a name and a number of seats.
3. In the **Classes** table (refreshes every 3 seconds), click **Apply** to hold a seat.
4. In **My Applications**, watch the countdown. Click **Pay Admission Fee** to confirm (payment is simulated) or **Cancel** to release the seat.
5. When the countdown reaches zero, the application turns **Expired** by itself, Pay is disabled and the seat is available again.

To see an expiry quickly, use a short hold time:

```bash
# macOS / Linux / Git Bash
HOLD_DURATION_SECONDS=20 docker compose up -d --build
```

```powershell
# Windows PowerShell
$env:HOLD_DURATION_SECONDS = 20
docker compose up -d --build
```

## Configuration

Copy `.env.example` to `.env` to change a value. Compose reads it automatically.

| Variable | Default | Meaning |
|---|---|---|
| `HOLD_DURATION_SECONDS` | `600` | How long a seat is held. Must be a positive integer |
| `DB_CONNECTION_STRING` | set in `docker-compose.yml` | PostgreSQL connection string used by the backend |
| `VITE_API_URL` | `http://localhost:8080` | Backend address **as seen from the browser**. Built into the frontend, so after changing it run `docker compose up --build` |
| `FRONTEND_PORT` | `3000` | Host port for the web app |
| `DB_HOST_PORT` | `5432` | Host port for PostgreSQL |

## API

Base URL `http://localhost:8080`, JSON only. The student is identified by the `X-Student-Id` header (17 digits).

| # | Method and path | Header | Success | Errors |
|---|---|---|---|---|
| 1 | `POST /classes` | | 201 | 400 |
| 2 | `GET /classes` | | 200 | |
| 3 | `GET /classes/{classId}` | | 200 | 404 |
| 4 | `POST /classes/{classId}/holds` | `X-Student-Id` | 201 | 400, 404, 409 |
| 5 | `GET /holds/{holdId}` | | 200 | 404 |
| 6 | `POST /holds/{holdId}/confirm` | `X-Student-Id` | 200 | 400, 403, 404, 409 |
| 7 | `DELETE /holds/{holdId}` | `X-Student-Id` | 204 | 400, 403, 404, 409 |
| 8 | `GET /me/holds` | `X-Student-Id` | 200 | 400 |

Every error has the same shape:

```json
{ "error": "NO_SEATS_AVAILABLE", "message": "No seats are available in this class." }
```

## Project structure

```
backend/
  src/
    SchoolAdmission.Domain/          entities, hold rules, domain exceptions
    SchoolAdmission.Application/     use cases, DTOs, repository interface
    SchoolAdmission.Infrastructure/  EF Core, migrations, repository, expiry job
    SchoolAdmission.Api/             controllers, error middleware, configuration
  tests/SchoolAdmission.Tests/       integration and concurrency tests (real PostgreSQL)
frontend/                            React app, served by nginx in Docker
docker-compose.yml                   PostgreSQL + backend + frontend
```

Dependencies point inward: `Api → Infrastructure → Application → Domain`.

## How seats are kept correct

- Every apply, confirm and cancel runs in one database transaction that first locks the class row (`SELECT ... FOR UPDATE`), so requests for the same class take turns and held plus confirmed seats never exceed the total.
- Seat counts are calculated from the applications table each time, never stored.
- An application past its expiry time stops counting immediately, and a background job tidies its stored status. No API call is needed to release a seat.

## Common Troubleshooting

| Problem | Fix |
|---|---|
| `port is already allocated` | Something uses port 3000, 8080 or 5432. Stop it, or change `FRONTEND_PORT` / `DB_HOST_PORT` in `.env` |
| The page says it cannot reach the server | Wait a few seconds after `docker compose up` (database and migrations start first), then reload. Check `docker compose ps` and `docker compose logs backend` |
| Old version of the page after a rebuild | Hard refresh with Ctrl+F5 |
| Backend tests fail to start | Docker must be running |
