using FluentAssertions;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;
using Xunit;

namespace VetRIS.Domain.UnitTests.Entities;

public class ReportTests
{
    private static Report CreateReport() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "No acute findings.");

    [Fact]
    public void Constructor_SetsStatusToDraft()
    {
        CreateReport().Status.Should().Be(ReportStatus.Draft);
    }

    [Fact]
    public void Constructor_EmptyFindings_Throws()
    {
        var act = () => new Report(Guid.NewGuid(), Guid.NewGuid(), "");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FullWorkflow_HappyPath_ReachesSigned()
    {
        var report = CreateReport();

        report.SubmitForApproval();
        report.Approve();
        report.FinalizeAndSign("base64-signature-data");

        report.Status.Should().Be(ReportStatus.Signed);
        report.SignedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Approve_WithoutSubmitting_Throws()
    {
        var act = () => CreateReport().Approve();
        act.Should().Throw<InvalidReportStatusTransitionException>();
    }

    [Fact]
    public void FinalizeAndSign_WithoutApproval_Throws()
    {
        var report = CreateReport();
        report.SubmitForApproval();

        var act = () => report.FinalizeAndSign("sig");
        act.Should().Throw<InvalidReportStatusTransitionException>();
    }

    [Fact]
    public void FinalizeAndSign_RaisesReportFinalizedEvent()
    {
        var report = CreateReport();
        report.SubmitForApproval();
        report.Approve();
        report.ClearDomainEvents();

        report.FinalizeAndSign("sig");

        report.DomainEvents.Should().ContainSingle(e => e is ReportFinalizedEvent);
    }

    [Fact]
    public void UpdateContent_AfterSigned_Throws()
    {
        var report = CreateReport();
        report.SubmitForApproval();
        report.Approve();
        report.FinalizeAndSign("sig");

        var act = () => report.UpdateContent("new findings", null, null, "dr-smith");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateContent_SnapshotsPreviousContentAsAVersion()
    {
        var report = CreateReport();

        report.UpdateContent("Updated findings", "Updated impression", null, "dr-smith");

        report.Versions.Should().ContainSingle();
        report.Versions.Single().Findings.Should().Be("No acute findings."); // the OLD content
        report.Findings.Should().Be("Updated findings");
        report.Version.Should().Be(2);
    }

    [Fact]
    public void Amend_OnlyAllowedWhenSigned()
    {
        var act = () => CreateReport().Amend("corrected findings", null, "dr-smith");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Amend_WhenSigned_SetsStatusToAmended()
    {
        var report = CreateReport();
        report.SubmitForApproval();
        report.Approve();
        report.FinalizeAndSign("sig");

        report.Amend("corrected findings", "corrected impression", "dr-jones");

        report.Status.Should().Be(ReportStatus.Amended);
        report.Findings.Should().Be("corrected findings");
    }
}
