# Database Setup

1. Install SQL Server (or use LocalDB, which ships with Visual Studio).
2. Set your connection string in `appsettings.Development.json`.
3. Run:

```
dotnet ef database update
```

4. Run the app — in Development, `DbSeeder` populates demo data automatically.

No manual SQL steps are required. If `dotnet ef` isn't found, install it once
with `dotnet tool install --global dotnet-ef`.

