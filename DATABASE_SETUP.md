# Database Setup

This project uses **Azure SQL Database** as its database, accessed through Entity Framework Core. This doc covers everything a team member needs to connect, run migrations, and troubleshoot.

> **This repository is public.** Never put the real server name, database name, username, or password into any file that gets committed (`appsettings.json` included). Connection details are shared privately (Slack/WhatsApp/whatever the team uses) and stored locally with .NET User Secrets, which lives outside the project folder and is never committed.

## 1. Get the connection string

Ask a teammate for the real Azure SQL connection string over a private channel — not a commit, not an issue, not a public PR comment. It looks like:

```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## 2. Store it locally with User Secrets

From the `GiftOfTheGivers/` folder (the one with `GiftOfTheGivers.csproj`):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<paste the full connection string here>"
```

You should see `Successfully saved ConnectionStrings:DefaultConnection to the secret store.` This writes to a file in your Windows user profile (`%APPDATA%\Microsoft\UserSecrets\...`), completely outside the repo — `git status` won't show anything change.

Verify what's stored at any time with:

```powershell
dotnet user-secrets list
```

In `Development` environment (the default when you `dotnet run` locally), user secrets automatically override whatever is in `appsettings.json`, so you never need to touch that file.

## 3. Get your IP allow-listed

Azure SQL rejects connections from any IP that isn't explicitly allowed. If you see an error like:

```
Cannot open server '<server>' requested by the login. Client with IP address 'X.X.X.X' is not allowed to access the server.
```

that tells you your IP. Ask whoever administers the Azure SQL server to add it under:
**Azure Portal → your SQL server (not the database) → Security → Networking → Firewall rules**

It takes a minute or two to take effect after saving. Your IP can change (new network, VPN, ISP renewal), so you may need to repeat this occasionally.

## 4. Run the app

```powershell
cd GiftOfTheGivers
dotnet run
```

The app applies any pending EF Core migrations automatically on startup and seeds the `Donor` and `Employee` roles, so there's no separate "create the database" step once your connection string and firewall access are set.

## Schema overview

| Table | Purpose |
|---|---|
| `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims` | ASP.NET Core Identity — login, roles (`Donor`, `Employee`) |
| `ReliefProjects` | Relief project listings shown across the site |
| `Volunteers` | Volunteer applications |
| `Donations` | Donation records, linked to a donor (`AspNetUsers`) and optionally a `ReliefProject` |
| `ProjectUpdates` | Status updates posted against a `ReliefProject` |

The EF Core model lives in `GiftOfTheGivers/Data/ApplicationDbContext.cs`; migrations live in `GiftOfTheGivers/Data/Migrations/`.

## Making a schema change

1. Edit the relevant model in `Models/` or the `DbSet`/config in `Data/ApplicationDbContext.cs`.
2. Generate a migration:
   ```powershell
   cd GiftOfTheGivers
   dotnet ef migrations add <DescriptiveName>
   ```
3. Apply it to the shared Azure database yourself:
   ```powershell
   dotnet ef database update
   ```
4. Commit the new files under `Data/Migrations/` (the `.cs`, `.Designer.cs`, and the updated `ApplicationDbContextModelSnapshot.cs`).

Whoever writes a migration should also be the one to apply it once with `dotnet ef database update`, since everyone shares the same Azure database. Once it's applied and pushed, teammates just pull and `dotnet run` — the app's startup `Database.Migrate()` call brings their code in sync automatically; they don't need to run `dotnet ef` themselves for changes they didn't author.

## Local SQLite file (`GiftOfTheGivers.db`)

Earlier in development this project used a local SQLite database. It's gitignored, no longer wired up (`Program.cs` uses `UseSqlServer` now), and is just kept on disk as a historical backup. You can ignore or delete it — nothing depends on it.

## Troubleshooting

| Error | Fix |
|---|---|
| `Connection string 'DefaultConnection' not found` | You haven't run the `dotnet user-secrets set` command yet (step 2). |
| `Client with IP address 'X.X.X.X' is not allowed to access the server` | Your IP isn't firewall-allowed yet (step 3). |
| `Login failed for user '<user>'` | Wrong or stale password in your local secret — re-run `dotnet user-secrets set` with the correct connection string. |
| `Could not find a MSBuild project file` when running `dotnet user-secrets` or `dotnet ef` | You're not in the `GiftOfTheGivers/` folder — `cd` into it first. |
