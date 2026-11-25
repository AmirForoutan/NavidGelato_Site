using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;

namespace NavidGelato_Site.Pages.Admin;

public class SmsSettingsModel(
    AppDbContext db,
    IDataProtectionProvider provider,
    IAdminPermissionService permissions) : PageModel
{
    public const string DefaultPaymentLinkTemplate =
        "نوید ژلاتو\nلینک پرداخت {amount} ریال:\n{link}\nمهلت پرداخت: ۱۵ دقیقه پس از اولین باز شدن لینک";

    private readonly IDataProtector _passwordProtector =
        provider.CreateProtector("NavidPro.MeliPayamak.Password.v1");

    [BindProperty]
    public SmsForm Form { get; set; } = new();

    public bool HasSavedPassword { get; private set; }

    public sealed class SmsForm
    {
        public bool Enabled { get; set; }

        [MaxLength(200)]
        public string? Username { get; set; }

        [DataType(DataType.Password), MaxLength(500)]
        public string? Password { get; set; }

        [MaxLength(50)]
        public string? From { get; set; }

        [MaxLength(3000)]
        public string? RecipientNumbers { get; set; }

        [Required, MaxLength(3000)]
        public string PaymentLinkTemplate { get; set; } = DefaultPaymentLinkTemplate;

        public bool MessageFilterEnabled { get; set; } = true;

        [MaxLength(12000)]
        public string? MessageFilterTerms { get; set; } = SmsSettings.DefaultMessageFilterTerms;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Sms.View))
            return Forbid();

        var settings = await GetSettingsAsync();
        HasSavedPassword = !string.IsNullOrWhiteSpace(settings.EncryptedPassword);

        Form = new SmsForm
        {
            Enabled = settings.Enabled,
            Username = settings.Username,
            From = settings.From,
            RecipientNumbers = settings.RecipientNumbers,
            PaymentLinkTemplate = string.IsNullOrWhiteSpace(settings.PaymentLinkTemplate)
                ? DefaultPaymentLinkTemplate
                : settings.PaymentLinkTemplate,
            MessageFilterEnabled = settings.MessageFilterEnabled,
            MessageFilterTerms = string.IsNullOrWhiteSpace(settings.MessageFilterTerms)
                ? SmsSettings.DefaultMessageFilterTerms
                : settings.MessageFilterTerms
        };

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Sms.Edit))
            return Forbid();

        var settings = await GetSettingsAsync();
        HasSavedPassword = !string.IsNullOrWhiteSpace(settings.EncryptedPassword);

        var recipients = Parse(Form.RecipientNumbers);
        Form.PaymentLinkTemplate = string.IsNullOrWhiteSpace(Form.PaymentLinkTemplate)
            ? string.Empty
            : Form.PaymentLinkTemplate.Trim();
        Form.MessageFilterTerms = string.IsNullOrWhiteSpace(Form.MessageFilterTerms)
            ? string.Empty
            : Form.MessageFilterTerms.Trim();

        if (Form.Enabled)
        {
            if (string.IsNullOrWhiteSpace(Form.Username))
                ModelState.AddModelError("Form.Username", "نام کاربری را وارد کنید.");

            if (string.IsNullOrWhiteSpace(Form.From))
                ModelState.AddModelError("Form.From", "شماره خط را وارد کنید.");

            if (recipients.Count == 0)
                ModelState.AddModelError("Form.RecipientNumbers", "حداقل یک دریافت‌کننده وارد کنید.");

            if (!HasSavedPassword && string.IsNullOrWhiteSpace(Form.Password))
                ModelState.AddModelError("Form.Password", "رمز را وارد کنید.");
        }

        if (string.IsNullOrWhiteSpace(Form.PaymentLinkTemplate))
        {
            ModelState.AddModelError("Form.PaymentLinkTemplate", "متن پیامک لینک پرداخت را وارد کنید.");
        }
        else if (!Form.PaymentLinkTemplate.Contains("{link}", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.PaymentLinkTemplate", "قالب پیامک باید شامل {link} باشد تا لینک پرداخت برای مشتری ارسال شود.");
        }

        if (Form.MessageFilterEnabled && string.IsNullOrWhiteSpace(Form.MessageFilterTerms))
            ModelState.AddModelError("Form.MessageFilterTerms", "برای فعال بودن فیلتر، حداقل یک کلمه یا عبارت وارد کنید.");

        if (!ModelState.IsValid)
            return Page();

        settings.Enabled = Form.Enabled;
        settings.Username = Form.Username?.Trim() ?? string.Empty;
        settings.From = Form.From?.Trim() ?? string.Empty;
        settings.RecipientNumbers = string.Join(Environment.NewLine, recipients);
        settings.PaymentLinkTemplate = Form.PaymentLinkTemplate;
        settings.MessageFilterEnabled = Form.MessageFilterEnabled;
        settings.MessageFilterTerms = Form.MessageFilterTerms ?? string.Empty;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(Form.Password))
            settings.EncryptedPassword = _passwordProtector.Protect(Form.Password.Trim());

        await db.SaveChangesAsync();
        TempData["Success"] = "تنظیمات پیامک ذخیره شد.";
        return RedirectToPage();
    }

    private async Task<SmsSettings> GetSettingsAsync()
    {
        var settings = await db.SmsSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings is not null)
            return settings;

        settings = new SmsSettings
        {
            PaymentLinkTemplate = DefaultPaymentLinkTemplate,
            MessageFilterEnabled = true,
            MessageFilterTerms = SmsSettings.DefaultMessageFilterTerms
        };

        db.SmsSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private static List<string> Parse(string? value) =>
        (value ?? string.Empty)
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();
}
