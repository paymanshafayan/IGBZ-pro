namespace IGBZ.Domain.Lms;

/// <summary>درس (سرفصل) یک دوره — دوره = محصول دیجیتال (ProductId).</summary>
public class CourseLesson : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    /// <summary>شناسهٔ محصول (دوره) در کاتالوگ.</summary>
    public string ProductId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>لینک ویدیو (VOD) — با توکن امن سرو می‌شود (سند ۱۵.۲).</summary>
    public string? VodVideoPath { get; set; }

    public string? AttachmentUrl { get; set; }

    /// <summary>پیش‌نمایش رایگان بدون نیاز به خرید.</summary>
    public bool IsFreePreview { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>سؤال چهارگزینه‌ای آزمون دوره.</summary>
public class CourseQuizQuestion : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<CourseQuizOption> Options { get; set; } = new();
}

public class CourseQuizOption
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

/// <summary>پیشرفت مشتری در دوره (درس‌های تکمیل‌شده).</summary>
public class CourseEnrollmentProgress : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedOnUtc { get; set; }
}

/// <summary>گواهی دیجیتال پس از قبولی در آزمون.</summary>
public class CourseCertificate : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string CertificateCode { get; set; } = string.Empty;
    public int QuizScorePercent { get; set; }
    public DateTime IssuedOnUtc { get; set; } = DateTime.UtcNow;
}
