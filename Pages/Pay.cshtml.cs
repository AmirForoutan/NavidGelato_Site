using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Api;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages;

public sealed class PayModel(AppDbContext db, ISepPaymentGateway gateway) : PageModel
{
    public string State { get; private set; } = "loading";
    public string Message { get; private set; } = string.Empty;
    public long? Amount { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    public async Task<IActionResult> OnGetAsync(string token, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

        var payment = await db.BranchPaymentLinks
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.PublicToken == token, cancellationToken);

        if (payment is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            State = "not-found";
            Message = "این لینک پرداخت پیدا نشد.";
            return Page();
        }

        Amount = payment.Amount;

        if (payment.Status == PaymentLinkStatus.Paid)
        {
            State = "paid";
            Message = "این پرداخت قبلاً با موفقیت انجام شده است.";
            return Page();
        }

        if (payment.Branch is null || !payment.Branch.IsActive)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            State = "not-found";
            Message = "این لینک پرداخت در دسترس نیست.";
            return Page();
        }

        var now = DateTime.UtcNow;

        if (!payment.OpenedAtUtc.HasValue)
        {
            payment.OpenedAtUtc = now;
            payment.ExpiresAtUtc = now.AddMinutes(15);
            payment.Status = PaymentLinkStatus.Opened;
            await db.SaveChangesAsync(cancellationToken);
        }

        ExpiresAtUtc = payment.ExpiresAtUtc;

        if (payment.ExpiresAtUtc.HasValue && payment.ExpiresAtUtc.Value <= now)
        {
            payment.Status = PaymentLinkStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);
            Response.StatusCode = StatusCodes.Status410Gone;
            State = "expired";
            Message = "مهلت ۱۵ دقیقه‌ای این لینک پرداخت به پایان رسیده است.";
            return Page();
        }

        var iban = PaymentLinkEndpoints.NormalizeIban(payment.SettlementIban);
        if (iban is null)
        {
            State = "gateway-error";
            Message = "شماره شبای این شعبه برای پرداخت تنظیم نشده است.";
            return Page();
        }

        var settings = await db.PaymentGatewaySettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null || !settings.Enabled || string.IsNullOrWhiteSpace(settings.TerminalId))
        {
            State = "gateway-error";
            Message = "درگاه پرداخت در حال حاضر فعال نیست.";
            return Page();
        }

        if (payment.Status == PaymentLinkStatus.Failed)
        {
            State = "failed";
            Message = payment.FailureMessage ?? "این تلاش پرداخت ناموفق بوده است. برای پرداخت مجدد باید لینک تازه‌ای ساخته شود.";
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(payment.GatewayToken))
            return Redirect(gateway.BuildPaymentUrl(settings, payment.GatewayToken));

        var requestId = Guid.NewGuid().ToString("N");
        var staleBefore = now.AddSeconds(-45);

        var claimed = await db.BranchPaymentLinks
            .Where(x => x.Id == payment.Id &&
                        x.Status != PaymentLinkStatus.Paid &&
                        x.GatewayToken == null &&
                        (x.GatewayRequestStartedAtUtc == null || x.GatewayRequestStartedAtUtc < staleBefore))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.GatewayRequestId, requestId)
                .SetProperty(x => x.GatewayRequestStartedAtUtc, now), cancellationToken);

        if (claimed == 0)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                await Task.Delay(250, cancellationToken);
                var current = await db.BranchPaymentLinks
                    .AsNoTracking()
                    .Where(x => x.Id == payment.Id)
                    .Select(x => new { x.Status, x.GatewayToken, x.ExpiresAtUtc })
                    .FirstAsync(cancellationToken);

                if (current.Status == PaymentLinkStatus.Paid)
                {
                    State = "paid";
                    Message = "پرداخت با موفقیت انجام شده است.";
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(current.GatewayToken))
                    return Redirect(gateway.BuildPaymentUrl(settings, current.GatewayToken));

                if (current.ExpiresAtUtc.HasValue && current.ExpiresAtUtc.Value <= DateTime.UtcNow)
                {
                    State = "expired";
                    Message = "مهلت این لینک پرداخت به پایان رسیده است.";
                    return Page();
                }
            }

            Response.Headers["Refresh"] = "2";
            State = "preparing";
            Message = "در حال آماده‌سازی درگاه پرداخت هستیم...";
            return Page();
        }

        db.ChangeTracker.Clear();
        payment = await db.BranchPaymentLinks
            .Include(x => x.Branch)
            .FirstAsync(x => x.Id == payment.Id, cancellationToken);

        var publicBaseUrl = PaymentLinkEndpoints.BuildPublicBaseUrl(settings.PublicBaseUrl, Request);
        var callbackUrl = $"{publicBaseUrl}/payment/callback";

        SepTokenResult tokenResult;
        try
        {
            tokenResult = await gateway.RequestTokenAsync(
                settings,
                payment,
                iban,
                callbackUrl,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            tokenResult = new SepTokenResult(false, null, null, "ارتباط با درگاه پرداخت برقرار نشد.");
        }

        if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Token))
        {
            await db.BranchPaymentLinks
                .Where(x => x.Id == payment.Id && x.GatewayRequestId == requestId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.GatewayRequestId, (string?)null)
                    .SetProperty(x => x.GatewayRequestStartedAtUtc, (DateTime?)null)
                    .SetProperty(x => x.FailureMessage, tokenResult.ErrorDescription)
                    .SetProperty(x => x.Status, PaymentLinkStatus.Opened), cancellationToken);

            State = "gateway-error";
            Message = tokenResult.ErrorDescription ?? "درگاه پرداخت پاسخ معتبری برنگرداند. دوباره تلاش کنید.";
            return Page();
        }

        var saved = await db.BranchPaymentLinks
            .Where(x => x.Id == payment.Id &&
                        x.GatewayRequestId == requestId &&
                        x.Status != PaymentLinkStatus.Paid)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.GatewayToken, tokenResult.Token)
                .SetProperty(x => x.GatewayRequestId, (string?)null)
                .SetProperty(x => x.GatewayRequestStartedAtUtc, (DateTime?)null)
                .SetProperty(x => x.FailureMessage, (string?)null)
                .SetProperty(x => x.Status, PaymentLinkStatus.GatewayReady), cancellationToken);

        if (saved == 0)
        {
            State = "gateway-error";
            Message = "وضعیت لینک پرداخت تغییر کرده است. صفحه را دوباره باز کنید.";
            return Page();
        }

        return Redirect(gateway.BuildPaymentUrl(settings, tokenResult.Token));
    }
}
