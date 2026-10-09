using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.Commands;

public record ApproveReportCommand(Guid ReportId) : IRequest<ReportDto>;

public class ApproveReportCommandValidator : AbstractValidator<ApproveReportCommand>
{
    public ApproveReportCommandValidator() => RuleFor(x => x.ReportId).NotEmpty();
}

public class ApproveReportCommandHandler : IRequestHandler<ApproveReportCommand, ReportDto>
{
    private readonly IRepository<Report> _reports;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveReportCommandHandler(IRepository<Report> reports, IUnitOfWork unitOfWork)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportDto> Handle(ApproveReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Report), request.ReportId);

        report.Approve();
        _reports.Update(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReportDto.FromEntity(report);
    }
}
