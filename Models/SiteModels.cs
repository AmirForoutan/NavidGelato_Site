namespace NavidGelato_Site.Models;

public sealed record BranchInfo(
    string Name,
    string City,
    string Address,
    IReadOnlyList<string> Phones,
    bool IsCentral = false)
{
    public string SearchQuery => $"نوید ژلاتو {Name} {Address}";
    public string EncodedSearchQuery => Uri.EscapeDataString(SearchQuery);
    public string GoogleMapsUrl => $"https://www.google.com/maps/search/?api=1&query={EncodedSearchQuery}";
    public string WazeUrl => $"https://www.waze.com/ul?q={EncodedSearchQuery}&navigate=yes";
    public string BaladUrl => $"https://balad.ir/search?query={EncodedSearchQuery}";
    public string NeshanUrl => $"https://neshan.org/maps/search/{EncodedSearchQuery}";
}

public sealed record MenuItemInfo(
    string Name,
    string Category,
    int Price,
    string ImageUrl,
    string? Description = null,
    bool IsFeatured = false)
{
    public string PriceText => Price.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
}

public sealed record MenuCategoryInfo(string Id, string Title, string Subtitle, IReadOnlyList<MenuItemInfo> Items);
