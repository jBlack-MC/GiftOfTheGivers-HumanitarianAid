# Gift of the Givers - Humanitarian Aid Management System

A comprehensive web application for managing humanitarian aid operations, built with ASP.NET Core MVC (.NET 10).

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/Entity%20Framework%20Core-10.0-512BD4)
![Azure SQL](https://img.shields.io/badge/Database-Azure%20SQL-0078D4?logo=microsoftazure&logoColor=white)
![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5-7952B3?logo=bootstrap&logoColor=white)
![License](https://img.shields.io/badge/license-Educational-lightgrey)

🔗 **Live demo**: [giftgivers-app-bvefdybyc7baguaq.southafricanorth-01.azurewebsites.net](https://giftgivers-app-bvefdybyc7baguaq.southafricanorth-01.azurewebsites.net/)
> Hosted on Azure App Service's Free (F1) tier — no "Always On", so the first request after a period of inactivity may take 10-20s to cold-start.
>
> **🔑 Demo login** (works on the live site and locally)
>
> | Role | Email | Password |
> | --- | --- | --- |
> | **Employee** | `employee@giftofthegivers.org` | `Employee#123` |
> | **Donor** | `donor@example.com` | `Donor#123` |
>
> Seeded automatically on first run by `Data/DbSeeder.cs` — see [Demo / seeded accounts](#demo--seeded-accounts) below for details.

## 📑 Contents

- [Project Overview](#-project-overview)
- [Features](#-features)
- [Technology Stack](#️-technology-stack)
- [Getting Started](#-getting-started)
- [Project Structure](#️-project-structure)
- [Key Models](#-key-models)
- [Authentication & Authorization](#-authentication--authorization)
- [UI Features](#-ui-features)
- [License](#-license)
- [Contributing](#-contributing)
- [Acknowledgments](#-acknowledgments)

## 📋 Project Overview

This application supports the Gift of the Givers Foundation's mission to provide humanitarian aid by offering a complete platform for:

- **Public Portal**: Information about relief projects, donation processing, and volunteer registration
- **Donor Area**: Donation tracking, tax certificate management, and donor dashboard
- **Employee Area**: Relief project management, volunteer coordination, and donation oversight

## 🚀 Features

### Public Pages

- **Home** - Landing page with mission statement and project highlights
- **About** - Organization information and impact statistics
- **Relief Projects** - Browse active humanitarian projects
- **Donate** - Multi-currency donation system (ZAR, USD, EUR) with one-time and recurring options
- **Volunteer** - Simplified volunteer registration form
- **Contact** - Contact form for inquiries

### Donor Portal (Authenticated)

- Donor dashboard with donation history
- Tax-deductible certificate generation
- Donation details and tracking
- Recurring donation management

### Employee Portal (Authenticated)

- Relief project creation and management
- Project update system
- Volunteer application review and approval
- Donation oversight and reporting

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core MVC (.NET 10)
- **Database**: Azure SQL Database via Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **UI**: Bootstrap 5 + Bootstrap Icons
- **PDF generation**: QuestPDF (donor tax certificates)
- **Development Environment**: Visual Studio 2026

## 📦 Getting Started

### Mirror GitHub pushes to Azure DevOps

The repository includes a GitHub Actions workflow that mirrors every branch
and tag push to Azure DevOps.

1. Create the destination Git repository in Azure DevOps.
2. Create an Azure DevOps PAT with **Code: Read & write** permission.
3. Add these GitHub repository secrets under **Settings → Secrets and variables → Actions**:
   - `AZURE_DEVOPS_REPO_URL`: for example, `https://dev.azure.com/ORG/PROJECT/_git/REPO`
   - `AZURE_DEVOPS_PAT`: the Azure DevOps PAT
4. Push to GitHub. The `Mirror to Azure DevOps` workflow will synchronize all branches and tags.

The workflow uses `git push --mirror`, so deleted GitHub branches and tags are
also deleted from the Azure DevOps mirror. Do not make independent changes in
the Azure DevOps repository.

### Publish NuGet package to Azure Artifacts

The repository includes a GitHub Actions workflow (`Publish Helpers Package to Azure Artifacts`) that packs and publishes `GiftOfTheGivers.Helpers` to this feed:

- `https://pkgs.dev.azure.com/ST10438928/663d5a52-1332-4463-92cb-b12591f7fde0/_packaging/giftgivers-helpers/nuget/v3/index.json`

1. Create an Azure DevOps PAT with **Packaging: Read & write** permission.
2. Add this GitHub repository secret under **Settings → Secrets and variables → Actions**:
   - `AZURE_ARTIFACTS_PAT`: the Azure DevOps PAT
3. Push `main` to publish an automatic CI package (`1.0.0-ci.<runNumber>`), or push a version tag to publish a fixed version:

   ```bash
   git tag helpers-v1.0.1
   git push origin helpers-v1.0.1
   ```

If you do not see packages in Azure DevOps Artifacts, open GitHub **Actions** and check the latest `Publish Helpers Package to Azure Artifacts` run first.

### Azure Pipelines + Azure Environment deployment

The repository contains `azure-pipelines.yml` so the mirrored Azure DevOps repo can build/test automatically, publish an artifact, and deploy from `main` to an Azure DevOps environment.

1. In Azure DevOps, open the mirrored repo and create a pipeline from existing YAML: `azure-pipelines.yml`.
2. Create or confirm an Azure Resource Manager service connection that can deploy to your App Service.
3. In the pipeline variables, set:
   - `azureServiceConnection` = your service connection name
   - `azureWebAppName` = your Azure App Service name
4. In Azure DevOps, create an environment named `giftgivers-dev` (Pipelines → Environments).
5. Push to GitHub `main`.

Flow after setup:
- GitHub push triggers `Mirror to Azure DevOps`
- Azure DevOps receives the mirrored commit
- `azure-pipelines.yml` runs CI
- If branch is `main` and deployment variables are set, it deploys to `giftgivers-dev`

### Prerequisites

- **.NET 10 SDK** — version is pinned in [global.json](global.json) (currently `10.0.401`); `dotnet build` will fail fast with a clear error if it's missing instead of silently using a different SDK.
- **SQL Server** — either **LocalDB** (ships with Visual Studio, Windows-only) or access to the team's **Azure SQL Database** (see [DATABASE_SETUP.md](DATABASE_SETUP.md)). Mac/Linux contributors should use the [Docker Compose](#docker-app--sql-server) setup instead of LocalDB.
- **EF Core tools** — `dotnet-ef` is pinned as a local tool in [.config/dotnet-tools.json](.config/dotnet-tools.json); `dotnet tool restore` installs the exact version the team uses (no need to `dotnet tool install --global`).
- Visual Studio 2026 (or compatible IDE) — optional, any editor works.

### Installation

**Option A — one-time setup script (recommended)**

```powershell
# Windows
.\setup.ps1
```

```bash
# Mac/Linux
chmod +x setup.sh
./setup.sh
```

This restores NuGet packages (using the committed `packages.lock.json` files, so everyone gets identical package versions), restores the `dotnet-ef` local tool, and applies EF Core migrations to LocalDB. It prints the run command and demo login at the end.

**Option B — manual steps**

1. **Clone the repository**

   ```bash
   git clone <your-repo-url>
   cd GiftOfTheGivers-HumanitarianAid
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Restore local dotnet tools**

   ```bash
   dotnet tool restore
   ```

4. **Set up the database connection**
   - The default connection string in `appsettings.json` points at LocalDB, which works out of the box on Windows.
   - To use Azure SQL instead, see [DATABASE_SETUP.md](DATABASE_SETUP.md) for how to get the connection string (never edit `appsettings.json` directly — this repo is public; use `dotnet user-secrets` instead, see below).

5. **Apply EF Core migrations**

   ```bash
   dotnet ef database update --project GiftOfTheGivers/GiftOfTheGivers.csproj --startup-project GiftOfTheGivers/GiftOfTheGivers.csproj
   ```

   (Pending migrations are also applied automatically on startup, so this step is optional if you're fine letting `dotnet run` do it.)

6. **Run the application**

   ```bash
   dotnet run --project GiftOfTheGivers/GiftOfTheGivers.csproj
   ```

7. **Access the application**
   - Browse to the URL shown in the console (e.g. `http://localhost:5106`)
   - Sign in with one of the [demo accounts](#demo--seeded-accounts) above, or [register](#how-registration-works) your own

### Managing local secrets

Never commit real connection strings or API keys — `appsettings.json` is public. Use [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) to store your own PayFast/SendGrid keys (or an Azure SQL connection string) locally; they're stored outside the repo and never get pushed.

```bash
cd GiftOfTheGivers
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-azure-sql-connection-string>"
dotnet user-secrets set "PayFast:MerchantId" "<your-merchant-id>"
dotnet user-secrets set "SendGrid:ApiKey" "<your-sendgrid-key>"
```

See [DATABASE_SETUP.md](DATABASE_SETUP.md) for the exact keys the app reads.

### Docker (app + SQL Server)

Use Docker Compose to run the web app and SQL Server together.

Run these commands from the **repository root** (`GiftOfTheGivers-HumanitarianAid`), not the `GiftOfTheGivers` subfolder.

1. Create an env file from the example:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Start both containers:

   ```powershell
   docker compose up --build -d
   ```

3. Open `http://localhost:8080`

4. Stop containers when done:

   ```powershell
   docker compose down
   ```

If you see `SA_PASSWORD variable is not set`, create `.env` first and set a strong password.

> `appsettings.json` uses LocalDB for local Visual Studio runs. LocalDB is Windows-only and does not work inside Linux containers, so the compose file overrides `ConnectionStrings__DefaultConnection` for container runtime.

## 🗂️ Project Structure

```text
GiftOfTheGivers/
├── Controllers/          # MVC Controllers (Home, Donor, Employee)
├── Models/                # Domain models and ViewModels
├── Views/                 # Razor views
├── Data/                  # DbContext, migrations, and DbSeeder
├── Services/               # PDF generation and other app services
├── wwwroot/                # Static files (CSS, JS, images, intro animation)
└── Areas/                  # Identity area for authentication
```

## 📝 Key Models

- **Donation** - Donation records with currency, amount, and tax certificate tracking
- **Volunteer** - Volunteer applications with skills and availability
- **VolunteerAssignment** - Links approved volunteers to the relief projects they're assigned to
- **ReliefProject** - Humanitarian projects with location, goals, and progress
- **ProjectUpdate** - Status updates for active projects
- **AppUser** - Application user (extends ASP.NET Identity) with `FullName` and `DateRegistered`

## 🔐 Authentication & Authorization

The application uses **ASP.NET Core Identity** (scaffolded UI, under the `Identity` area) with role-based authorization:

- **Public** - Access to home, about, projects, donate, volunteer, contact (no account needed)
- **Donor** - `[Authorize(Roles = "Donor")]` on `DonorController` - donation history and tax certificates
- **Employee** - `[Authorize(Roles = "Employee")]` on `EmployeeController` - full relief-project/volunteer/donation management

### Login / Register pages

| Action | URL |
| --- | --- |
| Register | `/Identity/Account/Register` |
| Login | `/Identity/Account/Login` |
| Logout | `/Identity/Account/Logout` (POST, via the navbar) |
| Manage account | `/Identity/Account/Manage` |

Both links live in the navbar (top right) when signed out; a "Hello, `<name>`" menu + Logout replace them once signed in.

### How registration works

Anyone can self-register at `/Identity/Account/Register` and **picks their own account type** ("Donor" or "Employee") from a dropdown on the form. This is a Part-1 prototype simplification — a real deployment would not let the public grant themselves the Employee (staff) role; that would move behind an admin-invite or approval step in a later phase.

- Email confirmation is **switched off** (`RequireConfirmedAccount = false` in `Program.cs`) and no real email sender is wired up, so new accounts are usable immediately after registering — no inbox check required.
- Password rules are ASP.NET Identity's defaults: **at least 6 characters**, with at least one uppercase letter, one lowercase letter, one digit, and one non-alphanumeric character (e.g. `Donor#123`).
- Roles (`Donor`, `Employee`) are created automatically the first time they're needed — no manual setup required.

### Demo / seeded accounts

On first run, `Data/DbSeeder.cs` seeds two ready-to-use accounts (Part 1 prototype only — **do not reuse these passwords for anything real**):

| Role | Email | Password | Sees |
| --- | --- | --- | --- |
| Employee | `employee@giftofthegivers.org` | `Employee#123` | Employee dashboard - manage relief projects, review volunteers, oversee donations |
| Donor | `donor@example.com` | `Donor#123` | Donor dashboard - donation history, tax certificates |

These are safe to commit because they're seed-only, non-production credentials for a public student prototype — not real Azure/database secrets (see [DATABASE_SETUP.md](DATABASE_SETUP.md) for those).

## 🎨 UI Features

- Responsive design with Bootstrap 5
- Clean, accessible forms with validation
- Success/confirmation pages for all submissions
- Print-friendly tax certificates
- Mobile-optimized layouts
- Cinematic intro animation on first visit each browser session

## 📄 License

This is a student project created for educational purposes.

## 👥 Contributing

This is an academic project. For questions or suggestions, please contact the development team. See [CONTRIBUTING.md](CONTRIBUTING.md) for branch naming and workflow conventions.

## 🙏 Acknowledgments

- Gift of the Givers Foundation for inspiration
- Bootstrap for UI components
- ASP.NET Core community for documentation and resources

---

**Note**: This is an educational project. Payment processing, email delivery,
and production hardening depend on environment-specific configuration and
must be verified before production use.
