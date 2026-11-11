# AI usage

## Tools
- **Claude** free version mostly.
- **ChatGPT** free version.

## What I used it for
- Discussing my plans, project requirements, figuring out the best possible work flow.
- Drafting the backend layers, the React UI and their tests.
- Explaining the best possible design choices to me, cross checking the references.

## How I checked the AI's output
- Ran the backend's concurrency and rule tests against a real PostgreSQL container (not an in-memory database, which ignores row locks).
- Tested every endpoint by hand with curl, including the error cases, and watched an application expire with a 20-second hold time.
- Exercised the full workflow in the browser, including stopping the backend to check the error messages.
- Ran lint, type-check and the frontend tests.

## Mistakes the AI made, and how they were caught

1. **Seat check and insert were not protected.** The first version counted seats and then inserted, so two requests could take the last seat. I moved everything into one transaction that locks the class row (`SELECT ... FOR UPDATE`) and proved it with the 100-requests / 10-seats test.
2. **Expiry used the app clock.** I changed all expiry decisions to the database clock so results don't depend on which process serves the request.
3. **Startup race in Docker.** The backend could start before Postgres accepted connections. I added a healthcheck, `depends_on`, and migration retries.
4. **Connection pool exhaustion under the burst test.** I capped the pool size so requests queue in the app instead of overloading Postgres.
