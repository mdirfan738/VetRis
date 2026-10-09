# VetRIS — Infrastructure Layer (Phase 5)

**33 C# files**, targeting `net8.0` against **MySQL** (Pomelo EF Core
provider) — not `net9.0`/SQL Server as the original master prompt specified.
See `/PROGRESS.md` at the repo root for why.

## What's in here

```
src/Backend/Infrastructure/
├── VetRIS.Infrastructure.csproj
├── DependencyInjection.cs        AddInfrastructure() — the composition root called from Program.cs
├── Identity/
│   ├── ApplicationUser.cs        IdentityUser<Guid> + display fields
│   ├── ApplicationRole.cs        IdentityRole<Guid> + description
│   └── ICurrentUserService.cs    ambient "who's asking" — implemented by the Web API later
├── Persistence/
│   ├── VetRISDbContext.cs        IdentityDbContext<ApplicationUser, ApplicationRole, Guid> + all 24 DbSets
│   ├── VetRISDbContextDesignTimeFactory.cs   lets `dotnet ef migrations add` run without the full host
│   ├── Configurations/           one IEntityTypeConfiguration<T> per entity (24 files)
│   └── Events/DomainEventNotification.cs     wraps a Domain IDomainEvent for MediatR
└── Repositories/
    ├── Repository.cs             generic IRepository<T> implementation
    └── UnitOfWork.cs             wraps SaveChangesAsync
```

## What VetRISDbContext does on every SaveChangesAsync

1. **Audit stamping** — walks `ChangeTracker.Entries<BaseEntity>()` and calls
   `SetCreatedBy` / `MarkUpdated` for Added/Modified entries.
2. **Hard-delete → soft-delete** — a `Deleted` entry is flipped to `Modified`
   and `SoftDelete()` is called instead, so calling `.Remove()` by mistake
   can never actually lose a row.
3. **Soft-delete query filter** — applied to every `BaseEntity` type via one
   reflection loop in `OnModelCreating` rather than 24 repeated
   `HasQueryFilter` calls.
4. **Domain event dispatch** — after `base.SaveChangesAsync` succeeds (never
   before — a rolled-back transaction should never fire events), every
   `AggregateRoot`'s collected `IDomainEvent`s are wrapped in
   `DomainEventNotification<T>` and published through MediatR's `IPublisher`.
   Study/Report/Invoice/Payment already raise these; there just aren't any
   handlers subscribed yet — that's Application layer's job next.

## Table-name mapping

Most tables match their entity name directly. The few that don't, matching
the spec's own naming:

| Entity | Table | Why |
|---|---|---|
| `DicomImage` | `Images` | spec's literal table name |
| `StoredFile` | `Files` | spec's literal table name |
| `ScheduledJob` | `Scheduler` | job *definitions* |
| `JobExecutionLog` | `Jobs` | job *run history* — see Domain's README for the Jobs/Scheduler split |
| `MasterDataItem` | `MasterData` | spec's "Master Tables" |

## MySQL-specific choices

- `Money.Amount` columns get explicit `.HasPrecision(18, 2)` — MySQL (unlike
  SQL Server) doesn't assume a safe default for `decimal` and will warn/trim
  without it.
- Long text fields (`Findings`, `ClinicalHistory`, JSON blobs in `AuditLog`,
  etc.) use `HasColumnType("text")` rather than an unbounded `nvarchar(max)`.
- Enums are stored as their underlying `int` (EF Core's default — no
  `HasConversion<string>()`) since every enum in Domain already pins
  explicit numeric values, so reordering the C# declaration can't silently
  renumber existing rows.
- `Address`/`Money` are mapped as EF Core *owned types* (`OwnsOne`), each
  property getting an explicit `HasColumnName` so `Invoice`'s four `Money`
  properties (SubTotal/Tax/Total/AmountPaid) don't collide.

## Honest caveats

- Same as Domain: no .NET SDK or NuGet access in this sandbox, so nothing
  here has been through a real `dotnet build` or `dotnet ef migrations add`.
  The static check (brace/paren balance, duplicate types, internal `using`
  resolution) passed across all 90 files in the solution so far, but a real
  build is still the first thing to run locally.
- Collection navigations backed by a private field (`Study.Series`,
  `Invoice.LineItems`, etc.) rely on EF Core's default backing-field
  convention rather than an explicit `UsePropertyAccessMode` call in every
  configuration — this is standard, documented EF Core behavior, but it's
  the one piece of "convention over explicit code" in this layer worth
  double-checking on your first migration.
