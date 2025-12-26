using Diax.Application.Customers.Dtos;
using FluentValidation;

namespace Diax.Application.Customers.Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(255).WithMessage("E-mail deve ter no máximo 255 caracteres.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Nome da empresa deve ter no máximo 200 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.CompanyName));

        RuleFor(x => x.Document)
            .MaximumLength(14).WithMessage("Documento deve ter no máximo 14 caracteres.")
            .Matches(@"^\d+$").WithMessage("Documento deve conter apenas números.")
            .When(x => !string.IsNullOrEmpty(x.Document));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.WhatsApp)
            .MaximumLength(20).WithMessage("WhatsApp deve ter no máximo 20 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.WhatsApp));

        RuleFor(x => x.SecondaryEmail)
            .EmailAddress().WithMessage("E-mail secundário inválido.")
            .MaximumLength(255).WithMessage("E-mail secundário deve ter no máximo 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.SecondaryEmail));

        RuleFor(x => x.Website)
            .MaximumLength(500).WithMessage("Website deve ter no máximo 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.SourceDetails)
            .MaximumLength(500).WithMessage("Detalhes da origem deve ter no máximo 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.SourceDetails));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Observações deve ter no máximo 4000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags deve ter no máximo 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Tags));
    }
}
