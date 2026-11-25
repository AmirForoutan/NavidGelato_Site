using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Pages.Admin;

public class LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty] public LoginInput Input { get; set; } = new();
    public string CaptchaQuestion { get; private set; } = string.Empty;
    public DateTimeOffset? LockoutEnd { get; private set; }

    public class LoginInput
    {
        [Required(ErrorMessage = "نام کاربری را وارد کنید.")] public string UserName { get; set; } = string.Empty;
        [Required(ErrorMessage = "رمز عبور را وارد کنید.")] public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "کد امنیتی را وارد کنید.")] public string CaptchaAnswer { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Admin/Index");
        await signInManager.SignOutAsync();
        GenerateCaptcha();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var expectedCaptcha = HttpContext.Session.GetString("AdminCaptchaAnswer");
        if (string.IsNullOrWhiteSpace(expectedCaptcha) || !string.Equals(expectedCaptcha, Input.CaptchaAnswer?.Trim(), StringComparison.Ordinal))
            ModelState.AddModelError(string.Empty, "کد امنیتی اشتباه است.");

        if (!ModelState.IsValid) { GenerateCaptcha(); return Page(); }

        var user = await userManager.FindByNameAsync(Input.UserName.Trim());
        if (user is null || !user.IsEnabled)
        {
            ModelState.AddModelError(string.Empty, "نام کاربری یا رمز عبور صحیح نیست.");
            GenerateCaptcha();
            return Page();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            LockoutEnd = await userManager.GetLockoutEndDateAsync(user);
            ModelState.AddModelError(string.Empty, "حساب کاربری شما موقتاً قفل شده است.");
            GenerateCaptcha();
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(user, Input.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            LockoutEnd = await userManager.GetLockoutEndDateAsync(user);
            ModelState.AddModelError(string.Empty, "حساب کاربری شما به دلیل چند ورود ناموفق موقتاً قفل شد.");
        }
        else if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "نام کاربری یا رمز عبور صحیح نیست.");
        }

        if (!result.Succeeded) { GenerateCaptcha(); return Page(); }

        HttpContext.Session.Remove("AdminCaptchaAnswer");
        if (user.MustChangePassword) return RedirectToPage("/Admin/ChangePassword");
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/Admin" : returnUrl);
    }

    private void GenerateCaptcha()
    {
        var a = Random.Shared.Next(2, 10);
        var b = Random.Shared.Next(1, 10);
        HttpContext.Session.SetString("AdminCaptchaAnswer", (a + b).ToString());
        CaptchaQuestion = $"{a} + {b} = ؟";
    }
}
