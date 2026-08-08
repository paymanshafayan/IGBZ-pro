namespace IGBZ.Application.AiStudio;

/// <summary>یک قطعه موسیقی پس‌زمینهٔ Royalty-Free برای ویدیوی استوری.</summary>
public class BackgroundMusicTrack
{
    public string TrackId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Mood { get; init; } = string.Empty;
}

/// <summary>کاتالوگ موسیقی پس‌زمینه — فهرست ثابت (محلی، بدون API).</summary>
public interface IBackgroundMusicCatalogService
{
    IReadOnlyList<BackgroundMusicTrack> GetAvailableTracks();
}

public class BackgroundMusicCatalogService : IBackgroundMusicCatalogService
{
    // فهرست ثابت Royalty-Free — در فاز کامل از پنل ادمین قابل ویرایش می‌شود
    private static readonly BackgroundMusicTrack[] Tracks =
    {
        new() { TrackId = "calm-uplift", DisplayName = "آرام و انگیزشی", Mood = "آرام" },
        new() { TrackId = "energetic-pop", DisplayName = "انرژی پاپ", Mood = "پرانرژی" },
        new() { TrackId = "luxury-elegant", DisplayName = "لاکچری و شیک", Mood = "لاکچری" },
        new() { TrackId = "summer-fresh", DisplayName = "تابستانی و تازه", Mood = "شاد" },
        new() { TrackId = "cinematic-dramatic", DisplayName = "سینمایی و دراماتیک", Mood = "سینمایی" }
    };

    public IReadOnlyList<BackgroundMusicTrack> GetAvailableTracks() => Tracks;
}
