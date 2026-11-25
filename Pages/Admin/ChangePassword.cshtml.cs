using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Pages.Admin;

public class ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool RequiredChange { get; private set; }

    public sealed class InputModel
    {
        [Required] public string CurrentPassword { get; set; } = string.Empty;
        [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
        [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        RequiredChange = user?.MustChangePassword == true;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        RequiredChange = user.MustChangePassword;
        if (!ModelState.IsValid) return Page();

        var result = await userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "رمز عبور با موفقیت تغییر کرد.";
        return RedirectToPage("/Admin/Index");
    }
}
