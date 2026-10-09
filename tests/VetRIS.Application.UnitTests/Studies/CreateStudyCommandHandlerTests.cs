using FluentAssertions;
using Moq;
using VetRIS.Application.Studies.Commands;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;
using Xunit;

namespace VetRIS.Application.UnitTests.Studies;

public class CreateStudyCommandHandlerTests
{
    private readonly Mock<IRepository<Study>> _studiesMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private CreateStudyCommandHandler CreateHandler() => new(_studiesMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_ValidCommand_AddsStudyAndSaves()
    {
        var command = new CreateStudyCommand
        {
            AccessionNumber = "ACC-100",
            PatientId = Guid.NewGuid(),
            ClinicId = Guid.NewGuid(),
            Modality = Modality.CT,
            Priority = StudyPriority.Routine
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessionNumber.Should().Be("ACC-100");
        result.Status.Should().Be(nameof(StudyStatus.Created));
        _studiesMock.Verify(r => r.AddAsync(It.IsAny<Study>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
