using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NavidGelato_Site.Models;

public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
