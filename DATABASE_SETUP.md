# Database Setup

The app uses SQL Server through Entity Framework Core. Pick one option.

## 1. Choose a database

| Option | Connection string |
| --- | --- |
| **LocalDB** (Windows, default) | Already in `appsettings.json` — nothing to do |
| **Docker SQL Server** (any OS) | `docker compose up -d sqlserver`, then set `Server=localhost,1433;Database=GiftOfTheGiversDb;User Id=sa;Password=<SA_PASSWORD>;Encrypt=False;TrustServerCertificate=True` |
| **Azure SQL** | Copy the ADO.NET string from the Azure portal (*SQL database → Connection strings*) |

Set anything other than LocalDB with user-secrets (never edit `appsettings.json` — the repo is public):

```bash
cd GiftOfTheGivers
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string>"
```

## 2. Create / update the schema

```bash
dotnet tool restore
dotnet ef database update --project GiftOfTheGivers/GiftOfTheGivers.csproj --startup-project GiftOfTheGivers/GiftOfTheGivers.csproj
```

`dotnet ef` builds the model from the app host, so it reads the same configuration (including user-secrets)
as the running app. The app also applies pending migrations on startup unless `Database:MigrateOnStartup=false`.

## 3. Run

```bash
dotnet run --project GiftOfTheGivers/GiftOfTheGivers.csproj
```

In Development, `DbSeeder` creates the Donor/Employee roles, two demo accounts and sample data.

## Production

1. Set `Database:MigrateOnStartup=false` in the App Service configuration.
2. Apply `migrate.sql` (the idempotent script published by CI / Azure Pipelines) before deploying the new build.
3. Enable Azure SQL point-in-time restore and confirm the retention period (see the CI/CD and release document).

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `The model ... has pending changes` at startup | Someone changed the model without adding a migration. Run `dotnet ef migrations add <Name>`; CI also catches this. |
| `Cannot open database` | Check the connection string / that SQL Server (or the Docker container) is running. |
| Login failed for `sa` (Docker) | `SA_PASSWORD` in `.env` must meet SQL Server complexity rules; recreate the volume after changing it. |
