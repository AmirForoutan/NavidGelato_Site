using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Pages.Admin;

public class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();
        HttpContext.Session.Clear();
        return RedirectToPage("/Admin/Login");
    }
}
