using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec's "Account Scheduler — run Daily/Weekly/Monthly/Quarterly jobs" is
/// a scheduling *capability*, not one job — DayEndJob is the daily
/// example; this is the monthly one (outstanding-balance summary per
/// clinic), showing the same IJob + Quartz cron-trigger pattern at a
/// different cadence. Weekly/quarterly jobs (e.g. a quarterly GST report)
/// follow identically — left for whoever needs that specific report first
/// rather than adding speculative stubs with no real content.
/// </summary>
[DisallowConcurrentExecution]
public class MonthlyBillingSummaryJob : IJob
{
    public const string CronSchedule = "0 0 3 1 * ?"; // 03:00 on the 1st of each month

    private readonly IRepository<Invoice> _invoices;
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MonthlyBillingSummaryJob> _logger;

    public MonthlyBillingSummaryJob(IRepository<Invoice> invoices, IRepository<Notification> notifications, IUnitOfWork unitOfWork, ILogger<MonthlyBillingSummaryJob> logger)
    {
        _invoices = invoices;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var outstandingByClinic = await _invoices.GetQueryable()
            .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
            .GroupBy(i => i.ClinicId)
            .Select(g => new
            {
                ClinicId = g.Key,
                Count = g.Count(),
                TotalOutstanding = g.Sum(i => i.TotalAmount.Amount - i.AmountPaid.Amount)
            })
            .ToListAsync(ct);

        foreach (var summary in outstandingByClinic)
        {
            var alert = new Notification(
                NotificationChannel.Email,
                type: "MonthlyBillingSummary",
                subject: "Monthly outstanding balance summary",
                message: $"{summary.Count} outstanding invoice(s), totalling {summary.TotalOutstanding:N2}.",
                relatedEntityType: "Clinic",
                relatedEntityId: summary.ClinicId);

            await _notifications.AddAsync(alert, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("MonthlyBillingSummaryJob: queued {Count} clinic summaries", outstandingByClinic.Count);
    }
}
