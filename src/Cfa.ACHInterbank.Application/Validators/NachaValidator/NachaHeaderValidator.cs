using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class NachaHeaderValidator : AbstractValidator<NachaHeader>
{
    public NachaHeaderValidator()
    {
        RuleFor(x => x.ImmediateDestination)
            .NotEmpty().WithMessage("ImmediateDestination es requerido")
            .Length(10);

        RuleFor(x => x.ImmediateOrigin)
            .NotEmpty().WithMessage("ImmediateOrigin es requerido")
            .Length(10);

        RuleForEach(x => x.Batches).SetValidator(new BatchHeaderValidator());
    }
}
