using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class AddendaRecordValidator : AbstractValidator<AddendaRecord>
{
    public AddendaRecordValidator()
    {
        RuleFor(x => x.AddendumSequence)
            .NotEmpty().Length(4);
    }
}
