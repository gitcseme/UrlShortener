using FluentValidation;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Validators;

public class CreateUrlRequestValidator : AbstractValidator<CreateUrlRequest>
{
    public CreateUrlRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required.")
            .Must(BeValidHttpOrHttpsUrl).WithMessage("A valid http or https URL is required.");

        When(x => !string.IsNullOrWhiteSpace(x.Alias), () =>
        {
            RuleFor(x => x.Alias!)
                .Length(3, 20).WithMessage("Alias must be between 3 and 20 characters.")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Alias may only contain letters, digits, hyphens, and underscores.");
        });
    }

    private static bool BeValidHttpOrHttpsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https");
    }
}
