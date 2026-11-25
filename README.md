# Navid Gelato Site

Independent ASP.NET Core Razor Pages website derived from NavidPro_Site. Branding: Navid Gelato. Main branch: main.

## Setup
Install .NET 10 SDK and SQL Server. Set `ConnectionStrings__DefaultConnection` to an **independent Gelato database**. Set `NAVID_GELATO_ADMIN_PASSWORD` to a unique strong password before the first run. Then run `dotnet restore` and `dotnet run`.

Admin: `/Admin/Login` (username `admin`, password from the environment variable). User is forced to change password.

## Content
Products, prices, branch locations and contact numbers must be entered through admin. No Navid Pro seed records are imported. Images currently in `wwwroot/img` are generic SVG concepts, not claimed to be official photos or logos.

## Before launch
Configure payment provider and SMS gateways independently. Set the production domain and SEO metadata. Audit all carried-over integrations for original brand references. Obtain official Navid Gelato logo, colors and product photos for exact Instagram matching. Build and deployment have not been independently verified.
