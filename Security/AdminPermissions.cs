namespace NavidGelato_Site.Security;

public sealed record AdminPermissionDefinition(string Key, string Group, string Title);

public static class AdminPermissions
{
    public const string ClaimType = "NavidPro.Permission";
    public static class Dashboard { public const string View = "Dashboard.View"; }
    public static class Categories { public const string View = "Categories.View"; public const string Create = "Categories.Create"; public const string Edit = "Categories.Edit"; public const string Delete = "Categories.Delete"; }
    public static class Products { public const string View = "Products.View"; public const string Create = "Products.Create"; public const string Edit = "Products.Edit"; public const string Delete = "Products.Delete"; public const string ManageImages = "Products.ManageImages"; }
    public static class Branches { public const string View = "Branches.View"; public const string Create = "Branches.Create"; public const string Edit = "Branches.Edit"; public const string Delete = "Branches.Delete"; }
    public static class Sms { public const string View = "Sms.View"; public const string Edit = "Sms.Edit"; }
    public static class SiteSettings { public const string View = "SiteSettings.View"; public const string Edit = "SiteSettings.Edit"; }
    public static class Payments { public const string View = "Payments.View"; public const string Edit = "Payments.Edit"; }
    public static class PaymentReports { public const string View = "PaymentReports.View"; public const string ViewDetails = "PaymentReports.ViewDetails"; public const string Export = "PaymentReports.Export"; }
    public static class ContactErrors { public const string View = "ContactErrors.View"; public const string Delete = "ContactErrors.Delete"; }
    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string ManagePermissions = "Users.ManagePermissions";
        public const string ResetPassword = "Users.ResetPassword";
        public const string Unlock = "Users.Unlock";
    }

    public static IReadOnlyList<AdminPermissionDefinition> All { get; } =
    [
        new(Dashboard.View, "داشبورد", "مشاهده داشبورد"),
        new(Categories.View, "دسته‌بندی‌ها", "مشاهده دسته‌بندی‌ها"), new(Categories.Create, "دسته‌بندی‌ها", "ایجاد دسته‌بندی"), new(Categories.Edit, "دسته‌بندی‌ها", "ویرایش دسته‌بندی"), new(Categories.Delete, "دسته‌بندی‌ها", "حذف دسته‌بندی"),
        new(Products.View, "کالاها", "مشاهده کالاها"), new(Products.Create, "کالاها", "ایجاد کالا"), new(Products.Edit, "کالاها", "ویرایش کالا و قیمت"), new(Products.Delete, "کالاها", "حذف کالا"), new(Products.ManageImages, "کالاها", "مدیریت تصاویر کالا"),
        new(Branches.View, "شعب", "مشاهده شعب"), new(Branches.Create, "شعب", "ایجاد شعبه"), new(Branches.Edit, "شعب", "ویرایش شعبه"), new(Branches.Delete, "شعب", "حذف شعبه"),
        new(Sms.View, "پیامک", "مشاهده تنظیمات پیامک"), new(Sms.Edit, "پیامک", "ویرایش تنظیمات پیامک"),
        new(SiteSettings.View, "تنظیمات سایت", "مشاهده تنظیمات سایت و اینماد"), new(SiteSettings.Edit, "تنظیمات سایت", "ویرایش تنظیمات سایت و اینماد"),
        new(Payments.View, "درگاه پرداخت", "مشاهده تنظیمات درگاه پرداخت"), new(Payments.Edit, "درگاه پرداخت", "ویرایش تنظیمات درگاه پرداخت"),
        new(PaymentReports.View, "گزارش پرداخت‌ها", "مشاهده گزارش پرداخت‌ها"), new(PaymentReports.ViewDetails, "گزارش پرداخت‌ها", "مشاهده جزئیات بانکی و فنی پرداخت"), new(PaymentReports.Export, "گزارش پرداخت‌ها", "خروجی Excel گزارش پرداخت‌ها"),
        new(ContactErrors.View, "خطاهای ارسال", "مشاهده خطاهای ارسال فرم ارتباط"), new(ContactErrors.Delete, "خطاهای ارسال", "حذف خطاهای ثبت‌شده"),
        new(Users.View, "کاربران", "مشاهده کاربران"), new(Users.Create, "کاربران", "ایجاد کاربر"), new(Users.Edit, "کاربران", "ویرایش مشخصات و وضعیت کاربر"), new(Users.Delete, "کاربران", "حذف کاربر"), new(Users.ManagePermissions, "کاربران", "تعیین دسترسی‌های کاربران"), new(Users.ResetPassword, "کاربران", "تغییر رمز کاربران"), new(Users.Unlock, "کاربران", "رفع قفل حساب کاربران")
    ];

    public static IReadOnlyList<string> AllKeys { get; } = All.Select(x => x.Key).ToArray();

    private static IReadOnlyDictionary<string, string> PageViewPermissions { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Categories"] = Categories.View,
        ["Products"] = Products.View,
        ["Branches"] = Branches.View,
        ["Sms"] = Sms.View,
        ["SiteSettings"] = SiteSettings.View,
        ["PaymentSettings"] = Payments.View,
        ["PaymentReports"] = PaymentReports.View,
        ["ContactErrors"] = ContactErrors.View,
        ["Users"] = Users.View
    };

    public static HashSet<string> Normalize(IEnumerable<string> permissions)
    {
        var allowed = AllKeys.ToHashSet(StringComparer.Ordinal);
        var result = permissions.Where(allowed.Contains).ToHashSet(StringComparer.Ordinal);

        foreach (var permission in result.ToArray())
        {
            var separator = permission.IndexOf('.');
            if (separator <= 0) continue;
            var page = permission[..separator];
            if (PageViewPermissions.TryGetValue(page, out var viewPermission))
                result.Add(viewPermission);
        }

        return result;
    }
}
