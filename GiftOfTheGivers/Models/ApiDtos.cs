namespace GiftOfTheGivers.Models;

public sealed class ReliefProjectApiDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal FundsRequired { get; set; }
    public decimal OfflineFundsZar { get; set; }
    public decimal FundsRaised { get; set; }
    public double ProgressPercentage { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public int UpdatesCount { get; set; }
}

public sealed class ProjectUpdateApiDto
{
    public int Id { get; set; }
    public int ReliefProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public string AuthorName { get; set; } = string.Empty;
}

public sealed class CreateDonationApiRequest
{
    public int? ReliefProjectId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string DonationType { get; set; } = "OneTime"; // "OneTime" or "Recurring"
    public string PaymentMethod { get; set; } = "Card"; // "Card" or "EFT"
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string? Notes { get; set; }
}

public sealed class DonationApiResponse
{
    public string TransactionReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public decimal AmountZar { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public int? ReliefProjectId { get; set; }
    public string? ReliefProjectTitle { get; set; }
    public string? PaymentCheckoutUrl { get; set; }
    public DateTime DonationDate { get; set; }
}

public sealed class VolunteerApplicationApiRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Skills { get; set; }
    public string? Availability { get; set; }
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public bool PrivacyConsent { get; set; }
}

public sealed class VolunteerStatusApiResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public int AssignedProjectsCount { get; set; }
}

public sealed class InquiryApiRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool PrivacyConsent { get; set; }
}

public sealed class ImpactStatsApiResponse
{
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int ApprovedVolunteers { get; set; }
    public int VerifiedDonations { get; set; }
    public decimal TotalRaisedZar { get; set; }
}

public sealed class LoginApiRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterApiRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class AuthApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public List<string> Roles { get; set; } = new();
}
