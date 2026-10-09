using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.Commands;

public record AmendReportCommand : IRequest<ReportDto>
{
    public Guid ReportId { get; init; }
    public string Findings { get; init; } = default!;
    public string? Impression { get; init; }
    public string EditedBy { get; init; } = default!;
}

public class AmendReportCommandValidator : AbstractValidator<AmendReportCommand>
{
    public AmendReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Findings).NotEmpty();
        RuleFor(x => x.EditedBy).NotEmpty();
    }
}

public class AmendReportCommandHandler : IRequestHandler<AmendReportCommand, ReportDto>
{
    private readonly IRepository<Report> _reports;
    private readonly IUnitOfWork _unitOfWork;

    public AmendReportCommandHandler(IRepository<Report> reports, IUnitOfWork unitOfWork)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportDto> Handle(AmendReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Report), request.ReportId);

        report.Amend(request.Findings, request.Impression, request.EditedBy);
        _reports.Update(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReportDto.FromEntity(report);
    }
}
