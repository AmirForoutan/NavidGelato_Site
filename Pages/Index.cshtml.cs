using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Models;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages;

public class IndexModel(SiteContentService content) : PageModel
{
    public IReadOnlyList<Branch> Branches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Branches = await content.GetBranchesAsync();
    }
}
