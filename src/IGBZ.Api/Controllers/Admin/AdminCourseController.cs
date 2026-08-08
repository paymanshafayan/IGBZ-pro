namespace IGBZ.Api.Controllers.Admin;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Lms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>مدیریت محتوای دوره (دروس + سؤالات آزمون) — فقط مالک تننت.</summary>
[ApiController]
[Route("api/admin/courses")]
[Authorize(Policy = "TenantOwner")]
public class AdminCourseController : ControllerBase
{
    private readonly ITenantScopedRepository<CourseLesson> _lessonRepository;
    private readonly ITenantScopedRepository<CourseQuizQuestion> _questionRepository;

    public AdminCourseController(
        ITenantScopedRepository<CourseLesson> lessonRepository,
        ITenantScopedRepository<CourseQuizQuestion> questionRepository)
    {
        _lessonRepository = lessonRepository;
        _questionRepository = questionRepository;
    }

    // ── دروس ──

    [HttpGet("{productId}/lessons")]
    public async Task<IActionResult> GetLessons(string productId, CancellationToken cancellationToken)
    {
        var lessons = await _lessonRepository.FindAsync(l => l.ProductId == productId, cancellationToken);
        return Ok(new { success = true, lessons = lessons.OrderBy(l => l.DisplayOrder) });
    }

    [HttpPost("{productId}/lessons")]
    public async Task<IActionResult> CreateLesson(string productId, [FromBody] LessonInputDto dto, CancellationToken cancellationToken)
    {
        var lesson = new CourseLesson
        {
            ProductId = productId,
            Title = dto.Title,
            DisplayOrder = dto.DisplayOrder,
            DurationMinutes = dto.DurationMinutes,
            VodVideoPath = dto.VodVideoPath,
            AttachmentUrl = dto.AttachmentUrl,
            IsFreePreview = dto.IsFreePreview,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _lessonRepository.InsertAsync(lesson, cancellationToken);
        return Ok(new { success = true, lessonId = lesson.Id });
    }

    [HttpDelete("lessons/{lessonId}")]
    public async Task<IActionResult> DeleteLesson(string lessonId, CancellationToken cancellationToken)
    {
        await _lessonRepository.DeleteAsync(lessonId, cancellationToken);
        return Ok(new { success = true });
    }

    // ── سؤالات آزمون ──

    [HttpPost("{productId}/quiz/questions")]
    public async Task<IActionResult> CreateQuestion(string productId, [FromBody] QuestionInputDto dto, CancellationToken cancellationToken)
    {
        var question = new CourseQuizQuestion
        {
            ProductId = productId,
            DisplayOrder = dto.DisplayOrder,
            QuestionText = dto.QuestionText,
            Options = dto.Options.Select(o => new CourseQuizOption
            {
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect
            }).ToList()
        };
        await _questionRepository.InsertAsync(question, cancellationToken);
        return Ok(new { success = true, questionId = question.Id });
    }

    [HttpDelete("quiz/questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(string questionId, CancellationToken cancellationToken)
    {
        await _questionRepository.DeleteAsync(questionId, cancellationToken);
        return Ok(new { success = true });
    }
}

public class LessonInputDto
{
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }
    public string? VodVideoPath { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool IsFreePreview { get; set; }
}

public class QuestionInputDto
{
    public int DisplayOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<OptionInputDto> Options { get; set; } = new();
}

public class OptionInputDto
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
