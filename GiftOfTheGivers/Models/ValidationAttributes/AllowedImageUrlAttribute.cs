using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGivers.Models.ValidationAttributes;

public sealed class AllowedImageUrlAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
        {
            return ValidationResult.Success;
        }

        if (url.Length > 500 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new ValidationResult("Image URL must be a valid http or https link no longer than 500 characters.");
        }

        return ValidationResult.Success;
    }
}