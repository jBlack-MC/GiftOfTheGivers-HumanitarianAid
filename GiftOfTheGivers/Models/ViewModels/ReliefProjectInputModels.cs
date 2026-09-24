using System.ComponentModel.DataAnnotations;
using GiftOfTheGivers.Models.ValidationAttributes;

namespace GiftOfTheGivers.Models.ViewModels;

public class CreateReliefProjectInput
{
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [Required] public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    [Required] public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Range(0, double.MaxValue)] public decimal FundsRequired { get; set; }
    [AllowedImageUrl] public string? ImageUrl { get; set; }
}

public class EditReliefProjectInput : CreateReliefProjectInput
{
    public int Id { get; set; }
}