using System.ComponentModel.DataAnnotations;
using GiftOfTheGivers.Models.ValidationAttributes;

namespace GiftOfTheGivers.Models.ViewModels;

public class CreateProjectUpdateInput
{
    public int ReliefProjectId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Content { get; set; } = string.Empty;
    [AllowedImageUrl] public string? ImageUrl { get; set; }
}

public class EditProjectUpdateInput : CreateProjectUpdateInput
{
    public int Id { get; set; }
}