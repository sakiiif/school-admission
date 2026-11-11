# Assumptions

## Expiry
- **The database clock** is used for all expiry decisions, not the application's clock, so results are consistent no matter which process or machine serves the request.
## Concurrency
- Apply, confirm and cancel run in one transaction that locks the class row with `SELECT ... FOR UPDATE`. Confirm and cancel take the same lock, so a seat that expired and was given to someone else can never be confirmed afterwards.
- A partial unique index (one `HELD` or `CONFIRMED` row per student and class) is a last line of defense behind the application-level check.
- Seat counts are never stored. They are computed from the applications table.
- The Npgsql connection pool is capped at 40 per backend process, so queued requests wait inside the application instead of exhausting PostgreSQL's connection limit.

## Students and access
- **No authentication.** The brief says no login is required, so the `X-Student-Id` header is trusted as the identity, and class creation is open to anyone.
- The student ID must be **exactly 17 digits**. Anything else gets `400`. It is not checked against a student record.
- **A student may hold or confirm seats in several different classes.** The rule only limits one active application or confirmed admission per student *per class*, and each shift is its own class.
- After an application expires or is cancelled, the student **may apply again** to the same class.

## Confirm, cancel and payment
- Confirming is **idempotent**: repeating it returns `200` and never counts a seat twice.
- **Only the owner** can confirm or cancel (`403` otherwise).
- **A confirmed admission cannot be cancelled** (`409`), because assuming refunds are out of scope.

## API details
- `GET /holds/{id}` needs no header, as in the contract. It also returns `studentId`, which is an extra field.
- Class names must be non-empty and at most 200 characters. Seats must be a positive integer. Duplicate names are allowed.
- IDs are strings with a prefix (`c_...`, `h_...`). All timestamps are UTC ISO 8601.
- `GET /me/holds` lists the newest applications first.
- CORS allows any origin. No cookies or credentials are used.
- `GET /health` is an extra endpoint that also checks the database connection.

## Frontend
- The countdown uses the browser clock for display. The server stays authoritative: when a countdown ends the page asks the server for the real status, and the server rejects late payments. A wrong browser clock can only affect the display.
- Seat counts and applications refresh every 3 seconds.
- The student ID is kept in the browser's `localStorage`.
  
## Data
- PostgreSQL data lives in the named volume `pgdata`, so it survives `docker compose down` and `up`. Only `down -v` removes it.
- Database migrations are applied automatically when the backend starts, and the backend retries until the database is ready.
