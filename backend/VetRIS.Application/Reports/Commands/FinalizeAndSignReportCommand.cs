using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.Commands;

public record FinalizeAndSignReportCommand : IRequest<ReportDto>
{
    public Guid ReportId { get; init; }
    public string SignatureData { get; init; } = default!;
    public string? PdfStorageKey { get; init; }
}

public class FinalizeAndSignReportCommandValidator : AbstractValidator<FinalizeAndSignReportCommand>
{
    public FinalizeAndSignReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.SignatureData).NotEmpty();
    }
}

public class FinalizeAndSignReportCommandHandler : IRequestHandler<FinalizeAndSignReportCommand, ReportDto>
{
    private readonly IRepository<Report> _reports;
    private readonly IUnitOfWork _unitOfWork;

    public FinalizeAndSignReportCommandHandler(IRepository<Report> reports, IUnitOfWork unitOfWork)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportDto> Handle(FinalizeAndSignReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Report), request.ReportId);

        report.FinalizeAndSign(request.SignatureData, request.PdfStorageKey);
        _reports.Update(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReportDto.FromEntity(report);
    }
}
