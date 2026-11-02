using System.ComponentModel.DataAnnotations;
using GiftOfTheGivers.Models.ValidationAttributes;

namespace GiftOfTheGivers.Models.ViewModels;

public class CreateReliefProjectInput
{
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Location { get; set; } = string.Empty;
    [Required] public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    [Required, Display(Name = "Start Date")] public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    [Display(Name = "End Date")] public DateTime? EndDate { get; set; }

    [Range(typeof(decimal), "0", "100000000000"), Display(Name = "Funding Goal (ZAR)")]
    public decimal FundsRequired { get; set; }

    [Range(typeof(decimal), "0", "100000000000"), Display(Name = "Funds Raised Offline (ZAR)")]
    public decimal OfflineFundsZar { get; set; }

    [AllowedImageUrl, Display(Name = "Image URL")] public string? ImageUrl { get; set; }
}

public class EditReliefProjectInput : CreateReliefProjectInput
{
    public int Id { get; set; }
}
