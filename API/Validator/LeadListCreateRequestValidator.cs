using API.DTOs;
using FluentValidation;

namespace API.Validator;

public class LeadListCreateRequestValidator : AbstractValidator<LeadListCreateRequest>
{
    public LeadListCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The 'name' field is required.")
            .MaximumLength(100).WithMessage("The 'name' field cannot exceed 100 characters.")
            .Matches(@"^[\p{L}\p{N}\s\-_]+$").WithMessage("The 'name' field contains invalid characters."); 

        RuleFor(x => x.SourceUrl)
            .Cascade(CascadeMode.Stop)
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("If provided, 'sourceUrl' must be a valid and absolute URL.")
            .Must(uri => string.IsNullOrEmpty(uri) || new Uri(uri).Scheme is "http" or "https")
            .WithMessage("The 'sourceUrl' must use HTTP or HTTPS.");
    }
}