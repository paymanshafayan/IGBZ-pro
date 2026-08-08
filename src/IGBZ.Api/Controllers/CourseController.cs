namespace IGBZ.Api.Controllers;

using System.Security.Claims;
using IGBZ.Application.Lms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>دوره‌های آموزشی — مشتری واردشده (دسترسی از سفارش پرداخت‌شده).</summary>
[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CourseController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    private string RequireCustomerId() =>
        User.FindFirstValue("customerId") ?? throw new UnauthorizedAccessException("شناسهٔ مشتری یافت نشد.");

    /// <summary>لیست سرفصل‌ها + وضعیت دسترسی + درصد پیشرفت.</summary>
    [HttpGet("{productId}/lessons")]
    public async Task<IActionResult> GetLessons(string productId, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();
        var hasAccess = await _courseService.HasAccessAsync(customerId, productId, cancellationToken);
        var lessons = await _courseService.GetLessonsAsync(productId, cancellationToken);
        var completion = hasAccess ? await _courseService.GetCompletionPercentAsync(customerId, productId, cancellationToken) : 0;

        return Ok(new { success = true, hasAccess, completionPercent = completion, lessons });
    }

    /// <summary>لینک امن پخش ویدیو — فقط خریداران یا پیش‌نمایش رایگان.</summary>
    [HttpGet("lessons/{lessonId}/video")]
    public async Task<IActionResult> GetLessonVideo(string lessonId, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _courseService.GetLessonVideoAsync(customerId, lessonId, ip, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(403, new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, embedUrl = result.EmbedPlayerUrl, expiresOnUtc = result.ExpiresOnUtc });
    }

    [HttpPost("lessons/{lessonId}/complete")]
    public async Task<IActionResult> MarkCompleted(string lessonId, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();
        await _courseService.MarkLessonCompletedAsync(customerId, lessonId, cancellationToken);
        return Ok(new { success = true });
    }

    /// <summary>سؤالات آزمون (بدون گزینهٔ صحیح).</summary>
    [HttpGet("{productId}/quiz")]
    public async Task<IActionResult> GetQuiz(string productId, CancellationToken cancellationToken)
    {
        var questions = await _courseService.GetQuizQuestionsAsync(productId, cancellationToken);
        return Ok(new { success = true, questions });
    }

    [HttpPost("{productId}/quiz/submit")]
    public async Task<IActionResult> SubmitQuiz(string productId, [FromBody] SubmitQuizDto dto, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();

        if (!await _courseService.HasAccessAsync(customerId, productId, cancellationToken))
            return StatusCode(403, new { success = false, message = "شما این دوره را خریداری نکرده‌اید." });

        var result = await _courseService.GradeQuizAsync(customerId, productId, dto.Answers, cancellationToken);
        return Ok(new { success = true, scorePercent = result.ScorePercent, passed = result.Passed, message = result.Message, certificateCode = result.CertificateCode });
    }

    [HttpGet("{productId}/certificate")]
    public async Task<IActionResult> GetCertificate(string productId, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();
        var certificate = await _courseService.GetCertificateAsync(customerId, productId, cancellationToken);

        if (certificate == null)
            return NotFound(new { success = false, message = "هنوز گواهی صادر نشده است." });

        return Ok(new { success = true, certificateCode = certificate.CertificateCode, scorePercent = certificate.QuizScorePercent, issuedOnUtc = certificate.IssuedOnUtc });
    }
}

public class SubmitQuizDto
{
    public Dictionary<string, string> Answers { get; set; } = new();
}
