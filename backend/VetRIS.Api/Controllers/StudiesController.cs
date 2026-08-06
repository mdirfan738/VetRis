using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRIS.Api.Data;
using VetRIS.Api.Dtos;
using VetRIS.Api.Models;
using VetRIS.Api.Services;

namespace VetRIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudiesController : ControllerBase
{
    private readonly VetrisDbContext _db;
    private readonly IS3UploadService _s3UploadService;
    private readonly ILogger<StudiesController> _logger;

    // 200 MB cap for scaffold purposes; tune per real DICOM series sizes.
    private const long MaxUploadBytes = 200L * 1024 * 1024;

    public StudiesController(VetrisDbContext db, IS3UploadService s3UploadService, ILogger<StudiesController> logger)
    {
        _db = db;
        _s3UploadService = s3UploadService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudyResponse>>> GetAll(CancellationToken ct)
    {
        var studies = await _db.Studies
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new StudyResponse(s.Id, s.PatientId, s.StudyDate, s.S3Key, s.Status, s.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(studies);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudyResponse>> GetById(int id, CancellationToken ct)
    {
        var study = await _db.Studies.FindAsync([id], ct);
        if (study is null) return NotFound();

        return Ok(new StudyResponse(study.Id, study.PatientId, study.StudyDate, study.S3Key, study.Status, study.CreatedAtUtc));
    }

    [HttpPost]
    public async Task<ActionResult<StudyResponse>> Create([FromBody] CreateStudyRequest request, CancellationToken ct)
    {
        var study = new Study
        {
            PatientId = request.PatientId,
            StudyDate = request.StudyDate,
            S3Key = request.S3Key,
            Status = "Received"
        };

        _db.Studies.Add(study);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = study.Id },
            new StudyResponse(study.Id, study.PatientId, study.StudyDate, study.S3Key, study.Status, study.CreatedAtUtc));
    }

    // POST /api/studies/upload — multipart/form-data with "file" and "patientId"
    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<StudyResponse>> Upload([FromForm] IFormFile file, [FromForm] string patientId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "A non-empty 'file' form field is required." });

        if (string.IsNullOrWhiteSpace(patientId))
            return BadRequest(new { message = "'patientId' form field is required." });

        await using var stream = file.OpenReadStream();
        var s3Key = await _s3UploadService.UploadAsync(stream, file.FileName, file.ContentType, ct);

        var study = new Study
        {
            PatientId = patientId,
            StudyDate = DateTime.UtcNow,
            S3Key = s3Key,
            Status = "Received"
        };

        _db.Studies.Add(study);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created study {StudyId} for patient {PatientId}", study.Id, study.PatientId);

        return CreatedAtAction(nameof(GetById), new { id = study.Id },
            new StudyResponse(study.Id, study.PatientId, study.StudyDate, study.S3Key, study.Status, study.CreatedAtUtc));
    }
}

public record CreateStudyRequest(string PatientId, DateTime StudyDate, string S3Key);
