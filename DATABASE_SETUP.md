# Database Setup

## 1. Clone

```powershell
git clone <repository-url>
cd GiftOfTheGivers-HumanitarianAid
```

## 2. Set the connection string

From the `GiftOfTheGivers` project directory, store the SQL Server connection string locally with .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>"
```

Do not commit real connection strings or passwords.

## 3. Apply the EF Core migrations

From the `GiftOfTheGivers` project directory:

```powershell
dotnet ef database update
```

The application also applies pending migrations on startup. Demo accounts and
sample data are seeded only in the Development environment.

## 4. Run

```powershell
dotnet run
```

