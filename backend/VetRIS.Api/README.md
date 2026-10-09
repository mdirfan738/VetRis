# VetRIS — Web API

**14 C# files.** ASP.NET Core Web API on `net8.0` — no Razor, no MVC views.
Every controller is a thin translation from HTTP to a MediatR `Send()` call
(except `AuthController`/`UsersController`, which talk to Identity's
`UserManager` directly — see Auth model below for why); all the actual
business logic lives in Application/Domain from the earlier phases.

## Running it locally

1. A MySQL instance reachable at the connection string in
   `appsettings.Development.json` (defaults to `localhost:3306`,
   database `vetris_dev` — matching the docker-compose MySQL service in
   `mdirfan738/VetRis` if you point it at that).
2. `dotnet ef database update --project ../Infrastructure --startup-project .`
   from `src/Backend/API` (once you have the SDK — this sandbox couldn't
   run it).
3. `dotnet run` — Swagger UI is at `/swagger` in Development.
4. Create your first account: `POST /api/auth/login` needs a user to
   already exist, and `POST /api/users` needs an authenticated
   SuperAdmin/HospitalAdmin — chicken-and-egg for the very first account.
   Bootstrap it once via `UserManager` directly (a throwaway script, or
   Identity's tables) with the `SuperAdmin` role, then create everyone else
   through the API from there.

## Routes

| Controller | Base route | Notes |
|---|---|---|
| `AuthController` | `/api/auth` | `POST /login`, `POST /mfa/setup`, `POST /mfa/enable`, `POST /mfa/disable`, `POST /mfa/verify`, `POST /refresh`, `POST /revoke`, `GET /me` |
| `UsersController` | `/api/users` | admin-only account provisioning — create/list/activate/deactivate |
| `PacsController` | `/api/pacs` | `GET /verify` (C-ECHO), `GET /studies` (C-FIND), `POST /studies/{uid}/retrieve` (C-MOVE) |
| `StudiesController` | `/api/studies` | 15 endpoints — full workflow, worklist, comments |
| `ReportsController` | `/api/reports` | 10 endpoints — draft through amend |
| `InvoicesController` | `/api/invoices` | 8 endpoints — line items, GST, send, payments |
| `HospitalsController` / `ClinicsController` / `PatientsController` / `RadiologistsController` | `/api/{plural}` | create + get + list/search |
| `/health` | — | anonymous, unauthenticated liveness check |

## Auth model

JWT bearer, with real MFA and refresh-token rotation now (not the Web-API-phase
baseline):

- **Login → MFA challenge, if enabled.** `POST /login` checks the password;
  if the account has 2FA on, it returns a 5-minute `MfaChallengeToken`
  instead of an access token. That token has a *different* JWT audience
  (`VetRIS.MfaChallenge`) than real access tokens, so it's rejected
  automatically by every `[Authorize]` endpoint if someone tries to use it
  directly — only `POST /mfa/verify` (with the matching TOTP code) accepts
  it and issues the real tokens.
- **MFA is TOTP via ASP.NET Identity's built-in provider** — `POST
  /mfa/setup` returns a shared key + `otpauth://` URI to render as a QR
  code, `POST /mfa/enable` confirms it with a generated code before
  actually turning 2FA on.
- **Refresh tokens are opaque, DB-backed, and rotated on every use** — not
  JWTs. `POST /refresh` revokes the token it's given and issues a new one;
  a reused old token is a stolen-token signal. `POST /revoke` for logout.
- `ChangedBy`/`UserId`/`ReceivedByUserId` fields throughout the app are
  still populated from the access token's claims server-side, never
  trusted from the request body.
- `[Authorize(Roles = "...")]` per endpoint using `Domain.Enums.UserRoleType`
  role names, seeded into Identity at startup. These role assignments are a
  reasonable starting point, not a signed-off security review.

**Still not done:** self-service registration (deliberate — see
`UsersController`'s doc comment for why), SMS/email as a second MFA method
(TOTP only), and replacing role-string checks with policy-based
authorization for anything more nuanced than "has this role."

## Honest caveats

- Same as every other phase: no .NET SDK here, so nothing has been through
  `dotnet build`. The static check (brace balance, Finalize-collision,
  namespace-aware duplicate-type detection, internal `using` resolution)
  passed clean across all 158 files in the solution.
- `[FromQuery]` binding to a record type (`GetClinicsQuery`, `SearchPatientsQuery`,
  etc.) relies on ASP.NET Core's constructor-based complex-type binding,
  standard since .NET 7. If a specific query doesn't bind as expected on
  your first run, the safe fallback is converting that one action's
  parameter list to explicit `[FromQuery] Guid? x, [FromQuery] bool y, ...`
  — everything downstream of model binding is unaffected either way.
- `RegisterPaymentCommand`'s handler is the one place in Application that
  touches two aggregates (`Invoice` + `Payment`) in a single use case — see
  the comment on that handler for why.

## Integrating into `mdirfan738/VetRis`

That repo's `backend/VetRIS.Api` is intentionally minimal (health check +
one `Study` CRUD path + S3 upload) — a skeleton to prove the deploy
pipeline, not a competing implementation. The natural move once this is
ready: replace that project's contents with `Domain/`, `Application/`,
`Infrastructure/`, and `API/` from this solution, keep its
`Dockerfile`/`docker-compose.yml`/Terraform/GitHub Actions as-is (they
already target net8.0 + MySQL, which is exactly what this build now
matches), and update the `ConnectionStrings`/`Jwt` values to come from
whatever secrets mechanism that pipeline already uses.

## Suggested next phase

Per the master prompt's own ordering: Workflow Implementation (the
background services — Study Archive, Day End, Account Scheduler, File
Distribution, Study Writeback — as Hosted Services/Quartz jobs in
Infrastructure), then PACS Integration (fo-dicom C-STORE/C-FIND/C-MOVE/
C-ECHO) and AWS S3 Integration (the `IFileStorageService` abstraction
Application doesn't have yet, implemented against the bucket Terraform
already provisions in the real repo).
