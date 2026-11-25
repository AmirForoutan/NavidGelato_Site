using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Api;

public static class PaymentLinkEndpoints
{
    public static IEndpointRouteBuilder MapPaymentLinkApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/payment-links")
            .WithTags("Branch Payment Links");

        group.MapPost("", CreateAsync)
            .WithName("CreateBranchPaymentLink")
            .Produces<CreatePaymentLinkResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/{token}/status", GetStatusAsync)
            .WithName("GetBranchPaymentLinkStatus")
            .Produces<PaymentLinkStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/p/{code}", OpenShortPaymentLinkAsync)
            .WithName("OpenShortPaymentLink")
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        [FromHeader(Name = "X-Security-Key")] string? securityKey,
        [FromBody] CreatePaymentLinkRequest requestBody,
        AppDbContext db,
        IContactSmsService smsService,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedKey = securityKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
            return Results.Unauthorized();

        if (requestBody.AmountRial <= 0)
            return Results.BadRequest(new { message = "مبلغ پرداخت باید بزرگ‌تر از صفر باشد." });

        var cellNumber = CleanCellNumber(requestBody.CellNumber);
        if (requestBody.SendSms && cellNumber is null)
            return Results.BadRequest(new { message = "برای ارسال پیامک لینک پرداخت، شماره موبایل معتبر الزامی است." });

        var branch = await db.Branches
            .AsNoTracking()
            .Where(x => x.IsActive && x.SecurityKey == normalizedKey)
            .Select(x => new { x.Id, x.Name, x.Iban })
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
            return Results.Unauthorized();

        var iban = NormalizeIban(branch.Iban);
        if (iban is null)
            return Results.Conflict(new { message = "برای این شعبه شماره شبای معتبر ثبت نشده است." });

