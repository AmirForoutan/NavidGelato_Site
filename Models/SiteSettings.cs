using System.ComponentModel.DataAnnotations;

namespace NavidGelato_Site.Models;

public sealed class SiteSettings
{
    public int Id { get; set; }

    [MaxLength(12000)]
    public string? EnamadHtml { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
