using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec: "Day End Service — automatically generate Reports, Invoices,
/// Logs, Payments, Backup." Interpreted narrowly and honestly: this job
/// does the part that's actually an application-code responsibility —
/// marking overdue invoices and writing a daily summary log. Database
/// backup is an RDS/ops concern (automated snapshots via Terraform), not
/// application code; faking a "backup" step here would be worse than not
/// having one. Report/Invoice *generation* already happens on-demand
/// through their own commands as studies complete, rather than being
/// batched to end-of-day.
/// </summary>
[DisallowConcurrentExecution]
public class DayEndJob : IJob
{
    public const string CronSchedule = "0 30 23 * * ?"; // 23:30 daily

    private readonly IRepository<Invoice> _invoices;
    private readonly IRepository<SystemLog> _systemLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DayEndJob> _logger;

    public DayEndJob(IRepository<Invoice> invoices, IRepository<SystemLog> systemLogs, IUnitOfWork unitOfWork, ILogger<DayEndJob> logger)
    {
        _invoices = invoices;
        _systemLogs = systemLogs;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTime.UtcNow;

        var overdue = await _invoices.GetQueryable(asNoTracking: false)
            .Where(i => (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid) && i.DueAtUtc < now)
            .ToListAsync(ct);

        foreach (var invoice in overdue)
        {
            invoice.MarkOverdue();
            _invoices.Update(invoice);
        }

        await _systemLogs.AddAsync(new SystemLog(
            "Information",
            $"Day End: marked {overdue.Count} invoice(s) overdue.",
            source: nameof(DayEndJob)), ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("DayEndJob complete: {OverdueCount} invoices marked overdue", overdue.Count);
    }
}
