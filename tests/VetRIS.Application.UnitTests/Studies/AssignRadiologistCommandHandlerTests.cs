using FluentAssertions;
using Moq;
using VetRIS.Application.Studies.Commands;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;
using Xunit;

namespace VetRIS.Application.UnitTests.Studies;

public class AssignRadiologistCommandHandlerTests
{
    private readonly Mock<IRepository<Study>> _studiesMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private AssignRadiologistCommandHandler CreateHandler() => new(_studiesMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_StudyNotFound_ThrowsEntityNotFoundException()
    {
        _studiesMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Study?)null);

        var command = new AssignRadiologistCommand { StudyId = Guid.NewGuid(), RadiologistId = Guid.NewGuid() };

        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Handle_StudyExists_AssignsAndSaves()
    {
        var study = new Study("ACC-200", Guid.NewGuid(), Guid.NewGuid(), Modality.MR, StudyPriority.Urgent);
        study.MarkInProgress();
        var radiologistId = Guid.NewGuid();

        _studiesMock.Setup(r => r.GetByIdAsync(study.Id, It.IsAny<CancellationToken>())).ReturnsAsync(study);

        var command = new AssignRadiologistCommand { StudyId = study.Id, RadiologistId = radiologistId, ChangedBy = "dr-admin" };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AssignedRadiologistId.Should().Be(radiologistId);
        result.Status.Should().Be(nameof(StudyStatus.AssignedToRadiologist));
        _studiesMock.Verify(r => r.Update(study), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
