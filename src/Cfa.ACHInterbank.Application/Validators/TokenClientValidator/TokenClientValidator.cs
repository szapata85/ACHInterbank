using Cfa.ACHInterbank.Application.Services.TokenClient.Model;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.TokenClientValidator;

public class TokenClientValidator : AbstractValidator<TokenModelClient>
{
    public TokenClientValidator()
    {
        RuleFor(c => c.client_id).NotEmpty().WithMessage("El client_id es obligatorio.")
                                 .NotNull().WithMessage("El client_id es obligatorio.");

        RuleFor(c => c.client_secret).NotEmpty().WithMessage("El client_secret es obligatorio.")
                                     .NotNull().WithMessage("El client_secret es obligatorio.");

    }
}
