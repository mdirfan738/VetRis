using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.Commands;

public record SubmitReportForApprovalCommand(Guid ReportId) : IRequest<ReportDto>;

public class SubmitReportForApprovalCommandValidator : AbstractValidator<SubmitReportForApprovalCommand>
{
    public SubmitReportForApprovalCommandValidator() => RuleFor(x => x.ReportId).NotEmpty();
}

public class SubmitReportForApprovalCommandHandler : IRequestHandler<SubmitReportForApprovalCommand, ReportDto>
{
    private readonly IRepository<Report> _reports;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitReportForApprovalCommandHandler(IRepository<Report> reports, IUnitOfWork unitOfWork)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportDto> Handle(SubmitReportForApprovalCommand request, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Report), request.ReportId);

        report.SubmitForApproval();
        _reports.Update(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReportDto.FromEntity(report);
    }
}
