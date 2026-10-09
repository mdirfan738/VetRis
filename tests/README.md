# VetRIS — Tests

**13 files, 68 `[Fact]` tests**, across three projects with three different
confidence levels — worth knowing which is which before trusting any of
them without running `dotnet test` yourself first.

```
tests/
├── VetRIS.Domain.UnitTests/          53 tests — HIGHEST confidence
├── VetRIS.Application.UnitTests/      9 tests — high confidence
└── VetRIS.Api.IntegrationTests/       6 tests — LOWER confidence
```

## VetRIS.Domain.UnitTests (highest confidence)

No mocking, no DI, no hosting — Domain has zero external dependencies, so
these tests instantiate entities directly and assert on behavior. This is
the same reason Domain itself was the highest-confidence *code* in the
solution; the same is true of testing it. Covers:

- `Money`/`Address` value-object arithmetic, validation, and equality
- `Study`'s full workflow state machine — happy path start-to-archive,
  every invalid-transition guard, domain events raised at the right times
- `Report`'s draft→approve/reject→sign→amend cycle, including that
  `UpdateContent` snapshots the *previous* content into a `ReportVersion`
  (easy to get backwards — the test pins it down)
- `Invoice`'s line-item/GST math, payment registration (full vs. partial →
  correct status), and that a cancelled invoice rejects further payments
- `Payment`'s status transitions and that `MarkCompleted` is a one-shot

## VetRIS.Application.UnitTests (high confidence)

Handlers with `IRepository<T>`/`IUnitOfWork` mocked via Moq — a
representative sample, not exhaustive coverage of all ~40 handlers:
`CreateStudyCommandHandler`, `AssignRadiologistCommandHandler` (including
the not-found path), `RegisterPaymentCommandHandler` (the one
two-aggregate handler — worth the extra test attention), and one
`FluentValidation.TestHelper`-based validator test class showing that
pattern. The same mock-repository approach applies directly to every other
handler in `Application/` — these four were chosen to cover the shapes
that actually differ (simple create, not-found handling, and cross-aggregate
coordination), not because the rest don't need tests.

## VetRIS.Api.IntegrationTests (lower confidence — read this before trusting it)

`WebApplicationFactory<Program>` against SQLite in-memory standing in for
MySQL (`CustomWebApplicationFactory.cs`), covering `/health` and the
authorization boundary (protected endpoints correctly 401 without a
token; a bad login is a clean 401, not a 500). This is the
**second-lowest-confidence code in the whole solution**, after
`FoDicomPacsService.cs` — for a different reason: it's not an unfamiliar
third-party API, it's that `WebApplicationFactory` + `IdentityDbContext` +
SQLite's `EnsureCreated()` path (rather than real migrations) has genuine
edge cases that are easy to get subtly wrong without a compiler to check
against. If `dotnet test` surfaces problems anywhere in this build, this
project is the first place to look — the Domain and Application test
projects need no hosting infrastructure at all and are much more likely to
just work.

## What's not here

**Repository tests** (Infrastructure): would need a real or Testcontainers
MySQL instance to be meaningful — an in-memory substitute would mostly
just test EF Core itself, not the `Repository<T>`/entity-configuration code
that's actually ours. **Controller tests** beyond the authorization checks
above: most controller logic is a one-line `Mediator.Send()`, so the
Application handler tests already cover the behavior that matters; adding
45 near-identical "does this endpoint call Send()" tests wasn't worth the
file count. **Load/Performance tests**: different tooling entirely (k6,
NBomber, JMeter) against a *running* deployment, not something to fake with
xUnit against nothing.

## Running these

```
dotnet test tests/VetRIS.Domain.UnitTests
dotnet test tests/VetRIS.Application.UnitTests
dotnet test tests/VetRIS.Api.IntegrationTests
```

None of this has run against a real compiler in this sandbox (no .NET SDK,
no NuGet access) — same caveat as every other phase, just worth repeating
here specifically since "do these tests even compile and pass" is exactly
the question a test suite exists to answer, and this one hasn't been asked
yet for real.
