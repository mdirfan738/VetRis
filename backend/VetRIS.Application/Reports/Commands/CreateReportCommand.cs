using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.Commands;

public record CreateReportCommand : IRequest<ReportDto>
{
    public Guid StudyId { get; init; }
    public Guid RadiologistId { get; init; }
    public string Findings { get; init; } = default!;
    public Guid? TemplateId { get; init; }
}

public class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.StudyId).NotEmpty();
        RuleFor(x => x.RadiologistId).NotEmpty();
        RuleFor(x => x.Findings).NotEmpty();
    }
}

public class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, ReportDto>
{
    private readonly IRepository<Report> _reports;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReportCommandHandler(IRepository<Report> reports, IUnitOfWork unitOfWork)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
    }

    // Deliberately doesn't also touch Study here — creating a draft report
    // and calling Study.MarkReportGenerated() are kept as separate commands
    // (single-aggregate-per-handler) so the caller decides when a draft
    // actually counts as "report generation" for the study's own workflow.
    public async Task<ReportDto> Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        var report = new Report(request.StudyId, request.RadiologistId, request.Findings, request.TemplateId);

        await _reports.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReportDto.FromEntity(report);
    }
}
