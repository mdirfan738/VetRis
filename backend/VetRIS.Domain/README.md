# VetRIS — Domain Layer (Phase 4)

Continues from the previously delivered solution architecture, folder
structure, and SQL schema. This phase adds the Domain project: **55 C#
files**, zero external dependencies, targeting `net8.0`.

> **Stack note:** originally targeted `net9.0` per the master prompt; retargeted
> to `net8.0` once Infrastructure needed to match the already-deployed AWS
> scaffold (`mdirfan738/VetRis`, .NET 8 + MySQL). See `/PROGRESS.md` at the repo
> root for the full reasoning. Nothing else in this layer changed — it has no
> external dependencies, so the retarget was a one-line csproj edit.

## What's in here

```
src/Backend/Domain/
├── VetRIS.Domain.csproj
├── Common/            BaseEntity, AggregateRoot, ValueObject, IDomainEvent, DomainEventBase
├── Exceptions/         DomainException + 3 specific subtypes (status-transition guards, not-found)
├── Enums/              15 enums — roles, statuses, modality, priority, job/notification/file/audit types
├── ValueObjects/        Address, Money (currency-safe arithmetic)
├── Events/              5 domain events raised by the aggregates below
└── Entities/
    ├── Organizations/   Hospital, Clinic
    ├── Clinical/         Patient, Radiologist, Study*, Series, DicomImage, StudyStatusHistory, StudyComment
    ├── Reporting/        ReportTemplate, Report*, ReportVersion
    ├── Billing/          Invoice*, InvoiceLineItem, Payment*
    ├── Ops/              Notification, AuditLog, ActivityLog, SystemLog, StoredFile, ScheduledJob, JobExecutionLog
    └── Admin/            SystemConfiguration, MasterDataItem

  (* = aggregate root, raises domain events)
```

`Study` and `Report` are the two entities with real behavior, not just
properties — they enforce the exact status state machines from the spec
(invalid transitions throw `InvalidStudyStatusTransitionException` /
`InvalidReportStatusTransitionException`) and record every transition to
`StudyStatusHistory` / `ReportVersion` automatically. `Invoice` computes
GST, tracks partial payments, and derives `Outstanding()`.

## Mapping decisions from the spec's table list

The spec's Database Design section lists tables slightly looser than a
1:1 class map; here's how each one landed and why:

- **Users, Roles** → not Domain entities. Login, password hashes, and MFA
  belong to ASP.NET Identity, which is an Infrastructure concern in Clean
  Architecture. `UserRoleType` (enum) gives Domain/Application logic a way
  to reason about the fixed role set without referencing Identity.
- **Radiologists** → its own `Radiologist` entity (license, specialization,
  signature, workload cap), linked by `ApplicationUserId`, since it carries
  real business data beyond what Identity stores.
- **Images** → `DicomImage` (one row per DICOM SOP instance) — named for
  clarity since "Image" collides conceptually with generic framework types.
- **Logs / Audit / Activity Logs** → split into three distinct entities
  since they answer different questions: `SystemLog` (technical/error
  events, mirrors Serilog output), `AuditLog` (before/after values on data
  changes), `ActivityLog` (user-facing actions like logins and exports).
- **Files** → single `StoredFile` entity for anything in S3/local storage
  (DICOM archives, PDFs, invoices, attachments, backups), categorized by
  `FileCategory`. Avoids a redundant separate "Attachment" concept.
- **Jobs, Scheduler** → `ScheduledJob` (the definition/config: frequency,
  cron expression, enabled flag) plus `JobExecutionLog` (one row per run),
  matching how Hangfire/Quartz job history is normally modeled.
- **Configurations, Settings** → consolidated into one `SystemConfiguration`
  key/value entity — the spec lists them separately but they're the same
  concept in every system that has both.

## Honest caveat

This sandbox has no .NET SDK and no NuGet access, so `dotnet build` could
not be run here. Every file was written and reviewed by hand (private
EF-Core-friendly constructors, nullable-reference-safe defaults, no
reserved-word collisions like `Finalize`), and a static brace/paren/
namespace check passed across all 55 files — but please run a real build
as your first step.

## Since this phase

`Domain/Repositories/` was added alongside Infrastructure (`IRepository<T>`,
`IUnitOfWork`) — generic persistence contracts with no framework dependency,
so they belong here rather than in a not-yet-built Application layer. See
`src/Backend/Infrastructure/README.md` for the EF Core implementation, and
`/PROGRESS.md` at the repo root for overall project status.
