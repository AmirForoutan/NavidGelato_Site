using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260904151000_UseAspNetCoreIdentity")]
public partial class UseAspNetCoreIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This migration intentionally uses conditional DDL so it can upgrade both:
        // 1) a fresh NavidPro database that has never used ASP.NET Core Identity, and
        // 2) an existing database where the standard AspNet* tables already exist.
        // Everything still runs through EF Core Migrations and is tracked in __EFMigrationsHistory.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[AspNetRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetRoles]
    (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[AspNetUsers]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUsers]
    (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(120) NOT NULL CONSTRAINT [DF_AspNetUsers_DisplayName] DEFAULT N'',
        [IsEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_IsEnabled] DEFAULT CAST(1 AS bit),
        [MustChangePassword] bit NOT NULL CONSTRAINT [DF_AspNetUsers_MustChangePassword] DEFAULT CAST(0 AS bit),
        [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'AspNetUsers', N'DisplayName') IS NULL
        ALTER TABLE [AspNetUsers] ADD [DisplayName] nvarchar(120) NOT NULL CONSTRAINT [DF_AspNetUsers_DisplayName] DEFAULT N'';

    IF COL_LENGTH(N'AspNetUsers', N'IsEnabled') IS NULL
        ALTER TABLE [AspNetUsers] ADD [IsEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_IsEnabled] DEFAULT CAST(1 AS bit);

    IF COL_LENGTH(N'AspNetUsers', N'MustChangePassword') IS NULL
        ALTER TABLE [AspNetUsers] ADD [MustChangePassword] bit NOT NULL CONSTRAINT [DF_AspNetUsers_MustChangePassword] DEFAULT CAST(0 AS bit);

    IF COL_LENGTH(N'AspNetUsers', N'CreatedAtUtc') IS NULL
        ALTER TABLE [AspNetUsers] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAtUtc] DEFAULT SYSUTCDATETIME();
END;

IF OBJECT_ID(N'[AspNetRoleClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetRoleClaims]
    (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
            FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserClaims]
    (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserLogins]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserLogins]
    (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserRoles]
    (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
            FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserTokens]
    (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_AspNetRoleClaims_RoleId' AND [object_id] = OBJECT_ID(N'[AspNetRoleClaims]'))
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'RoleNameIndex' AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
    CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_AspNetUserClaims_UserId' AND [object_id] = OBJECT_ID(N'[AspNetUserClaims]'))
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_AspNetUserLogins_UserId' AND [object_id] = OBJECT_ID(N'[AspNetUserLogins]'))
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_AspNetUserRoles_RoleId' AND [object_id] = OBJECT_ID(N'[AspNetUserRoles]'))
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'EmailIndex' AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UserNameIndex' AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
    CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

IF OBJECT_ID(N'[AdminUsers]', N'U') IS NOT NULL
    DROP TABLE [AdminUsers];
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Down is intentionally conventional. It should only be used when explicitly rolling
        // back this migration on a database owned by this application.
        migrationBuilder.CreateTable(
            name: "AdminUsers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AdminUsers", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AdminUsers_UserName",
            table: "AdminUsers",
            column: "UserName",
            unique: true);

        migrationBuilder.DropTable(name: "AspNetRoleClaims");
        migrationBuilder.DropTable(name: "AspNetUserClaims");
        migrationBuilder.DropTable(name: "AspNetUserLogins");
        migrationBuilder.DropTable(name: "AspNetUserRoles");
        migrationBuilder.DropTable(name: "AspNetUserTokens");
        migrationBuilder.DropTable(name: "AspNetRoles");
        migrationBuilder.DropTable(name: "AspNetUsers");
    }
}
