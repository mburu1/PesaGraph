using FluentValidation;
using PesaGraph.Tenancy.DTOs;

namespace PesaGraph.Tenancy.Validators;

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MaximumLength(150).WithMessage("Tenant name must not exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Tenant code is required.")
            .MinimumLength(3).WithMessage("Tenant code must be at least 3 characters.")
            .MaximumLength(20).WithMessage("Tenant code must not exceed 20 characters.")
            .Matches("^[A-Z0-9_]+$").WithMessage("Tenant code must contain only uppercase alphanumeric characters and underscores.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("A valid contact email is required.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Contact phone is required.");
    }
}
