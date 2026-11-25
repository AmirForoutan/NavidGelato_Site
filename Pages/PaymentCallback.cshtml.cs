using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages;

[IgnoreAntiforgeryToken]
public sealed class PaymentCallbackModel(AppDbContext db, ISepPaymentGateway gateway) : PageModel
{
    public string State { get; private set; } = "processing";
    public string Message { get; private set; } = "در حال بررسی نتیجه پرداخت...";
    public long? Amount { get; private set; }
    public string? TraceNo { get; private set; }

    public Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        ProcessAsync(cancellationToken);

    public Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) =>
        ProcessAsync(cancellationToken);

    private async Task<IActionResult> ProcessAsync(CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

        IFormCollection? form = null;
        if (Request.HasFormContentType)
            form = await Request.ReadFormAsync(cancellationToken);

        string? Value(string name)
        {
            var formValue = form?[name].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(formValue)) return formValue.Trim();

            var queryValue = Request.Query[name].FirstOrDefault();
            return string.IsNullOrWhiteSpace(queryValue) ? null : queryValue.Trim();
        }

        var resNum = Value("ResNum");
        var refNum = Value("RefNum");
        var callbackState = Value("State");
        var callbackStatus = Value("Status");
        var traceNo = Value("TraceNo") ?? Value("StraceNo");

        if (string.IsNullOrWhiteSpace(resNum))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            State = "failed";
            Message = "شناسه پرداخت در پاسخ درگاه وجود ندارد.";
            return Page();
        }

        var payment = await db.BranchPaymentLinks
            .FirstOrDefaultAsync(x => x.ResNum == resNum, cancellationToken);

        if (payment is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            State = "failed";
            Message = "تراکنش متناظر با پاسخ درگاه پیدا نشد.";
            return Page();
        }

        Amount = payment.Amount;
        TraceNo = payment.TraceNo ?? traceNo;

        if (payment.Status == PaymentLinkStatus.Paid)
        {
            State = "paid";
            Message = "پرداخت با موفقیت تایید شده است.";
            TraceNo = payment.TraceNo;
            return Page();
        }

        payment.LastCallbackAtUtc = DateTime.UtcNow;

        var stateOk = string.IsNullOrWhiteSpace(callbackState) ||
                      callbackState.Equals("OK", StringComparison.OrdinalIgnoreCase);
        var statusOk = string.IsNullOrWhiteSpace(callbackStatus) || callbackStatus == "2";

        if (string.IsNullOrWhiteSpace(refNum) || !stateOk || !statusOk)
        {
            payment.Status = PaymentLinkStatus.Failed;
            payment.FailureMessage = "پرداخت در درگاه تکمیل نشد یا توسط کاربر لغو شد.";
            payment.TraceNo = traceNo;
            await db.SaveChangesAsync(cancellationToken);

            State = "failed";
            Message = payment.FailureMessage;
            TraceNo = traceNo;
            return Page();
        }

        var duplicateRef = await db.BranchPaymentLinks
            .AsNoTracking()
            .AnyAsync(x => x.RefNum == refNum && x.Id != payment.Id, cancellationToken);

        if (duplicateRef)
        {
            payment.Status = PaymentLinkStatus.Failed;
            payment.FailureMessage = "رسید دیجیتال این تراکنش قبلاً برای پرداخت دیگری استفاده شده است.";
            await db.SaveChangesAsync(cancellationToken);

            State = "failed";
            Message = "امکان تایید این پرداخت وجود ندارد.";
            return Page();
        }

        payment.RefNum = refNum;
        payment.TraceNo = traceNo;
        payment.Status = PaymentLinkStatus.VerificationPending;
        await db.SaveChangesAsync(cancellationToken);

        var settings = await db.PaymentGatewaySettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null || string.IsNullOrWhiteSpace(settings.TerminalId))
        {
            State = "pending";
            Message = "پرداخت از درگاه برگشته اما تنظیمات تایید تراکنش در سایت کامل نیست. وضعیت پرداخت در انتظار بررسی است.";
            return Page();
        }

        SepVerifyResult verification;
        try
        {
            verification = await gateway.VerifyAsync(settings, refNum, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            State = "pending";
            Message = "پاسخ پرداخت دریافت شد اما ارتباط با سرویس تایید بانک برقرار نشد. وضعیت پرداخت هنوز قطعی نشده است.";
            return Page();
        }

        db.ChangeTracker.Clear();
        payment = await db.BranchPaymentLinks.FirstAsync(x => x.Id == payment.Id, cancellationToken);

        if (payment.Status == PaymentLinkStatus.Paid)
        {
            State = "paid";
            Message = "پرداخت با موفقیت تایید شده است.";
            TraceNo = payment.TraceNo;
            return Page();
        }

        if (!verification.Success)
        {
            payment.Status = PaymentLinkStatus.Failed;
            payment.FailureMessage = verification.ResultDescription ?? "تایید تراکنش توسط درگاه ناموفق بود.";
            await db.SaveChangesAsync(cancellationToken);

            State = "failed";
            Message = payment.FailureMessage;
            return Page();
        }

        if (!verification.Amount.HasValue || verification.Amount.Value != payment.Amount)
        {
            payment.Status = PaymentLinkStatus.Failed;
            payment.VerifiedAmount = verification.Amount;
            payment.FailureMessage = "مبلغ تاییدشده توسط درگاه با مبلغ لینک پرداخت مطابقت ندارد.";
            await db.SaveChangesAsync(cancellationToken);

            State = "failed";
            Message = "مبلغ تراکنش با مبلغ مورد انتظار مطابقت ندارد؛ پرداخت تایید نشد.";
            return Page();
        }

        payment.Status = PaymentLinkStatus.Paid;
        payment.VerifiedAmount = verification.Amount;
        payment.PaidAtUtc = DateTime.UtcNow;
        payment.FailureMessage = null;
        payment.TraceNo = traceNo;
        await db.SaveChangesAsync(cancellationToken);

        State = "paid";
        Message = "پرداخت با موفقیت انجام و توسط درگاه تایید شد.";
        TraceNo = traceNo;
        return Page();
    }
}
