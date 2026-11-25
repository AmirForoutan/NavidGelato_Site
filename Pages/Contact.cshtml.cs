using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages;

public sealed class ContactModel(
    IContactSmsService smsService,
    AppDbContext db,
    ILogger<ContactModel> logger) : PageModel
{
    [BindProperty]
    public ContactInput Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var name = Input.Name.Trim();
        var mobile = string.IsNullOrWhiteSpace(Input.Mobile) ? null : Input.Mobile.Trim();
        var message = Input.Message.Trim();

        try
        {
            await smsService.SendContactRequestAsync(name, mobile, message, cancellationToken);
            SuccessMessage = "درخواست شما با موفقیت ارسال شد. ممنون که با نوید ژلاتو در ارتباط هستید.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deliver contact request from {Name}.", name);
            await TryLogErrorAsync(ex, name, mobile, message);
            ErrorMessage = "ارسال درخواست انجام نشد. لطفاً چند لحظه دیگر دوباره تلاش کنید.";
            return Page();
        }
    }

    private async Task TryLogErrorAsync(Exception exception, string name, string? mobile, string message)
    {
        try
        {
            var deliveryException = exception as ContactDeliveryException;
            var details = !string.IsNullOrWhiteSpace(deliveryException?.TechnicalDetails)
                ? $"{deliveryException.TechnicalDetails}{Environment.NewLine}{Environment.NewLine}{exception}"
                : exception.ToString();

            db.ContactErrorLogs.Add(new ContactErrorLog
            {
                RequestName = name,
                RequestMobile = mobile,
                RequestMessage = message,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                ErrorMessage = Limit(exception.Message, 2000),
                TechnicalDetails = details,
                IntendedSmsText = string.IsNullOrWhiteSpace(deliveryException?.IntendedSmsText)
                    ? null
                    : Limit(deliveryException.IntendedSmsText, 4000),
                RequestPath = Limit(HttpContext.Request.Path.Value ?? "/Contact", 300),
                OccurredAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception logException)
        {
            logger.LogError(logException, "Could not persist the contact delivery error in ContactErrorLogs.");
        }
    }

    private static string Limit(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    public sealed class ContactInput
    {
        [Required(ErrorMessage = "نام را وارد کنید.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "نام باید بین ۲ تا ۸۰ کاراکتر باشد.")]
        public string Name { get; set; } = string.Empty;

        [RegularExpression(@"^(?:\+98|0098|98|0)?9\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string? Mobile { get; set; }

        [Required(ErrorMessage = "شرح درخواست را وارد کنید.")]
        [StringLength(1200, MinimumLength = 10, ErrorMessage = "شرح درخواست باید حداقل ۱۰ و حداکثر ۱۲۰۰ کاراکتر باشد.")]
        public string Message { get; set; } = string.Empty;
    }
}