        var settings = await db.PaymentGatewaySettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null || !settings.Enabled || string.IsNullOrWhiteSpace(settings.TerminalId))
            return Results.Conflict(new { message = "درگاه پرداخت سایت فعال یا تکمیل نشده است." });

        BranchPaymentLink? payment = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var token = GenerateToken();
            var shortCode = GenerateShortCode();
            payment = new BranchPaymentLink
            {
                BranchId = branch.Id,
                PublicToken = token,
                ShortCode = shortCode,
                ResNum = $"NP{token[..30]}",
                Amount = requestBody.AmountRial,
                SettlementIban = iban,
                CellNumber = cellNumber,
                Status = PaymentLinkStatus.Created,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.BranchPaymentLinks.Add(payment);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException) when (attempt < 2)
            {
                db.Entry(payment).State = EntityState.Detached;
                payment = null;
            }
        }

        if (payment is null || payment.Id <= 0)
            return Results.Problem("امکان ساخت شناسه یکتای پرداخت فراهم نشد.");

        var publicBaseUrl = BuildPublicBaseUrl(settings.PublicBaseUrl, request);
        var paymentUrl = $"{publicBaseUrl}/pay/{payment.PublicToken}";
        var shortPaymentUrl = $"{publicBaseUrl}/p/{payment.ShortCode}";
        var statusUrl = $"{publicBaseUrl}/api/payment-links/{payment.PublicToken}/status";

        var smsSent = false;
        string? smsMessage = null;

        if (requestBody.SendSms)
        {
            try
            {
                await smsService.SendPaymentLinkAsync(
                    cellNumber!,
                    shortPaymentUrl,
                    payment.Amount,
                    cancellationToken);

                smsSent = true;
                smsMessage = "پیامک لینک پرداخت با موفقیت ارسال شد.";
            }
            catch (ContactDeliveryException ex)
            {
                smsMessage = ex.Message;
            }
            catch (HttpRequestException)
            {
                smsMessage = "ارتباط با سرویس پیامک برقرار نشد.";
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                smsMessage = "ارسال پیامک در زمان مقرر پاسخ نگرفت.";
            }
        }

        return Results.Ok(new CreatePaymentLinkResponse(
            payment.PublicToken,
            paymentUrl,
            statusUrl,
            payment.Amount,
            "waiting_to_open",
            payment.CreatedAtUtc,
            requestBody.SendSms,
            smsSent,
            smsMessage));
    }

    private static async Task<IResult> OpenShortPaymentLinkAsync(
        string code,
        AppDbContext db,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await db.BranchPaymentLinks
            .AsNoTracking()
            .Where(x => x.ShortCode == code)
            .Select(x => new { x.PublicToken })
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
            return Results.NotFound();

        var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;
        return Results.Redirect($"{pathBase}/pay/{payment.PublicToken}");
    }

    private static async Task<IResult> GetStatusAsync(
        string token,
        [FromHeader(Name = "X-Security-Key")] string? securityKey,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedKey = securityKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
            return Results.Unauthorized();

        var branchId = await db.Branches
            .AsNoTracking()
            .Where(x => x.IsActive && x.SecurityKey == normalizedKey)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!branchId.HasValue)
            return Results.Unauthorized();

        var payment = await db.BranchPaymentLinks
            .FirstOrDefaultAsync(x => x.PublicToken == token && x.BranchId == branchId.Value, cancellationToken);

        if (payment is null)
            return Results.NotFound();

        var now = DateTime.UtcNow;
        var expired = payment.Status != PaymentLinkStatus.Paid &&
                      payment.ExpiresAtUtc.HasValue &&
                      payment.ExpiresAtUtc.Value <= now;

        if (expired && payment.Status != PaymentLinkStatus.Expired)
        {
            payment.Status = PaymentLinkStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new PaymentLinkStatusResponse(
            payment.PublicToken,
            GetStatusText(payment.Status),
            payment.OpenedAtUtc.HasValue,
            payment.Status == PaymentLinkStatus.Paid,
            payment.Status == PaymentLinkStatus.Expired,
            payment.Amount,
            payment.CreatedAtUtc,
            payment.OpenedAtUtc,
            payment.ExpiresAtUtc,
            payment.PaidAtUtc));
    }

    internal static string BuildPublicBaseUrl(string? configuredBaseUrl, HttpRequest request)
    {
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            return configuredBaseUrl.Trim().TrimEnd('/');

        return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
    }

    internal static string? NormalizeIban(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        const string arabic = "٠١٢٣٤٥٦٧٨٩";
        var normalized = value;
        for (var i = 0; i < 10; i++)
            normalized = normalized.Replace(persian[i], (char)('0' + i)).Replace(arabic[i], (char)('0' + i));

        normalized = Regex.Replace(normalized.ToUpperInvariant(), @"[\s-]", string.Empty);
        if (Regex.IsMatch(normalized, @"^\d{24}$")) normalized = "IR" + normalized;
        return Regex.IsMatch(normalized, @"^IR\d{24}$") ? normalized : null;
    }

    internal static string GetStatusText(PaymentLinkStatus status) => status switch
    {
        PaymentLinkStatus.Created => "waiting_to_open",
        PaymentLinkStatus.Opened => "waiting_for_gateway",
        PaymentLinkStatus.GatewayReady => "waiting_for_payment",
        PaymentLinkStatus.VerificationPending => "verifying",
        PaymentLinkStatus.Paid => "paid",
        PaymentLinkStatus.Failed => "failed",
        PaymentLinkStatus.Expired => "expired",
        _ => "unknown"
    };

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static string GenerateShortCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(9);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? CleanCellNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = Regex.Replace(value, @"\D", string.Empty);
        return normalized.Length is >= 10 and <= 14 ? normalized : null;
    }
}

public sealed record CreatePaymentLinkRequest(
    long AmountRial,
    string? CellNumber,
    bool SendSms = false);

public sealed record CreatePaymentLinkResponse(
    string PaymentKey,
    string PaymentUrl,
    string StatusUrl,
    long AmountRial,
    string Status,
    DateTime CreatedAtUtc,
    bool SmsRequested,
    bool SmsSent,
    string? SmsMessage);

public sealed record PaymentLinkStatusResponse(
    string PaymentKey,
    string Status,
    bool Opened,
    bool Paid,
    bool Expired,
    long AmountRial,
    DateTime CreatedAtUtc,
    DateTime? OpenedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? PaidAtUtc);
