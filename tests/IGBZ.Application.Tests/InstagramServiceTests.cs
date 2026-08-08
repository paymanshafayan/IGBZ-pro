namespace IGBZ.Application.Tests;

using System.Security.Cryptography;
using System.Text;
using IGBZ.Application.Instagram;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Discounts;
using IGBZ.Domain.Instagram;
using Xunit;

public class InstagramServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (
        InstagramService service,
        FakeCredentialRepository credentials,
        FakeTenantScopedRepository<InstagramFollowMentionReward> rewards,
        FakeTenantScopedRepository<Discount> discounts,
        FakeProductInventoryRepository products) Build()
    {
        _tenantContext.Set("t1");

        var credentials = new FakeCredentialRepository(_tenantContext);
        var rewards = new FakeTenantScopedRepository<InstagramFollowMentionReward>(_tenantContext);
        var discounts = new FakeTenantScopedRepository<Discount>(_tenantContext);
        var products = new FakeProductInventoryRepository(_tenantContext);
        var factory = new FakeHttpClientFactoryWithHandler(new FakeHttpMessageHandler());

        var service = new InstagramService(credentials, rewards, discounts, products, FakeEncryptionService.Instance, factory);
        return (service, credentials, rewards, discounts, products);
    }

    [Fact]
    public void VerifyWebhookSignature_ValidSignature_Passes()
    {
        var (service, _, _, _, _) = Build();
        var secret = "test-app-secret";
        Environment.SetEnvironmentVariable("IGBZ_INSTAGRAM_APP_SECRET", secret);

        var body = "{\"object\":\"instagram\"}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        Assert.True(service.VerifyWebhookSignature(body, $"sha256={hash}"));

        Environment.SetEnvironmentVariable("IGBZ_INSTAGRAM_APP_SECRET", null);
    }

    [Fact]
    public void VerifyWebhookSignature_InvalidSignature_Fails()
    {
        var (service, _, _, _, _) = Build();
        Environment.SetEnvironmentVariable("IGBZ_INSTAGRAM_APP_SECRET", "secret");

        Assert.False(service.VerifyWebhookSignature("body", "sha256=deadbeef"));

        Environment.SetEnvironmentVariable("IGBZ_INSTAGRAM_APP_SECRET", null);
    }

    [Fact]
    public void VerifyWebhookSignature_NoSecret_Fails()
    {
        var (service, _, _, _, _) = Build();
        Environment.SetEnvironmentVariable("IGBZ_INSTAGRAM_APP_SECRET", null);

        Assert.False(service.VerifyWebhookSignature("body", "sha256=abc"));
    }

    [Fact]
    public async Task ProcessComment_NoCredential_Fails()
    {
        var (service, _, _, _, _) = Build();

        var result = await service.ProcessCommentAsync("t1", "c1", "SKU-1", "ig-1");

        Assert.False(result.Processed);
        Assert.Contains("توکن", result.Message);
    }

    [Fact]
    public async Task ProcessComment_UnknownSku_NotProcessed()
    {
        var (service, credentials, _, _, _) = Build();
        credentials.AddActiveCredential("instagram.graph", "token");

        var result = await service.ProcessCommentAsync("t1", "c1", "NO-SUCH-SKU", "ig-1");

        Assert.False(result.Processed);
        Assert.Contains("محصول", result.Message);
    }

    [Fact]
    public async Task ProcessMention_NoCredential_Fails()
    {
        var (service, _, _, _, _) = Build();

        var result = await service.ProcessMentionAsync("t1", "ig-1", null);

        Assert.False(result.Processed);
        Assert.Contains("توکن", result.Message);
    }

    [Fact]
    public async Task ProcessMention_AlreadyRewarded_NotProcessed()
    {
        var (service, credentials, rewards, _, _) = Build();
        credentials.AddActiveCredential("instagram.graph", "token");
        rewards.Store.Add(new InstagramFollowMentionReward
        {
            Id = "r1",
            TenantId = "t1",
            InstagramScopedId = "ig-1",
            CouponCode = "OLD",
            IssuedOnUtc = DateTime.UtcNow
        });

        var result = await service.ProcessMentionAsync("t1", "ig-1", null);

        Assert.False(result.Processed);
        Assert.Contains("قبلاً", result.Message);
    }

    [Fact]
    public async Task ProcessMention_NoFollowGraphCall_FailsGracefully()
    {
        // بدون پاسخ واقعی از Graph (HttpClient ساختگی پاسخ OK خالی می‌دهد) → isFollowing=false
        var (service, credentials, _, discounts, _) = Build();
        credentials.AddActiveCredential("instagram.graph", "token");

        var result = await service.ProcessMentionAsync("t1", "ig-1", null);

        Assert.False(result.Processed); // فالو بررسی شد ولی تایید نشد
        Assert.Empty(discounts.Store);  // کد تخفیف صادر نشد
    }

    [Fact]
    public async Task PublishProductPost_NoCredential_Fails()
    {
        var (service, _, _, _, _) = Build();

        var result = await service.PublishProductPostAsync("p1", "https://img/x.jpg");

        Assert.False(result.IsSuccess);
        Assert.Contains("توکن", result.Message);
    }

    [Fact]
    public async Task PublishProductPost_NoImage_Fails()
    {
        var (service, credentials, _, _, _) = Build();
        credentials.AddActiveCredential("instagram.graph", "token");

        var result = await service.PublishProductPostAsync("p1", "");

        Assert.False(result.IsSuccess);
        Assert.Contains("تصویر", result.Message);
    }

    [Fact]
    public async Task PublishProductPost_NoProduct_Fails()
    {
        var (service, credentials, _, _, _) = Build();
        credentials.AddActiveCredential("instagram.graph", "token");

        var result = await service.PublishProductPostAsync("p999", "https://img/x.jpg");

        Assert.False(result.IsSuccess);
    }
}
