namespace IGBZ.Api.Controllers;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Instagram;
using IGBZ.Application.Tenancy;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// وب‌هوک متا (Instagram) — GET برای هندشیک تایید و POST برای رویدادها.
/// امضای HMAC-SHA256 پیش از پردازش اعتبارسنجی می‌شود.
/// </summary>
[ApiController]
[Route("api/instagram/webhook")]
public class InstagramWebhookController : ControllerBase
{
    private readonly IInstagramService _instagramService;
    private readonly ITenantContext _tenantContext;

    public InstagramWebhookController(IInstagramService instagramService, ITenantContext tenantContext)
    {
        _instagramService = instagramService;
        _tenantContext = tenantContext;
    }

    /// <summary>هندشیک تایید اشتراک وب‌هوک (hub.challenge را عیناً برمی‌گرداند).</summary>
    [HttpGet]
    public IActionResult VerifySubscription(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = Environment.GetEnvironmentVariable("IGBZ_INSTAGRAM_WEBHOOK_VERIFY_TOKEN");
        if (string.IsNullOrWhiteSpace(expectedToken))
            return StatusCode(500, "توکن تایید وب‌هوک تنظیم نشده است.");

        if (mode == "subscribe" && verifyToken == expectedToken && !string.IsNullOrWhiteSpace(challenge))
            return Content(challenge!, "text/plain");

        return Forbid();
    }

    /// <summary>دریافت رویدادها (comments/mentions) — با اعتبارسنجی امضا.</summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }
        Request.Body.Position = 0;

        var signatureHeader = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (!_instagramService.VerifyWebhookSignature(rawBody, signatureHeader ?? string.Empty))
            return Unauthorized();

        InstagramWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<InstagramWebhookPayload>(rawBody);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (payload?.Entry == null)
            return Ok(); // متا انتظار ۲۰۰ دارد حتی اگر چیزی نباشد

        foreach (var entry in payload.Entry)
        {
            // در این نسخه، تننت از هدر X-Tenant-Id (یا در حالت تک‌فروشگاه از مقدار پیش‌فرض)
            // خوانده می‌شود؛ مسیریابی چندتننتی بر اساس entry.id در فاز کامل افزوده می‌شود.
            var tenantId = _tenantContext.TenantId ?? "platform";

            foreach (var change in entry.Changes ?? new List<InstagramWebhookChange>())
            {
                if (change.Field == "comments")
                {
                    var commentId = GetString(change.Value, "id");
                    var commentText = GetString(change.Value, "text");
                    var fromId = GetNestedString(change.Value, "from", "id");

                    if (commentId != null && commentText != null && fromId != null)
                    {
                        await _instagramService.ProcessCommentAsync(tenantId, commentId, commentText, fromId, cancellationToken);
                    }
                }
                else if (change.Field == "mentions")
                {
                    var fromId = GetString(change.Value, "from") ?? GetString(change.Value, "sender");
                    var mediaId = GetString(change.Value, "media_id");

                    if (fromId != null)
                    {
                        await _instagramService.ProcessMentionAsync(tenantId, fromId, mediaId, cancellationToken);
                    }
                }
            }
        }

        return Ok();
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value))
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return null;
    }

    private static string? GetNestedString(JsonElement element, string outer, string inner)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(outer, out var outerEl)
            && outerEl.ValueKind == JsonValueKind.Object && outerEl.TryGetProperty(inner, out var innerEl))
            return innerEl.ValueKind == JsonValueKind.String ? innerEl.GetString() : null;
        return null;
    }
}

internal class InstagramWebhookPayload
{
    [JsonPropertyName("object")] public string? Object { get; set; }
    [JsonPropertyName("entry")] public List<InstagramWebhookEntry>? Entry { get; set; }
}

internal class InstagramWebhookEntry
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("changes")] public List<InstagramWebhookChange>? Changes { get; set; }
}

internal class InstagramWebhookChange
{
    [JsonPropertyName("field")] public string? Field { get; set; }
    [JsonPropertyName("value")] public JsonElement Value { get; set; }
}
