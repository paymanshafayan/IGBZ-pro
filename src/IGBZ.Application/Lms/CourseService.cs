namespace IGBZ.Application.Lms;

using System.Security.Cryptography;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Lms;
using IGBZ.Domain.Orders;

public class CourseService : ICourseService
{
    private const int PassingScorePercent = 70;

    private readonly ITenantScopedRepository<CourseLesson> _lessonRepository;
    private readonly ITenantScopedRepository<CourseQuizQuestion> _questionRepository;
    private readonly ITenantScopedRepository<CourseEnrollmentProgress> _progressRepository;
    private readonly ITenantScopedRepository<CourseCertificate> _certificateRepository;
    private readonly ITenantScopedRepository<Order> _orderRepository;
    private readonly ILmsVideoSecurityService _videoSecurityService;

    public CourseService(
        ITenantScopedRepository<CourseLesson> lessonRepository,
        ITenantScopedRepository<CourseQuizQuestion> questionRepository,
        ITenantScopedRepository<CourseEnrollmentProgress> progressRepository,
        ITenantScopedRepository<CourseCertificate> certificateRepository,
        ITenantScopedRepository<Order> orderRepository,
        ILmsVideoSecurityService videoSecurityService)
    {
        _lessonRepository = lessonRepository;
        _questionRepository = questionRepository;
        _progressRepository = progressRepository;
        _certificateRepository = certificateRepository;
        _orderRepository = orderRepository;
        _videoSecurityService = videoSecurityService;
    }

    public async Task<bool> HasAccessAsync(string customerId, string productId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(productId))
            return false;

        // دسترسی واقعی از سفارش پرداخت‌شده (سند ۱۵: نه Flag جعلی)
        var paidOrders = await _orderRepository.FindAsync(
            o => o.CustomerId == customerId
                 && o.Items.Any(i => i.ProductId == productId)
                 && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Processing
                     || o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Delivered),
            cancellationToken);

        return paidOrders.Count > 0;
    }

    public async Task<IReadOnlyList<CourseLessonSummaryDto>> GetLessonsAsync(string productId, CancellationToken cancellationToken = default)
    {
        var lessons = await _lessonRepository.FindAsync(l => l.ProductId == productId, cancellationToken);

        return lessons
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new CourseLessonSummaryDto
            {
                Id = l.Id,
                Title = l.Title,
                DisplayOrder = l.DisplayOrder,
                DurationMinutes = l.DurationMinutes,
                IsFreePreview = l.IsFreePreview,
                HasAttachment = !string.IsNullOrWhiteSpace(l.AttachmentUrl)
            })
            .ToList();
    }

    public async Task<SecureVideoUrlResult> GetLessonVideoAsync(
        string customerId, string lessonId, string userIpAddress, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson == null)
            return new SecureVideoUrlResult { IsSuccess = false, ErrorMessage = "درس یافت نشد." };

        if (!lesson.IsFreePreview && !await HasAccessAsync(customerId, lesson.ProductId, cancellationToken))
            return new SecureVideoUrlResult { IsSuccess = false, ErrorMessage = "شما این دوره را خریداری نکرده‌اید." };

        return await _videoSecurityService.GetSecureVideoUrlAsync(
            lesson.ProductId, lesson.Id, customerId, userIpAddress, null, TimeSpan.FromHours(2), cancellationToken);
    }

    public async Task MarkLessonCompletedAsync(string customerId, string lessonId, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson == null)
            return;

        var existing = await _progressRepository.FirstOrDefaultAsync(
            p => p.CustomerId == customerId && p.LessonId == lessonId, cancellationToken);

        if (existing != null)
        {
            if (!existing.IsCompleted)
            {
                existing.IsCompleted = true;
                existing.CompletedOnUtc = DateTime.UtcNow;
                await _progressRepository.UpdateAsync(existing, cancellationToken);
            }
            return;
        }

        await _progressRepository.InsertAsync(new CourseEnrollmentProgress
        {
            CustomerId = customerId,
            ProductId = lesson.ProductId,
            LessonId = lessonId,
            IsCompleted = true,
            CompletedOnUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<int> GetCompletionPercentAsync(string customerId, string productId, CancellationToken cancellationToken = default)
    {
        var lessons = await _lessonRepository.FindAsync(l => l.ProductId == productId, cancellationToken);
        if (lessons.Count == 0)
            return 0;

        var completed = await _progressRepository.FindAsync(
            p => p.CustomerId == customerId && p.ProductId == productId && p.IsCompleted, cancellationToken);

        return (int)Math.Round(100.0 * completed.Count / lessons.Count);
    }

    public async Task<IReadOnlyList<CourseQuizQuestion>> GetQuizQuestionsAsync(string productId, CancellationToken cancellationToken = default)
    {
        var questions = await _questionRepository.FindAsync(q => q.ProductId == productId, cancellationToken);

        // گزینهٔ صحیح به کلاینت نمی‌رود
        foreach (var q in questions)
        {
            foreach (var option in q.Options)
                option.IsCorrect = false;
        }

        return questions.OrderBy(q => q.DisplayOrder).ToList();
    }

    public async Task<QuizGradeResult> GradeQuizAsync(
        string customerId, string productId, IReadOnlyDictionary<string, string> selectedOptionIdByQuestionId,
        CancellationToken cancellationToken = default)
    {
        var questions = await _questionRepository.FindAsync(q => q.ProductId == productId, cancellationToken);
        if (questions.Count == 0)
            return new QuizGradeResult { ScorePercent = 0, Passed = false, Message = "این دوره آزمونی ندارد." };

        var correctCount = 0;
        foreach (var question in questions)
        {
            if (!selectedOptionIdByQuestionId.TryGetValue(question.Id, out var selectedOptionId))
                continue;

            var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
            if (correctOption != null && correctOption.Id == selectedOptionId)
                correctCount++;
        }

        var score = (int)Math.Round(100.0 * correctCount / questions.Count);
        var passed = score >= PassingScorePercent;

        if (passed)
        {
            var existing = await _certificateRepository.FirstOrDefaultAsync(
                c => c.CustomerId == customerId && c.ProductId == productId, cancellationToken);

            if (existing != null)
                return new QuizGradeResult { ScorePercent = score, Passed = true, CertificateCode = existing.CertificateCode };

            var certificate = new CourseCertificate
            {
                CustomerId = customerId,
                ProductId = productId,
                CertificateCode = $"CRT-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
                QuizScorePercent = score,
                IssuedOnUtc = DateTime.UtcNow
            };
            await _certificateRepository.InsertAsync(certificate, cancellationToken);

            return new QuizGradeResult { ScorePercent = score, Passed = true, CertificateCode = certificate.CertificateCode };
        }

        return new QuizGradeResult { ScorePercent = score, Passed = false, Message = $"نمرهٔ شما {score}٪ است (حداقل قبولی {PassingScorePercent}٪)." };
    }

    public async Task<CourseCertificate?> GetCertificateAsync(string customerId, string productId, CancellationToken cancellationToken = default)
    {
        return await _certificateRepository.FirstOrDefaultAsync(
            c => c.CustomerId == customerId && c.ProductId == productId, cancellationToken);
    }
}
