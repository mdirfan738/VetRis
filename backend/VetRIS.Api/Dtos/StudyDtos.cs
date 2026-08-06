namespace VetRIS.Api.Dtos;

public record StudyResponse(
    int Id,
    string PatientId,
    DateTime StudyDate,
    string S3Key,
    string Status,
    DateTime CreatedAtUtc
);

public record HealthResponse(
    string Status,
    string Service,
    string Version,
    DateTime TimestampUtc
);
