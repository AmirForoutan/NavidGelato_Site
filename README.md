# Navid Gelato Site

Independent ASP.NET Core Razor Pages website derived from NavidPro_Site. Main branch: `main`.

## Run locally

1. Install .NET 10 SDK and SQL Server.
2. Set `ConnectionStrings__DefaultConnection` to a **separate** Navid Gelato database.
3. Run `dotnet restore` and `dotnet run` in Development.
4. Open **`https://localhost:<port>/Admin/Setup`** (or `http://localhost:<port>/Admin/Setup`) from the **same machine**. Choose a unique strong password for the `admin` account.
5. Sign in through `/Admin/Login` with username `admin` and the password you just created.

Setup exists only when `ASPNETCORE_ENVIRONMENT=Development`, the connection comes from a loopback address, and the initial admin has not yet been created. It is unavailable in production. The site starts even if the admin has not been bootstrapped, but admin login is not possible until setup is completed.

## Production admin bootstrap

On the **first startup**, set `NAVID_GELATO_ADMIN_PASSWORD` in the application's secure environment (not source control), with at least eight characters and uppercase, lowercase, number, and symbol. On successful creation the admin must change that initial password at login. The bootstrap value is only used when no `admin` account exists; it does not overwrite existing credentials.

If the admin account already exists, the variable is not necessary. When the variable is absent and no admin exists, only a warning is logged; the public website continues to start.

## Content

Products, prices, branches and contact details are entered via the admin panel. No Navid Pro seed products/branches are copied. Current SVG images are temporary brand concepts, not verified official Instagram assets.

## Before launch

Configure the production domain, payment/SMS credentials and a dedicated database. Review the inherited integrations and replace temporary brand assets. Never publish a development environment, share the administrator password, or commit it to the repository.
