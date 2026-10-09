using FluentAssertions;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;
using Xunit;

namespace VetRIS.Domain.UnitTests.Entities;

public class StudyTests
{
    private static Study CreateStudy() =>
        new("ACC-001", Guid.NewGuid(), Guid.NewGuid(), Modality.CT, StudyPriority.Routine);

    private static Study CreateAndArchive()
    {
        var study = CreateStudy();
        study.MarkInProgress();
        study.AssignRadiologist(Guid.NewGuid());
        study.OpenForReview();
        study.MarkReportGenerated();
        study.MarkWrittenBack();
        study.MarkDistributed();
        study.MarkFinalized();
        study.Archive();
        return study;
    }

    [Fact]
    public void Constructor_SetsInitialStatusToCreated()
    {
        CreateStudy().Status.Should().Be(StudyStatus.Created);
    }

    [Fact]
    public void Constructor_RaisesStudyCreatedEvent()
    {
        CreateStudy().DomainEvents.Should().ContainSingle(e => e is StudyCreatedEvent);
    }

    [Fact]
    public void Constructor_EmptyAccessionNumber_Throws()
    {
        var act = () => new Study("", Guid.NewGuid(), Guid.NewGuid(), Modality.CT, StudyPriority.Routine);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FullWorkflow_HappyPath_ReachesArchived()
    {
        var study = CreateAndArchive();

        study.Status.Should().Be(StudyStatus.Archived);
        study.StatusHistory.Should().HaveCount(8);
    }

    [Fact]
    public void AssignRadiologist_WhenArchived_Throws()
    {
        var study = CreateAndArchive();

        var act = () => study.AssignRadiologist(Guid.NewGuid());
        act.Should().Throw<InvalidStudyStatusTransitionException>();
    }

    [Fact]
    public void OpenForReview_WithoutAssignedRadiologist_Throws()
    {
        var act = () => CreateStudy().OpenForReview();
        act.Should().Throw<InvalidStudyStatusTransitionException>();
    }

    [Fact]
    public void Archive_BeforeFinalized_Throws()
    {
        var act = () => CreateStudy().Archive();
        act.Should().Throw<InvalidStudyStatusTransitionException>();
    }

    [Fact]
    public void RequireAction_EmptyReason_Throws()
    {
        var act = () => CreateStudy().RequireAction("");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RequireAction_ValidReason_AddsCommentAndTransitions()
    {
        var study = CreateStudy();

        study.RequireAction("Missing prior images");

        study.Status.Should().Be(StudyStatus.ActionRequired);
        study.Comments.Should().ContainSingle(c => c.Comment.Contains("Missing prior images"));
    }

    [Fact]
    public void Cancel_WhenArchived_Throws()
    {
        var study = CreateAndArchive();

        var act = () => study.Cancel("test");
        act.Should().Throw<InvalidStudyStatusTransitionException>();
    }

    [Fact]
    public void MarkCritical_SetsFlag()
    {
        var study = CreateStudy();
        study.MarkCritical();
        study.IsCriticalFinding.Should().BeTrue();
    }

    [Fact]
    public void StatusChange_RaisesStudyStatusChangedEventWithCorrectFromAndTo()
    {
        var study = CreateStudy();
        study.ClearDomainEvents();

        study.MarkInProgress();

        var evt = study.DomainEvents.Should().ContainSingle(e => e is StudyStatusChangedEvent).Which as StudyStatusChangedEvent;
        evt!.FromStatus.Should().Be(StudyStatus.Created);
        evt.ToStatus.Should().Be(StudyStatus.InProgress);
    }
}
