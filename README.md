# Gift of the Givers - Humanitarian Aid Management System

A comprehensive web application for managing humanitarian aid operations, built with ASP.NET Core MVC (.NET 10).

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
- **Database**: Entity Framework Core with SQL Server/SQLite
- **Authentication**: ASP.NET Core Identity
- **UI**: Bootstrap 5 + Bootstrap Icons
- **Development Environment**: Visual Studio 2026

## 📦 Getting Started

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 (or compatible IDE)
- SQL Server or SQLite

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

3. **Update database connection**
   - Edit `appsettings.json` to configure your database connection string

4. **Run migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - Browse to `https://localhost:5001` or the displayed URL

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

The application uses ASP.NET Core Identity with role-based authorization:

- **Public** - Access to home, about, projects, donate, volunteer, contact
- **Donor** - Access to donation history and tax certificates
- **Employee** - Full management capabilities

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
