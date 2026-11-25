using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;

namespace NavidGelato_Site.Pages.Admin;

public sealed class PaymentSettingsModel(AppDbContext db, IAdminPermissionService permissions) : PageModel
{
    [BindProperty]
    public PaymentSettingsForm Form { get; set; } = new();

    public sealed class PaymentSettingsForm
    {
        public bool Enabled { get; set; }

        [MaxLength(64)]
        public string? TerminalId { get; set; }

        [Required, MaxLength(1000)]
        public string TokenUrl { get; set; } = "https://sep.shaparak.ir/onlinepg/onlinepg";

        [Required, MaxLength(1000)]
        public string PaymentUrlTemplate { get; set; } = "https://sep.shaparak.ir/OnlinePG/SendToken?token={token}";

        [Required, MaxLength(1000)]
        public string VerifyUrl { get; set; } = "https://sep.shaparak.ir/verifyTxnRandomSessionkey/ipg/VerifyTransaction";

        [MaxLength(500)]
        public string? PublicBaseUrl { get; set; }

        [Required, MaxLength(30)]
        public string DefaultPurchaseId { get; set; } = "000000000000000000000000000000";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Payments.View)) return Forbid();

        var settings = await db.PaymentGatewaySettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings is not null)
        {
            Form = new PaymentSettingsForm
            {
                Enabled = settings.Enabled,
                TerminalId = settings.TerminalId,
                TokenUrl = settings.TokenUrl,
                PaymentUrlTemplate = settings.PaymentUrlTemplate,
                VerifyUrl = settings.VerifyUrl,
                PublicBaseUrl = settings.PublicBaseUrl,
                DefaultPurchaseId = settings.DefaultPurchaseId
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Payments.Edit)) return Forbid();

        Form.TerminalId = Clean(Form.TerminalId);
        Form.PublicBaseUrl = Clean(Form.PublicBaseUrl)?.TrimEnd('/');
        Form.TokenUrl = (Form.TokenUrl ?? string.Empty).Trim();
        Form.PaymentUrlTemplate = (Form.PaymentUrlTemplate ?? string.Empty).Trim();
        Form.VerifyUrl = (Form.VerifyUrl ?? string.Empty).Trim();
        Form.DefaultPurchaseId = (Form.DefaultPurchaseId ?? string.Empty).Trim();

        if (Form.Enabled && string.IsNullOrWhiteSpace(Form.TerminalId))
            ModelState.AddModelError("Form.TerminalId", "برای فعال کردن درگاه، شماره ترمینال الزامی است.");

        ValidateAbsoluteUrl(Form.TokenUrl, "Form.TokenUrl", "آدرس دریافت توکن معتبر نیست.");
        ValidateAbsoluteUrl(Form.PaymentUrlTemplate.Replace("{token}", "sample-token", StringComparison.OrdinalIgnoreCase), "Form.PaymentUrlTemplate", "آدرس هدایت به درگاه معتبر نیست.");
        ValidateAbsoluteUrl(Form.VerifyUrl, "Form.VerifyUrl", "آدرس تایید تراکنش معتبر نیست.");

        if (Form.PublicBaseUrl is not null)
            ValidateAbsoluteUrl(Form.PublicBaseUrl, "Form.PublicBaseUrl", "آدرس عمومی سایت معتبر نیست.");

        if (string.IsNullOrWhiteSpace(Form.DefaultPurchaseId))
            ModelState.AddModelError("Form.DefaultPurchaseId", "شناسه تسویه را وارد کنید.");

        if (!ModelState.IsValid) return Page();

        var settings = await db.PaymentGatewaySettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new PaymentGatewaySettings();
            db.PaymentGatewaySettings.Add(settings);
        }

        settings.Enabled = Form.Enabled;
        settings.TerminalId = Form.TerminalId ?? string.Empty;
        settings.TokenUrl = Form.TokenUrl;
        settings.PaymentUrlTemplate = Form.PaymentUrlTemplate;
        settings.VerifyUrl = Form.VerifyUrl;
        settings.PublicBaseUrl = Form.PublicBaseUrl;
        settings.DefaultPurchaseId = Form.DefaultPurchaseId;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        TempData["Success"] = "تنظیمات درگاه پرداخت ذخیره شد.";
        return RedirectToPage();
    }

    private void ValidateAbsoluteUrl(string value, string key, string error)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            ModelState.AddModelError(key, error);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
