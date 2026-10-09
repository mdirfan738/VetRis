using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetRIS.Application.Common.Events;
using VetRIS.Application.Common.Interfaces;
using VetRIS.Domain.Common;
using VetRIS.Domain.Entities.Admin;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Entities.Organizations;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Infrastructure.Identity;

namespace VetRIS.Infrastructure.Persistence;

/// <summary>
/// The single EF Core DbContext for VetRIS. Also the Identity store
/// (IdentityDbContext&lt;ApplicationUser, ApplicationRole, Guid&gt;), so users/roles
/// and business data share one database and one migration history.
/// </summary>
public class VetRISDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;

    public VetRISDbContext(
        DbContextOptions<VetRISDbContext> options,
        ICurrentUserService currentUserService,
        IPublisher publisher)
        : base(options)
    {
        _currentUserService = currentUserService;
        _publisher = publisher;
    }

    // Organizations
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<Clinic> Clinics => Set<Clinic>();

    // Clinical
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Radiologist> Radiologists => Set<Radiologist>();
    public DbSet<Study> Studies => Set<Study>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<DicomImage> DicomImages => Set<DicomImage>();
    public DbSet<StudyStatusHistory> StudyStatusHistories => Set<StudyStatusHistory>();
    public DbSet<StudyComment> StudyComments => Set<StudyComment>();

    // Reporting
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportVersion> ReportVersions => Set<ReportVersion>();

    // Billing
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Ops
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();
    public DbSet<JobExecutionLog> JobExecutionLogs => Set<JobExecutionLog>();

    // Admin
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<MasterDataItem> MasterDataItems => Set<MasterDataItem>();

    // Identity-adjacent
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Soft-delete query filter, applied uniformly to every BaseEntity-derived
        // type via reflection rather than repeating `HasQueryFilter` 24 times.
        // Must run after ApplyConfigurationsFromAssembly since HasQueryFilter
        // is last-write-wins per entity type.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                typeof(VetRISDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(null, new object[] { builder });
            }
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : BaseEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteRules();

        var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch only after the transaction has actually committed, so
        // handlers (notifications, writeback, workflow services) never react
        // to a change that was rolled back.
        await DispatchDomainEventsAsync(aggregatesWithEvents, cancellationToken);

        return result;
    }

    private void ApplyAuditAndSoftDeleteRules()
    {
        var userId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(userId);
                    break;
                case EntityState.Modified:
                    entry.Entity.MarkUpdated(userId);
                    break;
                case EntityState.Deleted:
                    // Convert every hard delete into a soft delete so audit
                    // trails and FK-dependent history rows are never lost.
                    entry.State = EntityState.Modified;
                    entry.Entity.SoftDelete(userId);
                    break;
            }
        }
    }

    private async Task DispatchDomainEventsAsync(List<AggregateRoot> aggregatesWithEvents, CancellationToken cancellationToken)
    {
        var domainEvents = aggregatesWithEvents.SelectMany(a => a.DomainEvents).ToList();
        aggregatesWithEvents.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(wrapperType, domainEvent)!;
            await _publisher.Publish(notification, cancellationToken);
        }
    }
}
