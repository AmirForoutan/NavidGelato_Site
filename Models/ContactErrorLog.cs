using System.ComponentModel.DataAnnotations;

namespace NavidGelato_Site.Models;

public sealed class ContactErrorLog
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string RequestName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? RequestMobile { get; set; }

    [MaxLength(1200)]
    public string RequestMessage { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ExceptionType { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ErrorMessage { get; set; } = string.Empty;

    public string? TechnicalDetails { get; set; }

    [MaxLength(4000)]
    public string? IntendedSmsText { get; set; }

    [MaxLength(300)]
    public string RequestPath { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
