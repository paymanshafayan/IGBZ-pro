namespace IGBZ.Application.Lms;

/// <summary>خلاصهٔ درس برای لیست (بدون لینک ویدیو — لینک فقط از طریق امن صادر می‌شود).</summary>
public class CourseLessonSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public int DurationMinutes { get; init; }
    public bool IsFreePreview { get; init; }
    public bool HasAttachment { get; init; }
}

/// <summary>پاسخ نمره‌دهی آزمون.</summary>
public class QuizGradeResult
{
    public int ScorePercent { get; init; }
    public bool Passed { get; init; }
    public string? Message { get; init; }
    public string? CertificateCode { get; init; }
}

/// <summary>
/// سرویس آموزش (سند بخش ۱۵): کنترل دسترسی از سفارش واقعی پرداخت‌شده (نه Flag جعلی)،
/// لینک امن ویدیو، پیشرفت، آزمون چهارگزینه‌ای و گواهی دیجیتال.
/// </summary>
public interface ICourseService
{
    /// <summary>دسترسی واقعی: آیا مشتری دوره را خریده (سفارش پرداخت‌شده دارد)؟</summary>
    Task<bool> HasAccessAsync(string customerId, string productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CourseLessonSummaryDto>> GetLessonsAsync(string productId, CancellationToken cancellationToken = default);

    /// <summary>لینک امن پخش درس — فقط خریداران یا پیش‌نمایش رایگان.</summary>
    Task<SecureVideoUrlResult> GetLessonVideoAsync(
        string customerId, string lessonId, string userIpAddress, CancellationToken cancellationToken = default);

    Task MarkLessonCompletedAsync(string customerId, string lessonId, CancellationToken cancellationToken = default);

    Task<int> GetCompletionPercentAsync(string customerId, string productId, CancellationToken cancellationToken = default);

    /// <summary>دریافت سؤالات آزمون (بدون گزینهٔ صحیح).</summary>
    Task<IReadOnlyList<CourseQuizQuestion>> GetQuizQuestionsAsync(string productId, CancellationToken cancellationToken = default);

    Task<QuizGradeResult> GradeQuizAsync(
        string customerId, string productId, IReadOnlyDictionary<string, string> selectedOptionIdByQuestionId,
        CancellationToken cancellationToken = default);

    Task<CourseCertificate?> GetCertificateAsync(string customerId, string productId, CancellationToken cancellationToken = default);
}
