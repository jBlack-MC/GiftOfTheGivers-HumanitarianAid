# Gift of the Givers - Humanitarian Aid Management System

A web application for managing humanitarian aid operations — relief projects, online donations, volunteers and donor tax certificates — built with ASP.NET Core MVC (.NET 10).

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/Entity%20Framework%20Core-10.0-512BD4)
![Azure SQL](https://img.shields.io/badge/Database-Azure%20SQL-0078D4?logo=microsoftazure&logoColor=white)
![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5-7952B3?logo=bootstrap&logoColor=white)
![License](https://img.shields.io/badge/license-Educational-lightgrey)

> **Status: demonstration system (beta-quality code, not yet production).** It is an academic project that
> uses the name of a real NGO for illustration. Payments run against the PayFast **sandbox** by default and
> tax certificates are marked *demonstration* until real organisation details are configured. See
> [docs/deliverables](docs/deliverables/) for the full product, technical and audit documentation.

🔗 **Live demo**: [giftgivers-app-bvefdybyc7baguaq.southafricanorth-01.azurewebsites.net](https://giftgivers-app-bvefdybyc7baguaq.southafricanorth-01.azurewebsites.net/)
(Azure App Service Free tier — the first request after idle can take 10–20 s.)

## 📑 Contents

- [Features](#-features)
- [Technology Stack](#️-technology-stack)
- [Getting Started](#-getting-started)
- [Configuration](#️-configuration)
- [Accounts, Roles and Staff Access](#-accounts-roles-and-staff-access)
- [Payments and Donations](#-payments-and-donations)
- [Testing](#-testing)
- [Database and Migrations](#️-database-and-migrations)
- [CI/CD and Deployment](#-cicd-and-deployment)
- [Project Structure](#️-project-structure)
- [Documentation and Audits](#-documentation-and-audits)
- [Known Limitations](#️-known-limitations)
- [Security](#-security)

## 🚀 Features

### Public site

- Home, About (live impact figures from the database), Relief Projects with funding progress
- **Donate** in ZAR, USD or EUR → redirected to **PayFast** to pay; foreign currency is converted to rand at a configured rate
- Donation receipt by unguessable reference, showing live payment status
- Volunteer application and Contact form, both with privacy consent
- Privacy notice (POPIA) — draft pending legal review

### Donor portal (Donor role)

- Dashboard of confirmed donations and totals (in rand), full donation history with payment status
- Section 18A-style donation certificate (HTML + PDF) — **only for donations confirmed by PayFast**

### Employee portal (Employee role)

- Relief projects: create/edit, offline funds, project updates
- Volunteers: filter by status, approve/reject, assign approved volunteers to projects
- Donations: filter by status, CSV export (formula-injection safe)
- **Funding reconciliation**: compares each project's total with offline funds + paid donations
- Inquiries, **Staff Access** (grant/revoke the Employee role), **Audit Log** of every staff action

## 🛠️ Technology Stack

| Area | Technology |
| --- | --- |
| Web | ASP.NET Core MVC + Razor Pages (Identity UI), .NET 10 |
| Data | Entity Framework Core 10 on SQL Server / Azure SQL (LocalDB for development) |
| Identity | ASP.NET Core Identity (cookies, roles: Donor, Employee) |
| Payments | PayFast (redirect checkout + ITN webhook) |
| Email | SendGrid (optional; no-op when unconfigured) |
| PDF | QuestPDF (Community licence) |
| Logging | Serilog (console, request logging) |
| UI | Bootstrap 5, Bootstrap Icons |
| Serverless | Azure Functions (isolated worker) — key-protected certificate endpoint |
| Tests | xUnit, `WebApplicationFactory` integration tests, EF InMemory |
| CI/CD | GitHub Actions (CI, CD to GHCR, Azure DevOps mirror, NuGet publish), Azure Pipelines |

## 📦 Getting Started

### Prerequisites

- **.NET 10 SDK** — pinned in [global.json](global.json) (`10.0.401`, newer feature bands allowed).
- **SQL Server** — LocalDB (Windows, ships with Visual Studio) or Docker (any OS, see below).
- `dotnet-ef` is a **local tool** ([.config/dotnet-tools.json](.config/dotnet-tools.json)); `dotnet tool restore` installs it.

### Option A — setup script

```powershell
.\setup.ps1          # Windows (LocalDB)
```

```bash
chmod +x setup.sh && ./setup.sh   # macOS/Linux — start Docker SQL first (see Option C)
```

The script restores packages (lock files), restores `dotnet-ef` and applies migrations.

### Option B — manual

```bash
dotnet restore GiftOfTheGivers.slnx
dotnet tool restore
dotnet ef database update --project GiftOfTheGivers/GiftOfTheGivers.csproj --startup-project GiftOfTheGivers/GiftOfTheGivers.csproj
dotnet run --project GiftOfTheGivers/GiftOfTheGivers.csproj
```

Browse to the URL printed in the console (e.g. `http://localhost:5106`). In **Development**, demo data and two
demo accounts are seeded automatically:

| Role | Email | Password |
| --- | --- | --- |
| Employee | `employee@giftofthegivers.org` | `Employee#123` |
| Donor | `donor@example.com` | `Donor#123` |

> Demo accounts are created **only** in Development (or when `Seed:DemoData=true`). Never enable demo data on a
> public site.

### Option C — Docker (app + SQL Server)

```powershell
Copy-Item .env.example .env      # then set a strong SA_PASSWORD
docker compose up --build -d     # app on http://localhost:8080, SQL on 127.0.0.1:1433
docker compose down
```

The compose stack runs in Development (demo data, detailed errors) and is for local use only.

## ⚙️ Configuration

Never commit secrets — `appsettings.json` is public. Use user-secrets locally and App Service settings in Azure.

```bash
cd GiftOfTheGivers
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string>"
dotnet user-secrets set "PayFast:Passphrase" "<your sandbox passphrase>"
dotnet user-secrets set "SendGrid:ApiKey" "<key>"
```

| Key | Default | Purpose |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | LocalDB | Database |
| `Database:MigrateOnStartup` | `true` | Apply migrations at startup. Set `false` in production and run the pipeline's idempotent script instead |
| `Seed:DemoData` | `false` | Seed demo accounts/data outside Development |
| `StaffAccess:BootstrapEmployeeEmails` | `[]` | Existing accounts granted Employee at startup (how the first staff member is created) |
| `PayFast:Enabled` / `Sandbox` | `true` / `true` | Turn checkout on/off; sandbox vs live |
| `PayFast:MerchantId` / `MerchantKey` / `Passphrase` | sandbox values / empty | Merchant credentials. **Live mode refuses to start without a passphrase** |
| `PayFast:ReturnUrl` / `CancelUrl` / `NotifyUrl` | empty | Leave empty to derive from the request host |
| `Currency:RatesToZar:USD` / `EUR` | `18.50` / `20.00` | **Indicative** conversion rates — maintain before accepting real foreign donations |
| `Organisation:*` | demo values | Name, contact details, `PboReference`, `InformationOfficerEmail`, `IsDemo` |
| `Identity:RequireConfirmedAccount` | `false` | Require email confirmation (needs SendGrid) |
| `RateLimiting:PostPermitLimit` / `PostWindowSeconds` | `20` / `60` | Per-IP limit for POST requests |
| `ForwardedHeaders:Enabled` | `false` | Enable behind a reverse proxy (Azure App Service on Linux, containers) |

## 🔐 Accounts, Roles and Staff Access

- **Public registration always creates a Donor account.** There is no way to self-register as staff.
- **Employee (staff) access** is granted by an existing employee at *Employee → Staff Access*, or at startup
  for emails listed in `StaffAccess:BootstrapEmployeeEmails` (use this once to create the first employee).
- Role changes, project edits, volunteer decisions, reconciliations, exports and certificate access are written
  to the **Audit Log**.
- Passwords: 8+ characters with upper, lower, digit and symbol. Lockout settings are configured; note the default
  Identity login page does not enable lockout-on-failure (see Known Limitations).

## 💳 Payments and Donations

1. The donor submits the form → a **Pending** donation is stored (amount, currency, rand equivalent, consent time).
2. The donor is redirected to PayFast (sandbox by default) to pay the **rand** amount.
3. PayFast calls `POST /Payments/Notify` (ITN). The app verifies the signature with PayFast, checks the
   **merchant id and amount**, then marks the donation **Verified** and adds it to the project total — exactly once.
4. Only verified donations count toward totals, dashboards and certificates. Failed/cancelled payments never do.

To test end to end you need **your own PayFast sandbox account** (free) and its passphrase, and a public
`NotifyUrl` (e.g. a tunnel) so PayFast can reach your machine. The shared public sandbox merchant `10000100`
currently rejects signed requests (see the audit addendum).

Projects also have **offline funds** (cash, grants) maintained by staff; the public "raised" figure is
offline funds + verified online donations. *Employee → Funding Reconciliation* finds and fixes differences.

## 🧪 Testing

```bash
dotnet test GiftOfTheGivers.slnx
```

98 tests: unit tests plus `WebApplicationFactory` integration tests that exercise the real pipeline (routing,
anti-forgery, authorization, views) against an in-memory database — role matrix, registration, donation capture,
payment notifications, certificates, reconciliation, consent, security headers and rate limiting.
CI additionally applies every migration to a real SQL Server container and fails on model drift.

## 🗄️ Database and Migrations

- Migrations live in `GiftOfTheGivers/Data/Migrations`. `dotnet ef` builds the model from the app host, so it uses
  the same configuration (appsettings, user-secrets, environment variables) as the app.
- Add a migration: `dotnet ef migrations add <Name> --project GiftOfTheGivers/GiftOfTheGivers.csproj --startup-project GiftOfTheGivers/GiftOfTheGivers.csproj`
- Check for drift: `dotnet ef migrations has-pending-model-changes ...` (CI runs this).
- Production: generate `dotnet ef migrations script --idempotent` (published by CI/Azure Pipelines) and apply it as
  a deployment step with `Database:MigrateOnStartup=false`.

See [DATABASE_SETUP.md](DATABASE_SETUP.md) and the [backend schema document](docs/deliverables/05-backend-schema.html).

## 🔁 CI/CD and Deployment

| Pipeline | Trigger | What it does |
| --- | --- | --- |
| `.github/workflows/ci.yml` | push/PR | Locked restore, build, tests + coverage, **blocking** vulnerability gate, EF drift check, migrations applied to SQL Server 2022 (+ rollback/re-apply), idempotent script artifact |
| `.github/workflows/cd.yml` | CI succeeded on `main` | Build and push container image to GHCR (`:sha`, `:main`) |
| `.github/workflows/mirror-to-azure-devops.yml` | any push | Mirror to Azure DevOps (needs `AZURE_DEVOPS_REPO_URL`, `AZURE_DEVOPS_PAT`) |
| `.github/workflows/publish-helpers-package.yml` | `main`, `helpers-v*` tags | Test, pack and push `GiftOfTheGivers.Helpers` to Azure Artifacts (needs `AZURE_ARTIFACTS_PAT`) |
| `azure-pipelines.yml` | mirrored commits | Build/test/vulnerability gate/drift check, publish web + migration script, deploy `main` to App Service (needs `azureServiceConnection`, `azureWebAppName`) |

Full details: [CI/CD and release document](docs/deliverables/07-ci-cd-and-release.html).

## 🗂️ Project Structure

```text
GiftOfTheGivers/                 # Web app
├── Controllers/                 # Home, Donor, Employee, Payments (PayFast ITN)
├── Services/                    # DonationService, PayFast, currency, email, audit, PDF certificates
├── Infrastructure/              # Money formatting, security headers, CSV, claims helpers
├── ViewComponents/              # Live impact statistics
├── Data/                        # DbContext, DbSeeder, Migrations
├── Models/ (+ ViewModels/)      # Entities and input/view models
├── Views/, Areas/Identity/      # Razor views, customised Register page
└── wwwroot/                     # CSS, JS, images, intro animation
GiftOfTheGivers.Functions/       # Azure Function: key-protected certificate generation
GiftOfTheGivers.Helpers/         # Calculation helpers (NuGet package)
GiftOfTheGivers.Tests/           # Unit + integration tests
docs/deliverables/               # Product, technical, flow, design, schema, plan, CI/CD, audit (HTML + PDF)
docs/audits/                     # Audit programme, findings register (machine readable), audit history
```

## 📚 Documentation and Audits

| # | Document |
| --- | --- |
| 01 | [Product requirements](docs/deliverables/01-product-requirements.html) |
| 02 | [Technical requirements](docs/deliverables/02-technical-requirements.html) |
| 03 | [Application flow](docs/deliverables/03-app-flow.html) |
| 04 | [Design brief](docs/deliverables/04-design-brief.html) |
| 05 | [Backend schema](docs/deliverables/05-backend-schema.html) |
| 06 | [Implementation plan](docs/deliverables/06-implementation-plan.html) |
| 07 | [CI/CD and release](docs/deliverables/07-ci-cd-and-release.html) |
| 08 | [Professional engineering audit](docs/deliverables/08-professional-engineering-audit.html) and [remediation addendum](docs/deliverables/08-professional-engineering-audit-addendum.html) |

Each has a PDF beside it. Audits are versioned in [docs/audits](docs/audits/README.md); the next audit starts
from the findings register there.

## ⚠️ Known Limitations

- Recurring donations are recorded as monthly **pledges**; automatic recurring billing is not implemented.
- Exchange rates are configured manually (indicative); there is no live rates provider.
- Certificates are demonstration documents until `Organisation:PboReference` is set and `IsDemo=false`.
- The privacy notice is a draft that requires legal review; retention periods are not yet enforced automatically.
- The default Identity login page does not apply lockout-on-failure; rate limiting is the current brute-force control.
- No MFA for staff, no backups/DR runbook, no infrastructure-as-code yet (see the implementation plan).

## 🛡️ Security

Please report vulnerabilities privately — see [SECURITY.md](SECURITY.md).

## 📄 License

This is a student project created for educational purposes. Gift of the Givers Foundation is referenced for
inspiration only; this application is not affiliated with or endorsed by the organisation.
