namespace IGBZ.Application.Tests;

using IGBZ.Application.Lms;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Common;
using IGBZ.Domain.Lms;
using IGBZ.Domain.Orders;
using IGBZ.Infrastructure.Lms;
using Xunit;

public class CourseServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (
        CourseService service,
        FakeTenantScopedRepository<CourseLesson> lessons,
        FakeTenantScopedRepository<CourseQuizQuestion> questions,
        FakeTenantScopedRepository<CourseEnrollmentProgress> progress,
        FakeTenantScopedRepository<CourseCertificate> certificates,
        FakeTenantScopedRepository<Order> orders) Build()
    {
        _tenantContext.Set("t1");

        var lessons = new FakeTenantScopedRepository<CourseLesson>(_tenantContext);
        var questions = new FakeTenantScopedRepository<CourseQuizQuestion>(_tenantContext);
        var progress = new FakeTenantScopedRepository<CourseEnrollmentProgress>(_tenantContext);
        var certificates = new FakeTenantScopedRepository<CourseCertificate>(_tenantContext);
        var orders = new FakeTenantScopedRepository<Order>(_tenantContext);
        var videoSecurity = new LmsVideoSecurityService("test-lms-secret-0123456789abcdef");

        var service = new CourseService(lessons, questions, progress, certificates, orders, videoSecurity);
        return (service, lessons, questions, progress, certificates, orders);
    }

    private static void SeedPaidOrder(FakeTenantScopedRepository<Order> orders, string customerId, string productId)
    {
        var order = new Order { TenantId = "t1", CustomerId = customerId };
        order.AddItem(productId, "SKU", 1, new Money(100_000));
        order.ApplyPricing(new Money(100_000), Money.Zero, Money.Zero, Money.Zero, Array.Empty<string>());
        order.MarkAsPaid("test", "trk-1", "ref-1");
        orders.Store.Add(order);
    }

    [Fact]
    public async Task HasAccess_True_WhenPaidOrderExists()
    {
        var (service, _, _, _, _, orders) = Build();
        SeedPaidOrder(orders, "c1", "course-1");

        Assert.True(await service.HasAccessAsync("c1", "course-1"));
    }

    [Fact]
    public async Task HasAccess_False_WhenNoPaidOrder()
    {
        var (service, _, _, _, _, _) = Build();

        Assert.False(await service.HasAccessAsync("c1", "course-1"));
    }

    [Fact]
    public async Task GetLessons_ReturnsOrderedSummaries_WithoutVideoUrl()
    {
        var (service, lessons, _, _, _, _) = Build();
        lessons.Store.Add(new CourseLesson { Id = "l1", ProductId = "c1", Title = "جلسه ۱", DisplayOrder = 2 });
        lessons.Store.Add(new CourseLesson { Id = "l2", ProductId = "c1", Title = "جلسه ۲", DisplayOrder = 1, VodVideoPath = "secret.mp4" });

        var result = await service.GetLessonsAsync("c1");

        Assert.Equal(2, result.Count);
        Assert.Equal("l2", result[0].Id); // ترتیب با DisplayOrder
        Assert.DoesNotContain(result, l => l.ToString()!.Contains("secret")); // URL ویدیو لو نمی‌رود
    }

    [Fact]
    public async Task GetLessonVideo_NoAccess_Fails()
    {
        var (service, lessons, _, _, _, _) = Build();
        lessons.Store.Add(new CourseLesson { Id = "l1", ProductId = "c1", Title = "جلسه", IsFreePreview = false });

        var result = await service.GetLessonVideoAsync("c1", "l1", "1.2.3.4");

        Assert.False(result.IsSuccess);
        Assert.Contains("خریداری", result.ErrorMessage);
    }

    [Fact]
    public async Task GetLessonVideo_FreePreview_WorksWithoutPurchase()
    {
        var (service, lessons, _, _, _, _) = Build();
        lessons.Store.Add(new CourseLesson { Id = "l1", ProductId = "c1", Title = "پیش‌نمایش", IsFreePreview = true });

        var result = await service.GetLessonVideoAsync("c1", "l1", "1.2.3.4");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.SignedToken);
    }

    [Fact]
    public async Task CompletionPercent_CalculatesCorrectly()
    {
        var (service, lessons, _, progress, _, _) = Build();
        lessons.Store.Add(new CourseLesson { Id = "l1", ProductId = "c1", Title = "۱" });
        lessons.Store.Add(new CourseLesson { Id = "l2", ProductId = "c1", Title = "۲" });

        await service.MarkLessonCompletedAsync("c1", "l1");
        var percent = await service.GetCompletionPercentAsync("c1", "c1");

        Assert.Equal(50, percent);
    }

    [Fact]
    public async Task GradeQuiz_Pass_IssuesCertificate()
    {
        var (service, _, questions, _, certificates, _) = Build();
        questions.Store.Add(new CourseQuizQuestion
        {
            Id = "q1",
            ProductId = "c1",
            QuestionText = "سؤال",
            Options =
            {
                new CourseQuizOption { Id = "a", OptionText = "غلط", IsCorrect = false },
                new CourseQuizOption { Id = "b", OptionText = "درست", IsCorrect = true }
            }
        });

        var result = await service.GradeQuizAsync("c1", "c1", new Dictionary<string, string> { ["q1"] = "b" });

        Assert.True(result.Passed);
        Assert.NotNull(result.CertificateCode);
        Assert.Single(certificates.Store);
    }

    [Fact]
    public async Task GradeQuiz_Fail_NoCertificate()
    {
        var (service, _, questions, _, certificates, _) = Build();
        questions.Store.Add(new CourseQuizQuestion
        {
            Id = "q1",
            ProductId = "c1",
            QuestionText = "سؤال",
            Options =
            {
                new CourseQuizOption { Id = "a", OptionText = "غلط", IsCorrect = false },
                new CourseQuizOption { Id = "b", OptionText = "درست", IsCorrect = true }
            }
        });

        var result = await service.GradeQuizAsync("c1", "c1", new Dictionary<string, string> { ["q1"] = "a" });

        Assert.False(result.Passed);
        Assert.Empty(certificates.Store);
    }

    [Fact]
    public async Task GradeQuiz_DoesNotLeakCorrectAnswer()
    {
        var (service, _, questions, _, _, _) = Build();
        questions.Store.Add(new CourseQuizQuestion
        {
            Id = "q1",
            ProductId = "c1",
            QuestionText = "سؤال",
            Options =
            {
                new CourseQuizOption { Id = "a", OptionText = "غلط", IsCorrect = true },
                new CourseQuizOption { Id = "b", OptionText = "درست", IsCorrect = false }
            }
        });

        var quiz = await service.GetQuizQuestionsAsync("c1");

        Assert.All(quiz.SelectMany(q => q.Options), o => Assert.False(o.IsCorrect));
    }
}