- `IRepository<T>` is constrained to `BaseEntity`, not `AggregateRoot` (see
  the comment in `Domain/Repositories/IRepository.cs`) — several entities
  that are really their own aggregate root (Hospital, Clinic, Patient, ...)
  just don't happen to raise domain events yet, so the stricter constraint
  would have excluded them unnecessarily.

## Since Application + API were built (Workflow Implementation phase)

`BackgroundJobs/` (5 Quartz.NET jobs + `BackgroundJobsRegistration.cs`)
implements the spec's 5 "workflow services" as scheduled jobs that reuse
the *same* Application commands the API calls — a job is just another
caller of `ArchiveStudyCommand`/`MarkWrittenBackCommand`/etc., so a study
archived by the nightly job and one archived by an admin clicking a button
in the UI go through identical validation and raise identical domain
events. Two (PACS writeback, S3 archival/file distribution) have an
explicit `TODO` where the not-yet-built PACS/S3 integration goes — the
*workflow* (finding candidates, transitioning status, logging, retrying
per-item on failure) is real today regardless.

| Spec's service | Job | Cadence | What's real vs. stubbed today |
|---|---|---|---|
| Study Archive Service | `StudyArchiveJob` | daily, 02:00 | **Fully real** — moves every DicomImage to S3 cold storage, then archives |
| Day End Service | `DayEndJob` | daily, 23:30 | Overdue-invoice marking + summary log real; "backup" is an RDS/Terraform concern, not code |
| Account Scheduler | `BackgroundJobsRegistration` + `MonthlyBillingSummaryJob` | the registration *is* the scheduler; monthly job is the non-daily cadence example | Fully real (aggregation + notification) |
| File Distribution Service | `FileDistributionJob` | every 15 min | Status transition real; email/FTP send stubbed |
| Study Writeback Service | `StudyWritebackJob` | every 10 min | **C-ECHO preflight real** (skips the run if PACS is unreachable); C-STORE of the report itself still stubbed — see caveat below |

All five: `[DisallowConcurrentExecution]` (no overlapping runs of the same
job), per-item try/catch (one failing study doesn't block the rest of the
batch), and `WaitForJobsToComplete = true` on shutdown so a job mid-`SaveChangesAsync`
isn't killed. Registered via `AddBackgroundJobs()`, called from the API's
`Program.cs` alongside `AddApplication()`/`AddInfrastructure()`.

## PACS Integration + AWS S3 Integration

Both landed as one phase since they're both "give a workflow-service job a
real external dependency instead of a TODO." `IPacsService` and
`IFileStorageService` live in `Application/Common/Interfaces` (not here) —
Infrastructure implements them (`Pacs/FoDicomPacsService.cs`,
`Storage/S3FileStorageService.cs`), and the two jobs above consume the
interfaces, never the concrete AWS SDK / fo-dicom types directly.

**AWS S3** (`AWSSDK.S3`): standard, very stable SDK — `PutObjectRequest`/
`GetObjectAsync`/`CopyObjectRequest`/`GetPreSignedUrlRequest` are the same
shapes used in virtually every .NET S3 integration. High confidence.
`AddDefaultAWSOptions` + `AddAWSService<IAmazonS3>` picks up credentials
from the environment/instance role automatically — nothing here reads an
access key from config, matching the spec's IAM-least-privilege ask.

**fo-dicom** (`FoDicomPacsService.cs`) is the one file in this entire
solution I'd flag differently from everything else built so far. Every
other integration in this codebase (EF Core, ASP.NET Core, MediatR,
AWSSDK.S3) uses extremely standardized, long-stable APIs I have high
confidence in verbatim. fo-dicom's client/request API has moved around
more across versions, and this file couldn't be checked against a real
compiler or your installed package version. The *shape* — one
`IPacsService`, four DIMSE methods, host/port/AE-title options — is solid;
treat specific calls (`DicomClientFactory.Create(...)`,
`DicomCFindRequest.CreateStudyQuery(...)`, the `OnResponseReceived`
callback signature) as "check against fo-dicom's own samples for your
installed version first" rather than "known correct." `PacsController`'s
three endpoints (`GET verify`, `GET studies`, `POST studies/{uid}/retrieve`)
give you something to actually run against a test SCP to find out quickly
which parts need adjusting.

## Suggested next phase

Report Module → Send to PACS: a DICOM Encapsulated PDF (or Structured
Report) builder that turns a signed `Report` into a valid DICOM object —
this is what `StudyWritebackJob`'s remaining `TODO` is waiting on, not
another PACS-connectivity concern. After that: Authentication &
Authorization (refresh tokens, MFA, registration flow — the JWT baseline
from the Web API phase is deliberately minimal), then Unit & Integration
Tests, then CI/CD.
