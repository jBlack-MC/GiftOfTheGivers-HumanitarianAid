using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGivers.Models.ValidationAttributes;

/// <summary>Requires a boolean (e.g. a consent checkbox) to be ticked.</summary>
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public MustBeTrueAttribute()
        : base("You must accept the privacy notice to continue.")
    {
    }

    public override bool IsValid(object? value) => value is true;
}
