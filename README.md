# Gift of the Givers - Humanitarian Aid Management System

A comprehensive web application for managing humanitarian aid operations, built with ASP.NET Core MVC (.NET 8).

🔗 **Live demo**: [giftgivers-app-bvefdybyc7baguaq.southafricanorth-01.azurewebsites.net](https://giftgivers-app-bvefdybyc7baguaq.southafricanorth-01.azurewebsites.net/)
> Hosted on Azure App Service's Free (F1) tier — no "Always On", so the first request after a period of inactivity may take 10-20s to cold-start.

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

- **Framework**: ASP.NET Core MVC (.NET 8)
- **Database**: Azure SQL Database via Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **UI**: Bootstrap 5 + Bootstrap Icons
- **Development Environment**: Visual Studio 2026

## 📦 Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2026 (or compatible IDE)
- Access to the team's Azure SQL Database (see [DATABASE_SETUP.md](DATABASE_SETUP.md))

### Installation

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd GiftOfTheGivers
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Set up the database connection**
   - This project uses Azure SQL. See [DATABASE_SETUP.md](DATABASE_SETUP.md) for how to get the connection string and store it with `dotnet user-secrets` (never edit `appsettings.json` directly — this repo is public).

4. **Run the application**
   ```bash
   dotnet run
   ```

   Pending migrations are applied automatically on startup — no separate migration step needed.

5. **Access the application**
   - Browse to the URL shown in the console (e.g. `http://localhost:5106`)

## 🗂️ Project Structure

```
GiftOfTheGivers/
├── Controllers/          # MVC Controllers (Home, Donor, Employee)
├── Models/              # Domain models and ViewModels
├── Views/               # Razor views
├── Data/                # DbContext and migrations
├── wwwroot/             # Static files (CSS, JS, images)
└── Areas/               # Identity area for authentication
```

## 📝 Key Models

- **Donation** - Donation records with currency, amount, and tax certificate tracking
- **Volunteer** - Volunteer applications with skills and availability
- **ReliefProject** - Humanitarian projects with location, goals, and progress
- **ProjectUpdate** - Status updates for active projects

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

## 📄 License

This is a student project created for educational purposes.

## 👥 Contributing

This is an academic project. For questions or suggestions, please contact the development team.

## 🙏 Acknowledgments

- Gift of the Givers Foundation for inspiration
- Bootstrap for UI components
- ASP.NET Core community for documentation and resources

---

**Note**: This is Part 1 of the project - a prototype implementation with dummy payment processing and placeholder features. Real payment gateway integration and production security features would be added in future phases.
