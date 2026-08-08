namespace IGBZ.Application.Tests;

using IGBZ.Application.AiStudio;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using Xunit;

public class AiStudioServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (AiStudioService service, FakeCredentialRepository credentials, FakeHttpMessageHandler handler) Build()
    {
        _tenantContext.Set("t1");
        var credentials = new FakeCredentialRepository(_tenantContext);
        var handler = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactoryWithHandler(handler);
        return (new AiStudioService(credentials, FakeEncryptionService.Instance, factory), credentials, handler);
    }

    [Fact]
    public void GenerateSeoMeta_IsDeterministic_FromProductName()
    {
        var (service, _, _) = Build();

        var seo = service.GenerateSeoMeta("تی‌شرت مردانه", "یک تی‌شرت باکیفیت");

        Assert.Contains("تی‌شرت مردانه", seo.MetaTitle);
        Assert.Contains("تی‌شرت مردانه", seo.MetaDescription);
        Assert.Contains("#تی‌شرت_مردانه", seo.MetaKeywords);
    }

    [Fact]
    public void GenerateSeoMeta_EmptyName_Throws()
    {
        var (service, _, _) = Build();
        Assert.Throws<ArgumentException>(() => service.GenerateSeoMeta("", null));
    }

    [Fact]
    public async Task EnhancePhoto_NoCredential_Fails()
    {
        var (service, _, _) = Build();

        var result = await service.EnhancePhotoAsync(new EnhancePhotoRequest { ImageUrl = "https://img/x.jpg" });

        Assert.False(result.IsSuccess);
        Assert.Contains("deepfa", result.ErrorMessage);
    }

    [Fact]
    public async Task EnhancePhoto_Success_ReturnsOutputUrlFromResponse()
    {
        var (service, credentials, handler) = Build();
        credentials.AddActiveCredential("deepfa", "api-key");
        handler.RespondJson(200, new { output_url = "https://cdn.igbz/processed.jpg" });

        var result = await service.EnhancePhotoAsync(new EnhancePhotoRequest
        {
            ImageUrl = "https://img/x.jpg",
            SkuCode = "SKU-1"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.igbz/processed.jpg", result.OutputUrl);
        Assert.NotNull(handler.LastBody);
    }

    [Fact]
    public async Task EnhancePhoto_ResponseWithoutUrl_Fails()
    {
        var (service, credentials, handler) = Build();
        credentials.AddActiveCredential("deepfa", "api-key");
        handler.RespondJson(200, new { status = "ok" }); // بدون output_url

        var result = await service.EnhancePhotoAsync(new EnhancePhotoRequest { ImageUrl = "https://img/x.jpg" });

        Assert.False(result.IsSuccess);
        Assert.Contains("بدون URL", result.ErrorMessage);
    }

    [Fact]
    public async Task EnhancePhoto_GatewayError_Fails()
    {
        var (service, credentials, handler) = Build();
        credentials.AddActiveCredential("deepfa", "api-key");
        handler.RespondJson(500, new { error = "boom" });

        var result = await service.EnhancePhotoAsync(new EnhancePhotoRequest { ImageUrl = "https://img/x.jpg" });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AutoTranslate_NoCredential_Fails()
    {
        var (service, _, _) = Build();

        var result = await service.AutoTranslateAsync(new TranslateRequest { Text = "سلام", TargetLanguageCode = "en" });

        Assert.False(result.IsSuccess);
        Assert.Contains("tarjomyar", result.ErrorMessage);
    }

    [Fact]
    public async Task AutoTranslate_Success_ReadsTranslationFromResponse()
    {
        var (service, credentials, handler) = Build();
        credentials.AddActiveCredential("tarjomyar", "api-key");
        handler.RespondJson(200, new { translated_text = "Hello" });

        var result = await service.AutoTranslateAsync(new TranslateRequest { Text = "سلام", TargetLanguageCode = "en" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello", result.TranslatedText);
    }

    [Fact]
    public async Task GenerateVoiceOver_NoCredential_Fails()
    {
        var (service, _, _) = Build();

        var result = await service.GenerateVoiceOverAsync(new VoiceOverRequest { Text = "سلام" });

        Assert.False(result.IsSuccess);
        Assert.Contains("vira", result.ErrorMessage);
    }
}
