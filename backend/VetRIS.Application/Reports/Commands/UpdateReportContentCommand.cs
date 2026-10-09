using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.Commands;

public record UpdateReportContentCommand : IRequest<ReportDto>
{
    public Guid ReportId { get; init; }
    public string Findings { get; init; } = default!;
    public string? Impression { get; init; }
    public string? Recommendation { get; init; }
    public string EditedBy { get; init; } = default!;
}

public class UpdateReportContentCommandValidator : AbstractValidator<UpdateReportContentCommand>
{
    public UpdateReportContentCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Findings).NotEmpty();
        RuleFor(x => x.EditedBy).NotEmpty();
    }
}

public class UpdateReportContentCommandHandler : IRequestHandler<UpdateReportContentCommand, ReportDto>
{
    private readonly IRepository<Report> _reports;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateReportContentCommandHandler(IRepository<Report> reports, IUnitOfWork unitOfWork)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportDto> Handle(UpdateReportContentCommand request, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Report), request.ReportId);

        report.UpdateContent(request.Findings, request.Impression, request.Recommendation, request.EditedBy);
        _reports.Update(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReportDto.FromEntity(report);
    }
}
