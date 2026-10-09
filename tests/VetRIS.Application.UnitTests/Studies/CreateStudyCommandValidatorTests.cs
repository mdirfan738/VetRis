using FluentValidation.TestHelper;
using VetRIS.Application.Studies.Commands;
using VetRIS.Domain.Enums;
using Xunit;

namespace VetRIS.Application.UnitTests.Studies;

public class CreateStudyCommandValidatorTests
{
    private readonly CreateStudyCommandValidator _validator = new();

    [Fact]
    public void EmptyAccessionNumber_HasValidationError()
    {
        var command = new CreateStudyCommand { AccessionNumber = "", PatientId = Guid.NewGuid(), ClinicId = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AccessionNumber);
    }

    [Fact]
    public void EmptyPatientId_HasValidationError()
    {
        var command = new CreateStudyCommand { AccessionNumber = "ACC-1", PatientId = Guid.Empty, ClinicId = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.PatientId);
    }

    [Fact]
    public void ValidCommand_HasNoValidationErrors()
    {
        var command = new CreateStudyCommand
        {
            AccessionNumber = "ACC-1",
            PatientId = Guid.NewGuid(),
            ClinicId = Guid.NewGuid(),
            Modality = Modality.CT,
            Priority = StudyPriority.Routine
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
