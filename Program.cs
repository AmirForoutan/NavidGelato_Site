using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Api;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;
using NavidGelato_Site.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("NavidGelato_Site")
    .PersistKeysToDbContext<AppDbContext>();

if (OperatingSystem.IsWindows())
{
    dataProtectionBuilder.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Login";
    options.AccessDeniedPath = "/Admin/AccessDenied";
    options.Cookie.Name = "NavidGelato.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(2));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AllowAnonymousToPage("/Admin/Setup");
    options.Conventions.AllowAnonymousToPage("/Admin/AccessDenied");
});

builder.Services.AddScoped<SiteContentService>();
builder.Services.AddSingleton<OfflineMapTileService>();
builder.Services.AddScoped<IAdminPermissionService, AdminPermissionService>();
builder.Services.AddSingleton<IMessageContentFilter, MessageContentFilter>();
builder.Services.AddHttpClient<IContactSmsService, MeliPayamakSmsService>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient<ISepPaymentGateway, SepPaymentGateway>(client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "NavidGelato.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(20);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true && context.Request.Path.StartsWithSegments("/Admin"))
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var exempt = path.Equals("/Admin/Logout", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Admin/ChangePassword", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Admin/AccessDenied", StringComparison.OrdinalIgnoreCase);

        if (!exempt)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
            var user = await userManager.GetUserAsync(context.User);
            if (user is null || !user.IsEnabled)
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Admin/Login");
                return;
            }
            if (user.MustChangePassword)
            {
                context.Response.Redirect("/Admin/ChangePassword");
                return;
            }
        }
    }
    await next();
});

app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "Navid Gelato API v1");
    options.DocumentTitle = "Navid Gelato API";
});

await DatabaseSeeder.InitializeAsync(app.Services);
await SeedCompletenessRepair.EnsureAsync(app.Services);
await ProductCodeBackfill.EnsureAsync(app.Services);
app.MapStaticAssets();
app.MapCatalogApi();
app.MapPaymentLinkApi();
app.MapOfflineMapApi();
app.MapRazorPages().WithStaticAssets();
app.Run();
