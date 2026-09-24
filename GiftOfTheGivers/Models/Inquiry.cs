using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGivers.Models;

public class Inquiry
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

    public bool Handled { get; set; }
}