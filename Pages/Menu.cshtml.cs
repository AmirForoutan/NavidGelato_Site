using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Models;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages;

public class MenuModel(SiteContentService content) : PageModel
{
    public IReadOnlyList<MenuCategory> Categories { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Categories = await content.GetMenuCategoriesAsync();
    }
}
