using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages;

public sealed class FranchiseModel(
    IContactSmsService smsService,
    AppDbContext db,
    ILogger<FranchiseModel> logger) : PageModel
{
    [BindProperty]
    public FranchiseInput Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var name = Input.Name.Trim();
        var mobile = Input.Mobile.Trim();
        var location = string.IsNullOrWhiteSpace(Input.ProposedLocation) ? null : Input.ProposedLocation.Trim();
        var message = string.IsNullOrWhiteSpace(Input.Message) ? null : Input.Message.Trim();

        try
        {
            await smsService.SendFranchiseRequestAsync(name, mobile, location, message, cancellationToken);
            SuccessMessage = "درخواست فرانچایز شما با موفقیت ارسال شد. تیم نوید ژلاتو برای بررسی و هماهنگی با شما تماس می‌گیرد.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deliver franchise request from {Name}.", name);
            await TryLogErrorAsync(ex, name, mobile, location, message);
            ErrorMessage = "ارسال درخواست انجام نشد. لطفاً کمی بعد دوباره تلاش کنید یا با شماره 09122638537 تماس بگیرید.";
            return Page();
        }
    }

    private async Task TryLogErrorAsync(Exception exception, string name, string mobile, string? location, string? message)
    {
        try
        {
            var deliveryException = exception as ContactDeliveryException;
            var details = !string.IsNullOrWhiteSpace(deliveryException?.TechnicalDetails)
                ? $"{deliveryException.TechnicalDetails}{Environment.NewLine}{Environment.NewLine}{exception}"
                : exception.ToString();

            var requestMessage = $"درخواست فرانچایز | محل پیشنهادی: {location ?? "ثبت نشده"} | شرح: {message ?? "ثبت نشده"}";
            db.ContactErrorLogs.Add(new ContactErrorLog
            {
                RequestName = name,
                RequestMobile = mobile,
                RequestMessage = Limit(requestMessage, 1200),
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                ErrorMessage = Limit(exception.Message, 2000),
                TechnicalDetails = details,
                IntendedSmsText = string.IsNullOrWhiteSpace(deliveryException?.IntendedSmsText)
                    ? null
                    : Limit(deliveryException.IntendedSmsText, 4000),
                RequestPath = Limit(HttpContext.Request.Path.Value ?? "/Franchise", 300),
                OccurredAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception logException)
        {
            logger.LogError(logException, "Could not persist franchise delivery error in ContactErrorLogs.");
        }
    }

    private static string Limit(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    public sealed class FranchiseInput
    {
        [Required(ErrorMessage = "نام را وارد کنید.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "نام باید بین ۲ تا ۸۰ کاراکتر باشد.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره تماس را وارد کنید.")]
        [RegularExpression(@"^(?:\+98|0098|98|0)?9\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string Mobile { get; set; } = string.Empty;

        [StringLength(120, ErrorMessage = "محل پیشنهادی حداکثر ۱۲۰ کاراکتر باشد.")]
        public string? ProposedLocation { get; set; }

        [StringLength(1000, ErrorMessage = "شرح درخواست حداکثر ۱۰۰۰ کاراکتر باشد.")]
        public string? Message { get; set; }
    }
}
