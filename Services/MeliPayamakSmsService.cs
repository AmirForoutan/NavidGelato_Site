using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;

namespace NavidGelato_Site.Services;

public interface IContactSmsService
{
    Task SendContactRequestAsync(string name, string? mobile, string message, CancellationToken cancellationToken = default);
    Task SendFranchiseRequestAsync(string name, string mobile, string? proposedLocation, string? message, CancellationToken cancellationToken = default);
    Task SendPaymentLinkAsync(string mobile, string paymentUrl, long amountRial, CancellationToken cancellationToken = default);
}

public sealed class ContactDeliveryException : Exception
{
    public ContactDeliveryException(
        string message,
        string? technicalDetails = null,
        Exception? innerException = null,
        string? intendedSmsText = null)
        : base(message, innerException)
    {
        TechnicalDetails = technicalDetails;
        IntendedSmsText = intendedSmsText;
    }

    public string? TechnicalDetails { get; }
    public string? IntendedSmsText { get; }
}

public sealed class MeliPayamakSmsService(
    HttpClient httpClient,
    AppDbContext db,
    IDataProtectionProvider dataProtectionProvider,
    IMessageContentFilter contentFilter,
    ILogger<MeliPayamakSmsService> logger) : IContactSmsService
{
    private const string Endpoint = "https://rest.payamak-panel.com/api/SendSMS/SendSMS";
    private readonly IDataProtector _passwordProtector = dataProtectionProvider.CreateProtector("NavidPro.MeliPayamak.Password.v1");

    public async Task SendContactRequestAsync(string name, string? mobile, string message, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var safeName = contentFilter.Filter(name, settings.MessageFilterEnabled, settings.MessageFilterTerms);
        var safeMessage = contentFilter.Filter(message, settings.MessageFilterEnabled, settings.MessageFilterTerms);
        var smsText = $"درخواست جدید سایت نوید ژلاتو\nنام: {safeName}\nشماره تماس: {mobile ?? "ثبت نشده"}\nشرح: {safeMessage}";
        await SendAsync(smsText, cancellationToken, loadedSettings: settings);
    }

    public async Task SendFranchiseRequestAsync(string name, string mobile, string? proposedLocation, string? message, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var safeName = contentFilter.Filter(name, settings.MessageFilterEnabled, settings.MessageFilterTerms);
        var safeLocation = string.IsNullOrWhiteSpace(proposedLocation)
            ? "ثبت نشده"
            : contentFilter.Filter(proposedLocation, settings.MessageFilterEnabled, settings.MessageFilterTerms);
        var safeMessage = string.IsNullOrWhiteSpace(message)
            ? "ثبت نشده"
            : contentFilter.Filter(message, settings.MessageFilterEnabled, settings.MessageFilterTerms);

        var smsText = $"درخواست جدید فرانچایز نوید ژلاتو\nنام: {safeName}\nشماره تماس: {mobile}\nمحل پیشنهادی: {safeLocation}\nشرح: {safeMessage}";
        await SendAsync(smsText, cancellationToken, loadedSettings: settings);
    }

    public async Task SendPaymentLinkAsync(string mobile, string paymentUrl, long amountRial, CancellationToken cancellationToken = default)
    {
        var recipient = NormalizeMobile(mobile);
        if (recipient is null)
            throw new ContactDeliveryException("شماره موبایل مشتری برای ارسال لینک پرداخت معتبر نیست.");

        var settings = await GetSettingsAsync(cancellationToken);

        var template = string.IsNullOrWhiteSpace(settings.PaymentLinkTemplate)
            ? "نوید ژلاتو\nلینک پرداخت {amount} ریال:\n{link}\nمهلت پرداخت: ۱۵ دقیقه پس از اولین باز شدن لینک"
            : settings.PaymentLinkTemplate;

        if (!template.Contains("{link}", StringComparison.OrdinalIgnoreCase))
            template += "\n{link}";

        var smsText = template
            .Replace("{amount}", amountRial.ToString("N0", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{link}", paymentUrl, StringComparison.OrdinalIgnoreCase)
            .Replace("{mobile}", recipient, StringComparison.OrdinalIgnoreCase);

        await SendAsync(smsText, cancellationToken, [recipient], settings);
    }

    private async Task<NavidGelato_Site.Models.SmsSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        await db.SmsSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken)
        ?? throw new ContactDeliveryException(
            "تنظیمات ملی پیامک هنوز در پنل مدیریت ثبت نشده است.",
            "SmsSettings record was not found in the database.");

    private async Task SendAsync(
        string smsText,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? directRecipients = null,
        NavidGelato_Site.Models.SmsSettings? loadedSettings = null)
    {
        var settings = loadedSettings
            ?? await db.SmsSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ContactDeliveryException("تنظیمات ملی پیامک هنوز در پنل مدیریت ثبت نشده است.", "SmsSettings record was not found in the database.");

        if (!settings.Enabled)
            throw new ContactDeliveryException(
                "سرویس ملی پیامک در پنل مدیریت غیرفعال است.",
                "ارسال واقعی انجام نشد چون SmsSettings.Enabled خاموش است. متن زیر فقط پیش‌نمایش خروجی نهایی پس از اعمال فیلتر محتواست.",
                intendedSmsText: smsText);

        var recipients = directRecipients ?? ParseRecipients(settings.RecipientNumbers);
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.EncryptedPassword) ||
            string.IsNullOrWhiteSpace(settings.From) || recipients.Count == 0)
            throw new ContactDeliveryException(
                "تنظیمات ملی پیامک کامل نیست.",
                directRecipients is null
                    ? "One or more required SMS settings are empty: Username, Password, From or RecipientNumbers."
                    : "One or more required SMS settings are empty: Username, Password or From.");

        string password;
        try
        {
            password = _passwordProtector.Unprotect(settings.EncryptedPassword);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not decrypt MeliPayamak password.");
            throw new ContactDeliveryException(
                "رمز سرویس پیامک قابل بازیابی نیست. رمز را دوباره از پنل مدیریت ذخیره کنید.",
                $"Data Protection could not decrypt the saved SMS password. {ex.GetType().FullName}: {ex.Message}",
                ex);
        }

        foreach (var recipient in recipients)
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = settings.Username,
                ["password"] = password,
                ["to"] = recipient,
                ["from"] = settings.From,
                ["text"] = smsText,
                ["isFlash"] = "false"
            });

            using var response = await httpClient.PostAsync(Endpoint, content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("MeliPayamak HTTP error {StatusCode}: {Payload}", response.StatusCode, payload);
                throw new ContactDeliveryException(
                    "ارسال پیامک با خطا مواجه شد.",
                    $"MeliPayamak HTTP {(int)response.StatusCode} ({response.StatusCode}). Recipient: {recipient}. Response: {Limit(payload)}");
            }

            try
            {
                using var json = JsonDocument.Parse(payload);
                if (json.RootElement.TryGetProperty("RetStatus", out var status) && status.TryGetInt32(out var result) && result != 1)
                {
                    logger.LogError("MeliPayamak returned an unsuccessful response: {Payload}", payload);
                    throw new ContactDeliveryException(
                        "ملی پیامک درخواست را نپذیرفت.",
                        $"MeliPayamak returned RetStatus={result}. Recipient: {recipient}. Response: {Limit(payload)}");
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Could not parse MeliPayamak response as JSON: {Payload}", payload);
            }
        }
    }

    private static string? NormalizeMobile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0098", StringComparison.Ordinal))
            digits = "0" + digits[4..];
        else if (digits.StartsWith("98", StringComparison.Ordinal) && digits.Length == 12)
            digits = "0" + digits[2..];
        else if (digits.StartsWith("9", StringComparison.Ordinal) && digits.Length == 10)
            digits = "0" + digits;

        return digits.Length == 11 && digits.StartsWith("09", StringComparison.Ordinal)
            ? digits
            : null;
    }

    private static string Limit(string value) => value.Length <= 4000 ? value : value[..4000];

    private static List<string> ParseRecipients(string value) => value
        .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct()
        .ToList();
}
